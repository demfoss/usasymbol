(function () {
    "use strict";

    var card = document.getElementById("fc-card");
    if (!card) return;

    var deck = (window.flashcardData || []).slice();
    var index = 0;
    var correctCount = 0;
    var seenCorrect = {};

    var progressEl = document.getElementById("fc-progress");
    var scoreEl = document.getElementById("fc-score");
    var flagEl = document.getElementById("fc-flag");
    var nameEl = document.getElementById("fc-state-name");
    var capitalEl = document.getElementById("fc-capital");
    var linkEl = document.getElementById("fc-state-link");

    function shuffle(arr) {
        for (var i = arr.length - 1; i > 0; i--) {
            var j = Math.floor(Math.random() * (i + 1));
            var tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
        }
        return arr;
    }

    function renderCard() {
        var entry = deck[index];
        if (!entry) return;

        card.classList.remove("is-flipped");
        flagEl.src = entry.flagUrl;
        flagEl.alt = entry.name + " flag";
        nameEl.textContent = entry.name;
        capitalEl.textContent = entry.capital;
        linkEl.href = "/states/" + entry.slug;

        progressEl.textContent = (index + 1) + " / " + deck.length;
        scoreEl.textContent = "Correct: " + correctCount + " / " + deck.length;
    }

    function goTo(newIndex) {
        index = ((newIndex % deck.length) + deck.length) % deck.length;
        renderCard();
    }

    card.addEventListener("click", function () {
        card.classList.toggle("is-flipped");
    });

    document.getElementById("fc-flip").addEventListener("click", function () {
        card.classList.toggle("is-flipped");
    });

    document.getElementById("fc-prev").addEventListener("click", function () { goTo(index - 1); });
    document.getElementById("fc-next").addEventListener("click", function () { goTo(index + 1); });

    document.getElementById("fc-shuffle").addEventListener("click", function () {
        shuffle(deck);
        correctCount = 0;
        seenCorrect = {};
        goTo(0);
    });

    document.getElementById("fc-correct").addEventListener("click", function () {
        var entry = deck[index];
        if (entry && !seenCorrect[entry.slug]) {
            seenCorrect[entry.slug] = true;
            correctCount++;
        }
        scoreEl.textContent = "Correct: " + correctCount + " / " + deck.length;
        goTo(index + 1);
    });

    renderCard();
})();
