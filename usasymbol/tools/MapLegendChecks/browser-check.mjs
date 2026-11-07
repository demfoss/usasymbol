// Uses the installed browser and Node's built-in CDP client; no npm dependencies.
import { spawn } from 'node:child_process';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const output = path.resolve('artifacts/map-legend-checks');
await mkdir(output, { recursive: true });
const browser = spawn(process.env.MAP_CHECK_BROWSER || 'C:/Program Files/Google/Chrome/Application/chrome.exe', [
  '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', '--disable-background-networking',
  // The executor already isolates the process; only local, generated snapshots are opened.
  '--no-sandbox', '--host-resolver-rules=MAP * ~NOTFOUND',
  '--remote-debugging-pipe', `--user-data-dir=${path.join(output, 'browser-profile-' + process.pid)}`, 'about:blank'
], { windowsHide: true, stdio: ['ignore', 'ignore', 'pipe', 'pipe', 'pipe'] });
let rootClient;
let checks = 0;
function check(condition, message) {
  if (!condition) throw new Error(message);
  checks++;
}
function connect() {
  let id = 0;
  let buffer = '';
  const pending = new Map();
  const listeners = new Map();
  browser.stdio[4].on('data', chunk => {
    buffer += chunk.toString();
    let boundary;
    while ((boundary = buffer.indexOf('\0')) !== -1) {
      const raw = buffer.slice(0, boundary);
      buffer = buffer.slice(boundary + 1);
      if (!raw) continue;
      const message = JSON.parse(raw);
      if (message.id) {
        const handler = pending.get(message.id);
        if (handler) {
          pending.delete(message.id);
          message.error ? handler.reject(new Error(JSON.stringify(message.error))) : handler.resolve(message.result);
        }
      } else {
        (listeners.get(`${message.sessionId || ''}:${message.method}`) || []).forEach(handler => handler(message.params));
      }
    }
  });
  return {
    send(method, params = {}, sessionId) {
      return new Promise((resolve, reject) => {
        const request = ++id;
        const timeout = setTimeout(() => { pending.delete(request); reject(new Error(`Timeout: ${method}`)); }, 15000);
        pending.set(request, {
          resolve(value) { clearTimeout(timeout); resolve(value); },
          reject(error) { clearTimeout(timeout); reject(error); }
        });
        browser.stdio[3].write(JSON.stringify({ id: request, method, params, sessionId }) + '\0');
      });
    },
    on(method, handler, sessionId = '') {
      const key = `${sessionId}:${method}`;
      listeners.set(key, [...(listeners.get(key) || []), handler]);
    },
    close() { browser.stdio[3].end(); }
  };
}
try {
  let browserErrors = '';
  browser.stderr.on('data', chunk => { browserErrors = (browserErrors + chunk).slice(-6000); });
  browser.on('error', error => { console.error(error.message); });
  rootClient = connect();
  const { targetId } = await rootClient.send('Target.createTarget', { url: 'about:blank' });
  const { sessionId } = await rootClient.send('Target.attachToTarget', { targetId, flatten: true });
  const client = {
    send: (method, params) => rootClient.send(method, params, sessionId),
    on: (method, handler) => rootClient.on(method, handler, sessionId),
    close() {}
  };
  const errors = [];
  client.on('Runtime.exceptionThrown', error => errors.push(error.exceptionDetails.text));
  await client.send('Page.enable');
  await client.send('Runtime.enable');
  await client.send('Emulation.setFocusEmulationEnabled', { enabled: true });
  const evaluate = async expression => {
    const result = await client.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
    return result.result.value;
  };
  for (const slug of ['asthma', 'area-codes', 'categories', 'cancer', 'salary', 'years']) {
    for (const width of [1280, 375]) {
      await client.send('Emulation.setDeviceMetricsOverride', { width, height: 1000, deviceScaleFactor: 1, mobile: width < 500 });
      const loaded = new Promise(resolve => client.on('Page.loadEventFired', resolve));
      await client.send('Page.navigate', { url: pathToFileURL(path.join(output, `${slug}.html`)).href });
      await loaded;
      check(await evaluate("document.querySelectorAll('#map-legend').length === 1"), `${slug}/${width}: single legend`);
      check(await evaluate('document.documentElement.scrollWidth <= innerWidth'), `${slug}/${width}: no page overflow`);
      check(await evaluate("!document.querySelector('[data-state-key=HI]')"), `${slug}/${width}: Hawaii is not duplicated`);
      if (width < 500) check(await evaluate("getComputedStyle(document.querySelector('.map-legend__scroll')).overflowX === 'auto'"), `${slug}: horizontal legend scroll`);
      const selected = await evaluate(`(() => {
        const button = document.querySelector('[data-legend-key]');
        button.click();
        return button.getAttribute('aria-pressed') === 'true' && getComputedStyle(document.querySelector('#map-clear-filters')).display !== 'none';
      })()`);
      check(selected, `${slug}/${width}: selection and clear button`);
      check(await evaluate(`(() => {
        const data = JSON.parse(document.querySelector('#choropleth-data').textContent);
        const key = document.querySelector('[data-legend-key][aria-pressed=true]').dataset.legendKey;
        return Object.entries(data).every(([code, record]) => {
          const state = document.querySelector('#usmap-container .' + code.toLowerCase());
          return !state || Number(state.style.opacity) === (record.lk.includes(key) ? 1 : .18);
        });
      })()`), `${slug}/${width}: exact server membership controls opacity`);
      check(await evaluate(`(() => {
        const button = document.querySelector('[data-legend-key][aria-pressed=true]');
        button.click();
        return button.getAttribute('aria-pressed') === 'false';
      })()`), `${slug}/${width}: second click clears selection`);
      if (slug === 'asthma') {
        check(await evaluate(`(() => {
          document.querySelector('[data-legend-key="no-data"]').click();
          return document.querySelector('#map-legend-detail').textContent.includes('Kentucky')
            && document.querySelector('#usmap-container .ky').style.fill.includes('map-no-data-pattern')
            && document.querySelector('#usmap-container .pa').style.opacity === '1';
        })()`), `asthma/${width}: accessible missing-state list and hatching`);
        await evaluate("document.querySelector('#map-clear-filters').click()");
      }
      check(await evaluate("document.querySelectorAll('[data-legend-key][aria-pressed=true]').length === 0"), `${slug}/${width}: reset clears pressed state`);
      check(await evaluate("getComputedStyle(document.querySelector('#map-clear-filters')).display === 'none'"), `${slug}/${width}: reset hides clear button`);
      await evaluate("document.querySelector('[data-legend-key]').focus()");
      await client.send('Input.dispatchKeyEvent', { type: 'keyDown', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13, text: '\r' });
      await client.send('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
      check(await evaluate("document.querySelector('[data-legend-key]').getAttribute('aria-pressed') === 'true'"), `${slug}/${width}: keyboard activation`);
      await evaluate("document.querySelector('#map-clear-filters').click(); document.activeElement.blur(); document.querySelector('.map-legend__scroll').scrollLeft = 0; window.scrollTo(0,0)");
      if (slug === 'asthma' || slug === 'categories') {
        const screenshot = await client.send('Page.captureScreenshot', { format: 'png' });
        await writeFile(path.join(output, `${slug}-${width}.png`), Buffer.from(screenshot.data, 'base64'));
      }
    }
  }
  check(errors.length === 0, `No JavaScript exceptions: ${errors.join(', ')}`);
  console.log(`Passed ${checks} desktop/mobile browser checks. Screenshots: ${output}`);
  client.close();
} finally {
  if (rootClient) {
    await rootClient.send('Browser.close').catch(() => {});
    rootClient.close();
  } else browser.kill();
}
