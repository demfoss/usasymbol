// Quiz questions database
const quizQuestions = [
    {
        question: "What is the state bird of California?",
        hint: "🐦 This bird is known for its distinctive call and colorful plumage",
        answers: ["California Quail", "Bald Eagle", "Blue Jay", "Cardinal"],
        correct: 0,
        explanation: "The California Quail has been California's state bird since 1931!"
    },
    {
        question: "What is the capital of Texas?",
        hint: "🏛️ This city is also known for its live music scene",
        answers: ["Houston", "Dallas", "Austin", "San Antonio"],
        correct: 2,
        explanation: "Austin has been the capital of Texas since 1839."
    },
    {
        question: "What is the state flower of New York?",
        hint: "🌹 This flower shares its name with a precious gem",
        answers: ["Rose", "Daisy", "Tulip", "Violet"],
        correct: 0,
        explanation: "The Rose became New York's state flower in 1955."
    },
    {
        question: "What is the state tree of Florida?",
        hint: "🌴 This tree is perfect for the tropical climate",
        answers: ["Oak", "Sabal Palm", "Pine", "Magnolia"],
        correct: 1,
        explanation: "The Sabal Palm (Cabbage Palm) is Florida's state tree since 1953."
    },
    {
        question: "What is the nickname of Illinois?",
        hint: "🌾 This nickname relates to President Lincoln",
        answers: ["The Sunshine State", "The Prairie State", "The Garden State", "The Bay State"],
        correct: 1,
        explanation: "Illinois is known as 'The Prairie State' and 'Land of Lincoln'."
    },
    {
        question: "What is the state bird of Texas?",
        hint: "🦜 This colorful bird can mimic many sounds",
        answers: ["Mockingbird", "Cardinal", "Blue Jay", "Robin"],
        correct: 0,
        explanation: "The Northern Mockingbird became Texas's state bird in 1927."
    },
    {
        question: "What is the capital of California?",
        hint: "🏛️ This city is located in the Central Valley",
        answers: ["Los Angeles", "San Francisco", "Sacramento", "San Diego"],
        correct: 2,
        explanation: "Sacramento has been California's capital since 1854."
    },
    {
        question: "What is the state animal of California?",
        hint: "🐻 This large mammal is featured on the state flag",
        answers: ["California Grizzly Bear", "Mountain Lion", "Gray Wolf", "Black Bear"],
        correct: 0,
        explanation: "The California Grizzly Bear is the state animal, though sadly extinct in California since 1922."
    },
    {
        question: "What is Florida's state flower?",
        hint: "🍊 This flower comes from a citrus tree",
        answers: ["Hibiscus", "Orange Blossom", "Jasmine", "Magnolia"],
        correct: 1,
        explanation: "Orange Blossom became Florida's state flower in 1909."
    },
    {
        question: "What is the state motto of New York?",
        hint: "📜 Latin phrase meaning 'Ever Upward'",
        answers: ["E Pluribus Unum", "Excelsior", "Liberty and Prosperity", "In God We Trust"],
        correct: 1,
        explanation: "'Excelsior' has been New York's motto since 1778!"
    }
];

// Quiz state
let currentQuestionIndex = 0;
let score = 0;
let totalAnswered = 0;
let selectedAnswer = null;
let usedQuestions = [];

// Get random question that hasn't been used
function getRandomQuestion() {
    if (usedQuestions.length === quizQuestions.length) {
        usedQuestions = []; // Reset if all questions used
    }

    let availableQuestions = quizQuestions.filter((q, index) => !usedQuestions.includes(index));
    let randomIndex = Math.floor(Math.random() * availableQuestions.length);
    let questionIndex = quizQuestions.indexOf(availableQuestions[randomIndex]);

    usedQuestions.push(questionIndex);
    return questionIndex;
}

// Load question
function loadQuestion() {
    currentQuestionIndex = getRandomQuestion();
    const question = quizQuestions[currentQuestionIndex];

    document.getElementById('question').textContent = question.question;
    document.getElementById('questionHint').textContent = question.hint;
    document.getElementById('questionNum').textContent = totalAnswered + 1;

    const answersContainer = document.getElementById('answers');
    answersContainer.innerHTML = '';

    question.answers.forEach((answer, index) => {
        const button = document.createElement('button');
        button.className = 'answer-btn bg-white border-2 border-gray-200 p-4 rounded-lg text-left hover:border-blue-500 hover:bg-blue-50 font-medium transition';
        button.textContent = answer;
        button.onclick = () => selectAnswer(index);
        answersContainer.appendChild(button);
    });

    document.getElementById('feedbackText').textContent = '';
    document.getElementById('nextBtn').disabled = true;
    selectedAnswer = null;

    // Add fade-in animation
    answersContainer.querySelectorAll('.answer-btn').forEach((btn, i) => {
        setTimeout(() => {
            btn.style.opacity = '0';
            btn.style.transform = 'translateY(10px)';
            setTimeout(() => {
                btn.style.transition = 'all 0.3s ease';
                btn.style.opacity = '1';
                btn.style.transform = 'translateY(0)';
            }, 50);
        }, i * 100);
    });
}

// Select answer
function selectAnswer(index) {
    if (selectedAnswer !== null) return; // Prevent multiple selections

    selectedAnswer = index;
    const question = quizQuestions[currentQuestionIndex];
    const buttons = document.querySelectorAll('.answer-btn');
    const feedbackText = document.getElementById('feedbackText');

    totalAnswered++;
    document.getElementById('total').textContent = totalAnswered;

    if (index === question.correct) {
        score++;
        document.getElementById('score').textContent = score;
        buttons[index].classList.add('correct');
        feedbackText.innerHTML = `<span class="text-green-600">✓ Correct! ${question.explanation}</span>`;
    } else {
        buttons[index].classList.add('incorrect');
        buttons[question.correct].classList.add('correct');
        feedbackText.innerHTML = `<span class="text-red-600">✗ Incorrect. ${question.explanation}</span>`;
    }

    // Disable all buttons
    buttons.forEach(btn => {
        btn.style.pointerEvents = 'none';
    });

    document.getElementById('nextBtn').disabled = false;
}

// Next question
document.getElementById('nextBtn').addEventListener('click', () => {
    loadQuestion();
});

// Initialize quiz
document.addEventListener('DOMContentLoaded', function () {
    loadQuestion();
});