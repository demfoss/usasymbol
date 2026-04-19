document.addEventListener("DOMContentLoaded", () => {
    const allQuestions = window.quizData || [];
    if (!allQuestions.length) return;

    let quiz = [...allQuestions].sort(() => Math.random() - 0.5);
    if (quiz.length > 10) quiz = quiz.slice(0, 10);

    const total = quiz.length;
    let index = 0;
    let score = 0;

    let streak = 0;
    let maxStreak = 0;

    const scoreEl = document.getElementById("score");
    const questionNumEl = document.getElementById("questionNum");
    const questionText = document.getElementById("questionText");
    const questionImage = document.getElementById("questionImage");
    const answersBox = document.getElementById("answers");
    const feedbackText = document.getElementById("feedbackText");
    const nextBtn = document.getElementById("nextBtn");
    const totalEl = document.getElementById("total");
    const quizApp = document.getElementById("quizApp");

    if (totalEl) totalEl.innerText = total;

    function vibrate(pattern) {
        if (navigator.vibrate) navigator.vibrate(pattern);
    }

    function loadQuestion() {
        const q = quiz[index];

        questionText.innerText = q.question || "Which state is this";
        questionNumEl.innerText = index + 1;
        feedbackText.innerHTML = "";
        nextBtn.disabled = true;

        if (q.image_url || q.imageUrl) {
            const imgUrl = q.image_url || q.imageUrl;
            questionImage.innerHTML = `
                <img src="${imgUrl}" 
                     class="h-40 md:h-56 lg:h-64 object-contain rounded-lg border border-gray-300 bg-white">
            `;
            questionImage.style.display = "flex";
            questionImage.className = "flex justify-center mb-3";
        } else {
            questionImage.style.display = "none";
        }

        answersBox.innerHTML = "";

        const choices = (q.choices || []).filter(x => x && x.trim() !== "");
        const shuffled = [...choices].sort(() => Math.random() - 0.5);

        shuffled.forEach(choice => {
            const btn = document.createElement("button");
            btn.className =
                "w-full px-4 py-3 rounded-lg bg-gray-100 text-gray-900 border border-gray-300 font-semibold text-sm hover:bg-blue-100 hover:border-blue-400 transition";
            btn.innerText = choice;
            btn.addEventListener("click", () => handleAnswer(btn, q.correct_answer || q.correctAnswer, q.explanation));
            answersBox.appendChild(btn);
        });
    }

    function handleAnswer(button, correctAnswer, explanation) {
        const isCorrect = button.innerText.trim() === correctAnswer.trim();

        disableButtons();

        if (isCorrect) {
            button.style.backgroundColor = "#22c55e";
            button.style.color = "#ffffff";
            button.style.borderColor = "#16a34a";
            button.style.opacity = "1";

            score++;
            streak++;
            maxStreak = Math.max(maxStreak, streak);

            if (scoreEl) scoreEl.innerText = score;

            vibrate(60);

            feedbackText.innerHTML = `<span class="text-green-600 font-bold">✓ Correct</span>`;

            if (streak >= 3) showStreakNotification(streak);
        } else {
            button.style.backgroundColor = "#ef4444";
            button.style.color = "#ffffff";
            button.style.borderColor = "#dc2626";
            button.style.opacity = "1";

            vibrate([80, 40, 80]);
            streak = 0;

            feedbackText.innerHTML = `<span class="text-red-600 font-bold">✗ Incorrect. Correct: ${correctAnswer}</span>`;

            [...answersBox.children].forEach(btn => {
                if (btn.innerText.trim() === correctAnswer.trim()) {
                    btn.style.backgroundColor = "#22c55e";
                    btn.style.color = "#ffffff";
                    btn.style.borderColor = "#16a34a";
                    btn.style.opacity = "1";
                }
            });
        }

        if (explanation) {
            feedbackText.innerHTML += `<div class="text-xs text-gray-600 mt-1">${explanation}</div>`;
        }

        nextBtn.disabled = false;
    }

    function showStreakNotification(count) {
        const notification = document.createElement("div");
        notification.className =
            "fixed top-4 right-4 bg-orange-500 text-white px-5 py-2 rounded-lg shadow-lg z-50 animate-bounce";
        notification.innerHTML = `🔥 ${count} in a row!`;
        document.body.appendChild(notification);
        setTimeout(() => notification.remove(), 2000);
    }

    function disableButtons() {
        [...answersBox.children].forEach(btn => {
            btn.disabled = true;
            btn.classList.add("cursor-not-allowed");
            btn.style.opacity = "0.7";
        });
    }

    nextBtn.addEventListener("click", () => {
        index++;
        if (index >= total) {
            showResult();
        } else {
            loadQuestion();
        }
    });

    function showResult() {
        const isPerfect = score === total;
        const isExpert = score >= total * 0.9;

        let achievementsHtml = "";
        if (isPerfect) achievementsHtml += `<div class="bg-yellow-100 px-4 py-2 rounded mb-2 text-yellow-800 font-bold">🏆 Perfect Score</div>`;
        if (isExpert) achievementsHtml += `<div class="bg-green-100 px-4 py-2 rounded mb-2 text-green-800 font-bold">🎓 Expert Level</div>`;
        if (maxStreak >= 5) achievementsHtml += `<div class="bg-orange-100 px-4 py-2 rounded mb-2 text-orange-800 font-bold">🔥 Hot Streak (${maxStreak})</div>`;

        quizApp.innerHTML = `
            <div class="text-center py-6">
                <div class="text-2xl font-bold mb-2">Quiz complete</div>
                <div class="text-5xl font-black text-blue-700 mb-4">${score}/${total}</div>

                ${achievementsHtml ? `<div class="mb-4">${achievementsHtml}</div>` : ""}

                <div id="communityStats" class="mt-6"></div>

                <button id="retryBtn" class="w-full py-3 mb-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition">
                    Try again ↺
                </button>

                <button id="shareBtn" class="w-full py-3 mb-3 bg-yellow-400 text-black rounded-lg font-semibold hover:bg-yellow-500 transition">
                    Share result 📤
                </button>

                <a href="/quizzes" class="block w-full py-3 bg-gray-100 rounded-lg border text-gray-600">
                    More quizzes →
                </a>
            </div>
        `;

        document.getElementById("retryBtn").addEventListener("click", () => location.reload());

        document.getElementById("shareBtn").addEventListener("click", () => {
            const text = `I scored ${score}/${total} in this quiz! 🇺🇸`;
            if (navigator.share) {
                navigator.share({ title: "Quiz Result", text: text, url: window.location.href });
            } else {
                navigator.clipboard.writeText(text + "\n" + window.location.href);
                alert("Result copied to clipboard!");
            }
        });

        if (isPerfect && typeof confetti !== "undefined") {
            confetti({ particleCount: 120, spread: 70, origin: { y: 0.6 } });
        }

        const percentScore = Math.round((score / total) * 100);

        fetch(`/quizzes/@Model.Slug/result`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                mode: "classic",
                score: percentScore,
                questionCount: total,
                timeSpentSeconds: 0
            })
        })
            .then(r => r.ok ? r.json() : Promise.reject(r))
            .then(data => {
                const statsEl = document.getElementById("communityStats");
                if (!statsEl) return;

                statsEl.innerHTML = `
                <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 text-sm text-gray-700">
                    You scored better than <span class="font-bold text-gray-900">${data.percentile}%</span> of players.
                    <div class="mt-1">
                        Average score: <span class="font-semibold text-gray-900">${data.average}%</span>
                    </div>
                </div>
            `;
            })
            .catch(() => {
            });
    }

    loadQuestion();
});
