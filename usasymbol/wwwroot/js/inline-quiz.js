(function () {
  'use strict';
  document.querySelectorAll('.iq-widget').forEach(function (root) {
    if (root.dataset.iqReady) return;
    root.dataset.iqReady = 'true';
    var $ = function (name) { return root.querySelector('[data-iq="' + name + '"]'); };
    var data;
    try { data = JSON.parse($('data').textContent); } catch (_) { return; }
    var pool = data.pool.filter(function (q) { return q.q && q.c && q.c.indexOf(q.a) !== -1; });
    var per = Math.min(data.perRound, pool.length);
    if (per < 1) return;
    var round, index, score, locked, completed, began, startedAt, token = 0;
    var history = [], seen = new Set(), events = [];
    var stage = $('stage'), result = $('result'), answers = $('answers'), feedback = $('feedback');
    var dots = Array.from($('progress').children);
    var bestKey = 'usas.quiz.inline.best', statesKey = 'usas.quiz.states';

    function node(tag, className, text) {
      var element = document.createElement(tag);
      if (className) element.className = className;
      if (text != null) element.textContent = text;
      return element;
    }
    function icon(name) {
      var element = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      element.setAttribute('class', 'quiz-icon'); element.setAttribute('aria-hidden', 'true');
      var use = document.createElementNS('http://www.w3.org/2000/svg', 'use');
      use.setAttribute('href', '/icons/quiz.svg#' + name); element.appendChild(use); return element;
    }
    function internal(url) { return typeof url === 'string' && url[0] === '/' && url[1] !== '/' ? url : data.fullQuizUrl; }
    function link(url, label) { var element = node('a', '', label); element.href = internal(url); return element; }
    function picture(url, alt) {
      var image = node('img'); image.src = url; image.alt = alt || ''; image.decoding = 'async';
      image.addEventListener('error', function () { image.hidden = true; }); return image;
    }
    function shuffle(items) {
      var list = items.slice();
      for (var i = list.length - 1; i > 0; i--) {
        var other = Math.floor(Math.random() * (i + 1)), item = list[i]; list[i] = list[other]; list[other] = item;
      }
      return list;
    }
    function chooseRound() {
      var fresh = pool.filter(function (q) { return !seen.has(q.q); });
      if (new Set(fresh.map(function (q) { return q.family || q.q; })).size < per) { seen.clear(); fresh = pool; }
      var chosen = [], families = new Set();
      function add(items, count) {
        var added = 0;
        shuffle(items).forEach(function (q) {
          var family = q.family || q.q;
          if (added >= count || chosen.length >= per || families.has(family)) return;
          chosen.push(q); families.add(family); added++;
        });
      }
      add(fresh.filter(function (q) { return q.image; }), Math.floor(per / 2));
      add(fresh.filter(function (q) { return !q.image; }), per - chosen.length);
      add(fresh, per - chosen.length);
      add(pool, per - chosen.length);
      if (chosen.length < per) shuffle(fresh).forEach(function (q) { if (chosen.length < per && chosen.indexOf(q) < 0) chosen.push(q); });
      // Alternate text and image questions when the topic supplies both.
      var visual = shuffle(chosen.filter(function (q) { return q.image; }));
      var text = shuffle(chosen.filter(function (q) { return !q.image; }));
      return chosen.map(function (_, i) { return (i % 2 && visual.length ? visual : text.length ? text : visual).shift(); });
    }
    function track(type, q, correct, picked) {
      var event = { t: type };
      if (q) event.q = q;
      if (typeof correct === 'boolean') event.c = correct;
      if (picked) event.p = picked;
      events.push(event);
      window.dataLayer = window.dataLayer || [];
      window.dataLayer.push({ event: 'quiz_' + type, quiz_slug: data.slug, quiz_mode: 'inline',
        question_number: Math.min(index + 1, per), is_correct: correct, destination: type === 'result_click' ? q : undefined });
      if (events.length >= 20) flush();
    }
    function flush(beacon) {
      if (!events.length) return Promise.resolve();
      var body = JSON.stringify({ mode: 'inline', events: events }); events = [];
      var url = '/quizzes/' + encodeURIComponent(data.slug) + '/events';
      try {
        if (beacon && navigator.sendBeacon) { navigator.sendBeacon(url, new Blob([body], { type: 'application/json' })); return Promise.resolve(); }
        return fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: body, keepalive: true }).catch(function () {});
      } catch (_) { return Promise.resolve(); }
    }
    function begin() { if (!began && !completed) { began = true; startedAt = Date.now(); track('start'); flush(); } }
    function visible() { var box = root.getBoundingClientRect(); return box.top < innerHeight && box.bottom > 0; }
    function read(key) {
      try { var value = JSON.parse(localStorage.getItem(key) || '{}'); return value && typeof value === 'object' && !Array.isArray(value) ? value : {}; }
      catch (_) { return {}; }
    }
    function store(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch (_) {} }
    function progress() {
      dots.forEach(function (dot, i) {
        dot.className = 'iq-dot' + (history[i] ? history[i].correct ? ' is-correct' : ' is-wrong' : i === index && !completed ? ' is-current' : '');
        dot.setAttribute('aria-label', 'Question ' + (i + 1) + (history[i] ? history[i].correct ? ', correct' : ', incorrect' : i === index && !completed ? ', current' : ', not answered'));
        if (i === index && !completed) dot.setAttribute('aria-current', 'step'); else dot.removeAttribute('aria-current');
      });
    }
    function start(focus) {
      token++; round = chooseRound(); index = 0; score = 0; locked = false; completed = false; began = false; history = [];
      round.forEach(function (q) { seen.add(q.q); });
      result.hidden = true; $('more').hidden = true; stage.hidden = false;
      $('community').hidden = true; $('review').open = false; $('share-status').textContent = '';
      show(focus);
      if (visible()) begin();
    }
    function show(focus) {
      var q = round[index]; locked = false; progress();
      $('question').textContent = q.q;
      $('image').replaceChildren(); $('image').hidden = !q.image;
      if (q.image) $('image').appendChild(picture(q.image, 'Identify the symbol shown.'));
      feedback.replaceChildren(); feedback.className = 'iq-feedback'; $('next').hidden = true;
      $('next').replaceChildren(document.createTextNode(index === round.length - 1 ? 'See result ' : 'Next '), icon('arrow-right'));
      answers.replaceChildren();
      shuffle(q.c).forEach(function (choice, i) {
        var button = node('button', 'iq-choice'); button.type = 'button'; button.dataset.choice = choice;
        button.append(node('span', 'iq-choice__letter', String.fromCharCode(65 + i)), node('span', 'iq-choice__text', choice));
        button.setAttribute('aria-keyshortcuts', String(i + 1));
        button.addEventListener('click', function () { pick(button, choice, q); }); answers.appendChild(button);
      });
      if (focus) $('question').focus({ preventScroll: true });
    }
    function pick(button, choice, q) {
      if (locked || completed) return;
      begin(); locked = true;
      var correct = choice === q.a;
      if (correct) score++;
      history.push({ q: q, choice: choice, correct: correct }); progress();
      Array.from(answers.children).forEach(function (item) {
        item.disabled = true;
        item.classList.add(item.dataset.choice === q.a ? 'is-correct' : item === button ? 'is-wrong' : 'is-muted');
      });
      var heading = node('div', 'iq-feedback__heading');
      heading.append(icon('circle-check'), document.createTextNode((correct ? 'Correct: ' : 'Correct answer: ') + q.a));
      feedback.className = 'iq-feedback' + (correct ? '' : ' is-wrong');
      feedback.appendChild(heading);
      var body = node('div', 'iq-feedback__body'), text = node('div');
      if (q.answerImage) body.appendChild(picture(q.answerImage, q.a));
      if (q.e) text.appendChild(node('p', '', q.e));
      if (q.linkUrl) text.appendChild(link(q.linkUrl, 'Read more about ' + (q.linkLabel || q.a) + ' →'));
      body.appendChild(text); feedback.appendChild(body);
      if (q.related && q.related.length) {
        var related = node('p', 'iq-feedback__related', 'Other symbols: ');
        q.related.slice(0, 3).forEach(function (item, i) { if (i) related.appendChild(document.createTextNode(' · ')); related.appendChild(link(item.url, item.answer + ' — ' + item.state)); });
        feedback.appendChild(related);
      }
      $('next').hidden = false;
      track('answer', q.q, correct, choice); flush();
    }
    function renderReview() {
      var list = $('review-list'); list.replaceChildren();
      history.forEach(function (answer) {
        var item = node('li'), q = answer.q, body = node('div');
        if (q.answerImage) item.appendChild(picture(q.answerImage, q.a));
        body.appendChild(node('strong', '', q.q));
        body.appendChild(node('p', answer.correct ? 'is-correct' : 'is-wrong', (answer.correct ? 'Correct: ' : 'Your answer: ') + answer.choice));
        if (!answer.correct) body.appendChild(node('p', 'is-correct', 'Correct answer: ' + q.a));
        if (q.e) body.appendChild(node('p', '', q.e));
        if (q.linkUrl) body.appendChild(link(q.linkUrl, (q.linkLabel || q.a) + ' →'));
        item.appendChild(body); list.appendChild(item);
      });
    }
    function personalRecord() {
      var best = read(bestKey), previous = Number(best[data.slug]) || 0;
      best[data.slug] = Math.max(previous, score); store(bestKey, best);
      $('record').textContent = 'Your best: ' + best[data.slug] + ' / ' + per;
      if (!data.stateSlug) return;
      var states = read(statesKey); states[data.stateSlug] = true; store(statesKey, states);
      var valid = data.states.filter(function (state) { return states[state.slug]; });
      $('collection-label').textContent = 'Your state collection: ' + valid.length + ' / 50 explored';
      $('collection-grid').replaceChildren();
      data.states.forEach(function (state) {
        var destination = link(state.url, state.abbr); destination.title = state.name + (states[state.slug] ? ' — completed' : ' — try this state');
        if (states[state.slug]) destination.classList.add('is-complete');
        destination.dataset.iq = 'destination'; $('collection-grid').appendChild(destination);
      });
    }
    function finish() {
      if (completed) return;
      completed = true; progress(); stage.hidden = true; result.hidden = false; $('more').hidden = false;
      $('final').textContent = score;
      var ratio = score / per;
      $('result-icon').replaceChildren(icon(ratio >= .8 ? 'trophy' : ratio >= .6 ? 'graduation-cap' : 'book-open'));
      $('verdict').textContent = ratio >= .8 ? (data.stateName ? data.stateName + ' expert! ' : 'A strong round! ') + 'Ready for a new challenge?'
        : ratio >= .6 ? 'A good round. Explore the answers below to fill in the gaps.' : 'Every answer has a story. Review yours, then try a fresh round.';
      renderReview(); personalRecord(); track('complete');
      var current = token;
      var submission = { answers: history.map(function (answer) { return { question: answer.q.q, choice: answer.choice }; }),
        timeSpentSeconds: Math.min(7200, Math.max(0, Math.round((Date.now() - startedAt) / 1000))) };
      flush().then(function () {
        return fetch('/quizzes/' + encodeURIComponent(data.slug) + '/inline-result', { method: 'POST', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(submission) });
      }).then(function (response) { return response.ok && response.status !== 204 ? response.json() : null; }).then(function (stats) {
        if (!stats || current !== token || !completed) return;
        var lines = [];
        if (stats.rounds >= 20 && Number.isFinite(stats.percentile)) lines.push('Better than ' + stats.percentile + '% of ' + stats.rounds + ' completed five-question rounds.');
        if (stats.hardestQuestion && stats.questionAnswers >= 20 && Number.isFinite(stats.correctPercent))
          lines.push('The toughest question this round: “' + stats.hardestQuestion + '” ' + stats.correctPercent + '% answered correctly.');
        $('community').textContent = lines.join(' '); $('community').hidden = !lines.length;
      }).catch(function () {});
      $('result-heading').focus({ preventScroll: true });
    }
    $('next').addEventListener('click', function () { if (!locked || completed) return; index++; if (index < round.length) show(true); else finish(); });
    $('again').addEventListener('click', function () { track('result_click', 'again'); flush(); start(true); });
    $('share').addEventListener('click', function () {
      track('result_click', 'share'); flush();
      var url = new URL(data.fullQuizUrl, location.origin);
      url.search = new URLSearchParams({ s: score, n: per, m: 'inline', g: history.map(function (answer) { return answer.correct ? '1' : '0'; }).join('') }).toString();
      var text = 'I got ' + score + '/' + per + ' on ' + data.title + '. Can you beat it?';
      if (navigator.share) navigator.share({ title: data.title, text: text, url: url.href }).catch(function (error) {
        if (error.name !== 'AbortError') $('share-status').textContent = 'Share this link: ' + url.href;
      });
      else if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text + '\n' + url.href).then(function () {
        $('share-status').textContent = 'Score and link copied.';
      }).catch(function () { $('share-status').textContent = 'Share this link: ' + url.href; });
      else $('share-status').textContent = 'Share this link: ' + url.href;
    });
    root.addEventListener('click', function (event) {
      var destination = event.target.closest('a');
      if (destination && completed) { track('result_click', destination.getAttribute('href')); flush(true); }
    });
    root.addEventListener('keydown', function (event) {
      if (completed || event.ctrlKey || event.altKey || event.metaKey || /INPUT|TEXTAREA|SELECT/.test(event.target.tagName)) return;
      var position = '1234'.indexOf(event.key);
      if (position < 0) position = 'abcd'.indexOf(event.key.toLowerCase());
      if (position >= 0 && !locked && answers.children[position]) { event.preventDefault(); answers.children[position].click(); }
    });
    window.addEventListener('pagehide', function () { flush(true); });
    start(false);
    if ('IntersectionObserver' in window) new IntersectionObserver(function (entries) { if (entries[0].isIntersecting) begin(); }, { threshold: .15 }).observe(root.querySelector('.iq-card'));
  });
})();
