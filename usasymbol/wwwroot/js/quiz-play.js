/*
 * Quiz player: practice / classic / timed rounds, segment progress, 1-4 keys, pause,
 * results with share + missed-answer links, anonymous analytics events.
 * Config comes from window.QUIZ (see Views/Quiz/Details.cshtml).
 */
(function () {
  "use strict";

  var Q = window.QUIZ;
  if (!Q || !Array.isArray(Q.questions) || !Q.questions.length) return;

  var $ = function (id) { return document.getElementById(id); };
  var app = $("quizApp"), startScreen = $("quizStartScreen"), stage = $("quizStage"), resultBox = $("quizResult");
  var startBtn = $("startQuizBtn"), roundLine = $("qzRound");
  if (!app || !startScreen || !stage || !resultBox || !startBtn) return;

  var el = {
    seg: $("qzSeg"), count: $("qzCount"), score: $("qzScore"), pause: $("qzPause"),
    timer: $("qzTimer"), timerBar: $("qzTimerBar"), timerText: $("qzTimerText"),
    image: $("questionImage"), question: $("questionText"), answers: $("answers"),
    form: $("typedAnswerForm"), input: $("typedAnswerInput"), submit: $("typedAnswerSubmit"),
    note: $("qzNote"), paused: $("qzPaused"), resume: $("qzResume")
  };

  var MODES = {
    practice: { name: "Practice", limit: Q.perRound, perQ: 0, session: 0, speed: 0 },
    classic:  { name: "Classic",  limit: Q.perRound, perQ: 20, session: 0, speed: 20 },
    timed:    { name: "Timed",    limit: Infinity,   perQ: 0, session: 60, speed: 15 }
  };

  MODES.daily = { name: "Daily", limit: 5, perQ: 0, session: 0, speed: 0 };

  var modeInputs = Array.prototype.slice.call(document.querySelectorAll('input[name="quizMode"]'));
  var mode = "practice", cfg = MODES.practice;

  // Daily quiz: ?daily=1 on today's quiz. Same 5 questions for everyone, picked by date.
  var isDaily = false;
  try {
    isDaily = new URLSearchParams(location.search).get("daily") === "1" &&
      Q.daily && Q.daily.slug === Q.slug && Q.questions.length >= 5;
  } catch (e) { }

  function seededRandom(seedText) {
    var h = 1779033703 ^ seedText.length;
    for (var i = 0; i < seedText.length; i++) { h = Math.imul(h ^ seedText.charCodeAt(i), 3432918353); h = (h << 13) | (h >>> 19); }
    var a = h >>> 0;
    return function () {
      a |= 0; a = (a + 0x6D2B79F5) | 0;
      var t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  function dailyPool() {
    var rnd = seededRandom("usasymbol-" + Q.daily.date + "-" + Q.slug);
    var idx = Q.questions.map(function (_, i) { return i; });
    for (var i = idx.length - 1; i > 0; i--) {
      var j = Math.floor(rnd() * (i + 1));
      var t = idx[i]; idx[i] = idx[j]; idx[j] = t;
    }
    return idx.slice(0, 5).map(function (i) { return Q.questions[i]; });
  }

  var STREAK_KEY = "usas.quiz.daily";
  function updateStreak() {
    // Returns the current streak. Stored only in this browser.
    try {
      var d = JSON.parse(localStorage.getItem(STREAK_KEY) || "null") || { streak: 0, last: "" };
      var today = Q.daily.date;
      if (d.last === today) return d.streak;
      var y = new Date(today + "T00:00:00Z"); y.setUTCDate(y.getUTCDate() - 1);
      d.streak = d.last === y.toISOString().slice(0, 10) ? d.streak + 1 : 1;
      d.last = today;
      localStorage.setItem(STREAK_KEY, JSON.stringify(d));
      return d.streak;
    } catch (e) { return 0; }
  }

  // ---------- helpers ----------
  function shuffle(a) {
    a = a.slice();
    for (var i = a.length - 1; i > 0; i--) {
      var j = Math.floor(Math.random() * (i + 1));
      var t = a[i]; a[i] = a[j]; a[j] = t;
    }
    return a;
  }
  function norm(v) { return String(v == null ? "" : v).trim().replace(/\s+/g, " ").toLocaleLowerCase("en-US"); }
  function esc(v) {
    return String(v == null ? "" : v).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }
  function field(q, a, b) { return q[a] != null && q[a] !== "" ? q[a] : q[b]; }
  function show(node) { if (node) node.hidden = false; }
  function hide(node) { if (node) node.hidden = true; }

  // ---------- analytics (anonymous; no ids, no cookies) ----------
  var events = [];
  function track(t, q, c, picked) {
    var e = { t: t };
    if (q) { e.q = q; e.c = !!c; if (picked && !Q.typed) e.p = String(picked).slice(0, 200); }
    events.push(e);
    if (events.length >= 25) flush();
  }
  function flush(useBeacon) {
    if (!events.length) return;
    var body = JSON.stringify({ mode: mode, events: events });
    events = [];
    var url = "/quizzes/" + Q.slug + "/events";
    try {
      if (useBeacon && navigator.sendBeacon) {
        navigator.sendBeacon(url, new Blob([body], { type: "application/json" }));
      } else {
        fetch(url, { method: "POST", headers: { "Content-Type": "application/json" }, body: body, keepalive: true }).catch(function () {});
      }
    } catch (e) { /* analytics never blocks play */ }
  }
  window.addEventListener("pagehide", function () { flush(true); });

  // ---------- start screen ----------
  function describeRound() {
    if (isDaily) {
      var modes = document.querySelector(".qz-modes");
      if (modes) modes.hidden = true;
      roundLine.innerHTML = "<strong>Today\u2019s 5 questions</strong>, the same for everyone. Finish one every day to build a streak.";
      startBtn.textContent = "Start today\u2019s quiz";
      return;
    }
    var c = MODES[mode];
    var total = Q.questions.length;
    roundLine.innerHTML = c.session
      ? "<strong>" + c.session + " seconds</strong> on the clock. Answer as many as you can."
      : "<strong>" + Math.min(c.limit, total) + " questions</strong> drawn from " + total + ", a new mix every round.";
    startBtn.textContent = "Start " + c.name;
  }
  function applyMode(m) {
    mode = MODES[m] ? m : "practice";
    cfg = MODES[mode];
    describeRound();
  }
  if (isDaily) { mode = "daily"; cfg = MODES.daily; }
  modeInputs.forEach(function (i) {
    if (i.checked && !isDaily) applyMode(i.value);
    i.addEventListener("change", function () { if (i.checked) applyMode(i.value); });
  });
  describeRound();

  // ---------- round state ----------
  var pool = [], index = 0, correct = 0, points = 0, streak = 0, bestStreak = 0, answered = 0;
  var marks = [];            // true / false per answered question, for segments and share grid
  var misses = [];
  var locked = true, paused = false, running = false;
  var qLeftMs = 0, qElapsedMs = 0, sessionLeftMs = 0, startedAt = 0, ticker = null;
  var awaitingNext = false;

  function stopTicker() { if (ticker) { clearInterval(ticker); ticker = null; } }

  function startRound(customPool) {
    stopTicker();
    pool = customPool ? shuffle(customPool) : (mode === "daily" ? dailyPool() : shuffle(Q.questions));
    index = 0; correct = 0; points = 0; streak = 0; bestStreak = 0; answered = 0;
    marks = []; misses = []; locked = true; paused = false; running = true; awaitingNext = false;
    sessionLeftMs = cfg.session * 1000;
    startedAt = Date.now();

    hide(startScreen); hide(resultBox); show(stage);
    hide(el.paused);
    el.pause.hidden = !(cfg.perQ || cfg.session);
    el.pause.textContent = "Pause";
    el.timer.hidden = !(cfg.perQ || cfg.session);
    buildSegments();
    track("start");
    ticker = setInterval(tick, 100);
    loadQuestion();
    app.scrollIntoView({ behavior: "smooth", block: "nearest" });
  }

  function roundLimit() {
    return cfg.limit === Infinity ? Infinity : Math.min(cfg.limit, pool.length);
  }

  function buildSegments() {
    var n = roundLimit();
    el.seg.innerHTML = "";
    el.seg.hidden = n === Infinity;
    if (n === Infinity) return;
    for (var i = 0; i < n; i++) {
      var s = document.createElement("i");
      el.seg.appendChild(s);
    }
  }

  function paintSegments() {
    var n = roundLimit();
    if (n === Infinity) return;
    var kids = el.seg.children;
    for (var i = 0; i < kids.length; i++) {
      var cls = "";
      if (i < marks.length) cls = marks[i] ? "is-ok" : "is-bad";
      else if (i === index) cls = "is-now";
      kids[i].className = cls;
    }
  }

  function updateHud() {
    var n = roundLimit();
    el.count.textContent = n === Infinity ? (answered + " answered") : ("Question " + Math.min(index + 1, n) + " of " + n);
    el.score.textContent = points + " pts";
    paintSegments();
  }

  // ---------- timers ----------
  function tick() {
    if (!running || paused) return;
    if (cfg.session) {
      sessionLeftMs -= 100;
      var pct = Math.max(0, sessionLeftMs / (cfg.session * 1000));
      el.timerBar.style.transform = "scaleX(" + pct + ")";
      el.timerText.textContent = Math.max(0, Math.ceil(sessionLeftMs / 1000)) + "s";
      if (sessionLeftMs <= 0) { finish(); return; }
    }
    if (cfg.perQ && !locked) {
      qLeftMs -= 100; qElapsedMs += 100;
      el.timerBar.style.transform = "scaleX(" + Math.max(0, qLeftMs / (cfg.perQ * 1000)) + ")";
      el.timerText.textContent = Math.max(0, Math.ceil(qLeftMs / 1000)) + "s";
      el.timer.classList.toggle("is-low", qLeftMs <= 5000);
      if (qLeftMs <= 0) onTimeUp();
    } else if (cfg.session && !locked) {
      qElapsedMs += 100;
    }
  }

  function setPaused(p) {
    if (!running || !(cfg.perQ || cfg.session)) return;
    paused = p;
    el.paused.hidden = !p;
    el.pause.textContent = p ? "Resume" : "Pause";
    if (p) { el.resume.focus({ preventScroll: true }); }
  }
  el.pause.addEventListener("click", function () { setPaused(!paused); });
  el.resume.addEventListener("click", function () { setPaused(false); });

  // ---------- questions ----------
  function current() { return pool[index]; }

  function loadQuestion() {
    var n = roundLimit();
    if (n !== Infinity && index >= n) return finish();
    if (!pool[index]) { pool = shuffle(Q.questions); index = 0; }
    var q = current();

    locked = false; awaitingNext = false;
    qElapsedMs = 0; qLeftMs = cfg.perQ * 1000;
    el.timer.classList.remove("is-low");
    if (cfg.perQ) {
      el.timerBar.style.transform = "scaleX(1)";
      el.timerText.textContent = cfg.perQ + "s";
    }

    el.note.hidden = true; el.note.innerHTML = ""; el.note.className = "qz-note";
    el.question.textContent = q.question || "Which state is this?";

    var img = field(q, "image_url", "imageUrl");
    el.image.innerHTML = img ? '<img src="' + esc(img) + '" alt="" decoding="async">' : "";
    el.image.hidden = !img;

    // warm the cache for the next image so questions do not flash
    var nextQ = pool[index + 1];
    var nextImg = nextQ && field(nextQ, "image_url", "imageUrl");
    if (nextImg) { var pre = new Image(); pre.src = nextImg; }

    el.answers.innerHTML = "";
    if (Q.typed) {
      el.answers.hidden = true;
      el.form.hidden = false;
      el.input.value = ""; el.input.disabled = false; el.submit.disabled = false;
      el.input.className = "qz-input";
      setTimeout(function () { el.input.focus({ preventScroll: true }); }, 0);
    } else {
      el.answers.hidden = false;
      if (el.form) el.form.hidden = true;
      var choices = (q.choices || []).filter(function (c) { return c && String(c).trim() !== ""; });
      shuffle(choices).forEach(function (choice, i) {
        var b = document.createElement("button");
        b.type = "button";
        b.className = "qz-ans";
        b.innerHTML = '<kbd aria-hidden="true">' + (i + 1) + '</kbd><span class="qz-ans-text"></span><i class="qz-ans-ico" aria-hidden="true"></i>';
        b.querySelector(".qz-ans-text").textContent = choice;
        b.addEventListener("click", function () { choose(b, choice); });
        el.answers.appendChild(b);
      });
    }
    updateHud();
  }

  function calcPoints() {
    var base = 100;
    if (!cfg.speed) return base;
    var left = Math.max(0, cfg.speed - qElapsedMs / 1000);
    return base + Math.ceil((left / cfg.speed) * 50);
  }

  function choose(btn, choice) {
    if (locked || paused) return;
    var q = current();
    var answer = field(q, "correct_answer", "correctAnswer");
    resolve(q, norm(choice) === norm(answer), choice, btn);
  }

  if (el.form) {
    el.form.addEventListener("submit", function (e) {
      e.preventDefault();
      if (locked || paused) return;
      var typed = el.input.value;
      if (!norm(typed)) { el.input.focus({ preventScroll: true }); return; }
      var q = current();
      var answer = field(q, "correct_answer", "correctAnswer");
      resolve(q, norm(typed) === norm(answer), typed, null);
    });
  }

  function onTimeUp() {
    if (locked) return;
    resolve(current(), false, null, null, true);
  }

  function resolve(q, ok, picked, btn, timeUp) {
    locked = true;
    answered++;
    var answer = field(q, "correct_answer", "correctAnswer");
    var explanation = q.explanation || "";
    var addPts = 0;

    if (ok) {
      addPts = calcPoints();
      points += addPts; correct++; streak++; bestStreak = Math.max(bestStreak, streak);
    } else {
      streak = 0;
      misses.push({ q: q, picked: picked, timeUp: !!timeUp });
    }
    marks.push(ok);
    track("answer", (q.question || "").trim(), ok, picked);

    // mark the answers
    Array.prototype.forEach.call(el.answers.children, function (b) {
      b.disabled = true;
      var text = b.querySelector(".qz-ans-text").textContent;
      var ico = b.querySelector(".qz-ans-ico");
      if (norm(text) === norm(answer)) {
        b.classList.add("is-ok"); ico.className = "qz-ans-ico fa-solid fa-check";
      } else if (b === btn) {
        b.classList.add("is-bad"); ico.className = "qz-ans-ico fa-solid fa-xmark";
      } else {
        b.classList.add("is-dim");
      }
    });
    if (Q.typed) {
      el.input.disabled = true; el.submit.disabled = true;
      el.input.className = "qz-input " + (ok ? "is-ok" : "is-bad");
    }

    updateHud();

    var timed = !!cfg.session;
    var head = ok ? "Correct" : (timeUp ? "Time’s up" : "Not quite");
    var html = '<div class="qz-note-head ' + (ok ? "is-ok" : "is-bad") + '"><i class="fa-solid ' + (ok ? "fa-check" : "fa-xmark") + '" aria-hidden="true"></i> ' + head;
    if (ok && addPts) html += ' <span class="qz-note-pts">+' + addPts + '</span>';
    html += "</div>";
    if (!ok) html += '<div class="qz-note-ans">Answer: <strong>' + esc(answer) + "</strong></div>";

    if (!timed) {
      var st = Q.stats && Q.stats[(q.question || "").trim()];
      if (st && !Q.typed) {
        var right = st[answer];
        var line = typeof right === "number" ? "About " + right + "% of players got this right." : "";
        if (!ok && picked && typeof st[picked] === "number") line += " About " + st[picked] + "% picked the same answer as you.";
        if (line) html += '<p class="qz-note-stat">' + esc(line.trim()) + "</p>";
      }
      if (explanation) html += '<p class="qz-note-text">' + esc(explanation) + "</p>";
      if (q.link_url) html += '<a class="qz-note-link" href="' + esc(q.link_url) + '">' + esc(q.link_label || "Read more") + ' <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>';
      var last = roundLimit() !== Infinity && index + 1 >= roundLimit();
      html += '<button type="button" class="qz-next" id="qzNext">' + (last ? "See results" : "Next question") + ' <span class="qz-key">Enter</span></button>';
    }
    el.note.className = "qz-note " + (ok ? "is-ok" : "is-bad");
    el.note.innerHTML = html;
    el.note.hidden = false;

    if (timed) {
      setTimeout(function () { if (running && !paused) { index++; loadQuestion(); } }, ok ? 700 : 1300);
    } else {
      awaitingNext = true;
      var nb = $("qzNext");
      if (nb) { nb.addEventListener("click", next); nb.focus({ preventScroll: true }); }
    }
  }

  function next() {
    if (!awaitingNext) return;
    awaitingNext = false;
    index++;
    loadQuestion();
  }

  // ---------- keyboard ----------
  document.addEventListener("keydown", function (e) {
    if (stage.hidden || !running || e.ctrlKey || e.metaKey || e.altKey) return;
    var typing = e.target && (e.target.tagName === "INPUT" || e.target.tagName === "TEXTAREA");
    if (e.key === "p" || e.key === "P") { if (!typing) { e.preventDefault(); setPaused(!paused); } return; }
    if (paused) return;
    if (!typing && e.key >= "1" && e.key <= "4") {
      var b = el.answers.children[Number(e.key) - 1];
      if (b && !locked) { e.preventDefault(); b.click(); }
      return;
    }
    if ((e.key === "Enter" || e.key === " ") && awaitingNext && !typing) {
      e.preventDefault();
      next();
    }
  });

  // ---------- results ----------
  function finish() {
    if (!running) return;
    running = false;
    stopTicker();
    track("complete");
    flush();

    var secs = Math.max(0, Math.round((Date.now() - startedAt) / 1000));
    var timed = !!cfg.session;
    var total = timed ? answered : roundLimit();
    var pct = total ? Math.round(correct / total * 100) : 0;
    var resultIcon = pct >= 80 ? "trophy" : pct >= 60 ? "graduation-cap" : "book-open";
    var verdict = pct === 100 ? "A perfect round."
      : pct >= 80 ? "Very strong. Just a slip or two."
      : pct >= 60 ? "A solid round. Review the answers to fill in the gaps."
      : pct >= 40 ? "Not bad. A fresh round might go better."
      : "A good place to start. Play again and it will stick.";
    if (!answered) verdict = "Time ran out before the first answer. Try again.";

    var streakNow = mode === "daily" ? updateStreak() : 0;
    if (!timed && total > 0) {
      try {
        var bestAll = JSON.parse(localStorage.getItem("usas.quiz.best") || "{}") || {};
        var prev = bestAll[Q.slug];
        if (!prev || correct / total > prev.s / prev.n) { bestAll[Q.slug] = { s: correct, n: total }; localStorage.setItem("usas.quiz.best", JSON.stringify(bestAll)); }
        if (Q.slug.endsWith("-state-symbols-quiz")) {
          var states = JSON.parse(localStorage.getItem("usas.quiz.states") || "{}") || {};
          states[Q.slug.slice(0, -"-state-symbols-quiz".length)] = true;
          localStorage.setItem("usas.quiz.states", JSON.stringify(states));
        }
      } catch (e) { }
    }
    var html = '<div class="qz-res">';
    html += '<div class="qz-res-mode">' + esc(cfg.name) + (timed ? " · 60 seconds" : "") + "</div>";
    html += '<div class="qz-res-icon" aria-hidden="true"><svg class="quiz-icon"><use href="/icons/quiz.svg#' + resultIcon + '"></use></svg></div>';
    html += '<div class="qz-res-score">' + correct + '<span> / ' + (total || 0) + "</span></div>";
    html += '<p class="qz-res-verdict">' + verdict + "</p>";
    html += '<div class="qz-res-stats"><div><b>' + points + '</b><span>points</span></div><div><b>' + bestStreak + '</b><span>best streak</span></div><div><b>' + secs + 's</b><span>time</span></div></div>';
    if (streakNow > 0) html += '<div class="qz-streak"><i class="fa-solid fa-fire" aria-hidden="true"></i> ' + streakNow + '-day streak. Come back tomorrow for a new quiz.</div>';
    html += '<div id="communityStats" class="qz-community" hidden></div>';

    html += '<div class="qz-res-actions">';
    html += '<button type="button" class="qz-btn qz-btn-primary" id="qzAgain"><i class="fa-solid fa-rotate-right" aria-hidden="true"></i> Play again</button>';
    html += '<button type="button" class="qz-btn" id="qzShare"><i class="fa-solid fa-share-nodes" aria-hidden="true"></i> Share result</button>';
    if (misses.length && !timed) html += '<button type="button" class="qz-btn" id="qzMissed">Replay ' + misses.length + " missed</button>";
    if (!timed && mode !== "daily") html += '<button type="button" class="qz-btn" id="qzTimed"><i class="fa-solid fa-stopwatch" aria-hidden="true"></i> Try it timed</button>';
    html += "</div>";

    if (misses.length) {
      html += '<div class="qz-review"><h3>Review your misses</h3><ul>';
      misses.slice(0, 8).forEach(function (m) {
        var q = m.q;
        html += "<li><div class=\"qz-review-q\">" + esc(q.question) + "</div>";
        html += '<div class="qz-review-a">Answer: <strong>' + esc(field(q, "correct_answer", "correctAnswer")) + "</strong></div>";
        if (q.explanation) html += '<p class="qz-review-e">' + esc(q.explanation) + "</p>";
        if (q.link_url) html += '<a class="qz-note-link" href="' + esc(q.link_url) + '">' + esc(q.link_label || "Read more") + ' <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>';
        html += "</li>";
      });
      html += "</ul></div>";
    }

    if (Q.more && Q.more.length) {
      html += '<div class="qz-more"><h3>Keep going</h3><div class="qz-more-grid">';
      Q.more.forEach(function (m) {
        html += '<a href="' + esc(m.url) + '"><b>' + esc(m.title) + "</b><span>" + esc(m.meta) + "</span></a>";
      });
      html += "</div></div>";
    }
    html += "</div>";

    hide(stage);
    resultBox.innerHTML = html;
    show(resultBox);
    app.scrollIntoView({ behavior: "smooth", block: "start" });

    $("qzAgain").addEventListener("click", function () { startRound(); });
    var again = $("qzMissed");
    if (again) again.addEventListener("click", function () {
      startRound(misses.map(function (m) { return m.q; }));
    });
    var tm = $("qzTimed");
    if (tm) tm.addEventListener("click", function () { mode = "timed"; cfg = MODES.timed; startRound(); });
    $("qzShare").addEventListener("click", function () { share(this, timed, total, secs); });

    // community percentile (existing endpoint)
    fetch("/quizzes/" + Q.slug + "/result", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ mode: mode, score: pct, questionCount: answered, timeSpentSeconds: secs })
    }).then(function (r) { return r.ok ? r.json() : Promise.reject(); }).then(function (d) {
      var p = Number.isFinite(d && d.percentile) ? Math.max(0, Math.min(100, Math.round(d.percentile))) : 0;
      var box = $("communityStats");
      if (!box || p <= 0) return;
      var beat = Math.max(10, Math.floor(p / 10) * 10);
      box.textContent = "Better than " + beat + "% of players";
      box.hidden = false;
    }).catch(function () {});
  }

  function share(btn, timed, total, secs) {
    var grid = marks.map(function (m) { return m ? "🟩" : "🟥"; }).join("");
    var line = timed
      ? correct + " correct in 60 seconds"
      : correct + "/" + total;
    var text = Q.title + " (" + cfg.name + ")\n" + line + (grid && !timed ? "\n" + grid : "") + "\nCan you beat it?";
    var bits = marks.map(function (m) { return m ? "1" : "0"; }).join("").slice(0, 20);
    var url = location.origin + location.pathname + "?s=" + correct + "&n=" + (total || 0) + "&m=" + mode + "&g=" + bits;
    var done = function (label) {
      var old = btn.innerHTML;
      btn.textContent = label;
      setTimeout(function () { btn.innerHTML = old; }, 1800);
    };
    if (navigator.share) {
      navigator.share({ title: Q.title, text: text, url: url }).catch(function () {});
      return;
    }
    var full = text + "\n" + url;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(full).then(function () { done("Copied to clipboard"); }, function () { done("Copy failed"); });
    } else {
      done("Copy not supported");
    }
  }

  startBtn.addEventListener("click", function () {
    var picked = modeInputs.filter(function (i) { return i.checked; })[0];
    if (picked && !isDaily) applyMode(picked.value);
    startRound();
  });
})();
