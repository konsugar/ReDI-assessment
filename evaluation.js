// ─── Configuration ────────────────────────────────────────────────────────────
const CONFIG = {
  apiBase: 'http://localhost:5000/api',
  useMock: true   // set to false when backend is ready
};

const QUESTIONS_KEY = 'redi_questions';

function loadStoredQuestions() {
  try {
    const stored = JSON.parse(localStorage.getItem(QUESTIONS_KEY) || 'null');
    if (stored && Array.isArray(stored.topics) && Array.isArray(stored.questions) && Array.isArray(stored.openQuestions)) {
      return stored;
    }
  } catch (_) {}
  return null;
}

// ─── Utils ────────────────────────────────────────────────────────────────────
function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

// ─── State ────────────────────────────────────────────────────────────────────
const state = {
  code: '',
  data: null,
  selfEval: {},   // topicId → rating (1–5)
  mcq: {},        // questionId → selectedIndex
  open: {}        // questionId → text
};

// ─── API ──────────────────────────────────────────────────────────────────────
async function fetchQuestions() {
  if (CONFIG.useMock) {
    return loadStoredQuestions() || window.REDI_DEFAULT_QUESTIONS;
  }

  const res = await fetch(`${CONFIG.apiBase}/questions`);
  if (!res.ok) throw new Error('Failed to load questions');
  return res.json();
}

async function postSubmission(payload) {
  if (CONFIG.useMock) {
    let correct = 0;
    state.data.questions.forEach(q => {
      if (state.mcq[q.id] === q.correctIndex) correct++;
    });
    const total = state.data.questions.length;
    return {
      submissionId: 'MOCK-' + Date.now(),
      participantCode: payload.participantCode,
      score: correct,
      total,
      percentage: total ? Math.round((correct / total) * 100) : 0,
      questionResults: state.data.questions.map(q => ({
        questionId: q.id,
        questionText: q.text,
        options: q.options,
        selectedOption: payload.multipleChoiceAnswers.find(a => a.questionId === q.id)?.selectedOption ?? null,
        correctIndex: q.correctIndex,
        correct: state.mcq[q.id] === q.correctIndex
      })),
      selfEvaluation: payload.selfEvaluation,
      openAnswers: payload.openAnswers
    };
  }

  const res = await fetch(`${CONFIG.apiBase}/submissions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) throw new Error('Failed to submit evaluation');
  return res.json();
}

// ─── Navigation ───────────────────────────────────────────────────────────────
function showSection(id) {
  document.querySelectorAll('.eval-section').forEach(s => s.classList.add('hidden'));
  document.getElementById(id).classList.remove('hidden');
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function showPart(id) {
  document.querySelectorAll('.eval-part').forEach(p => p.classList.add('hidden'));
  document.getElementById(id).classList.remove('hidden');
  window.scrollTo({ top: 0, behavior: 'smooth' });
  updateProgressSteps(id);
}

function updateProgressSteps(partId) {
  const stepMap = { 'part-a': 1, 'part-b': 2, 'part-c': 3 };
  const current = stepMap[partId];
  document.querySelectorAll('.progress-step').forEach((step, i) => {
    step.classList.remove('active', 'completed');
    if (i + 1 === current) step.classList.add('active');
    if (i + 1 < current) step.classList.add('completed');
  });
}

// ─── Render ───────────────────────────────────────────────────────────────────
function renderSelfEval(topics) {
  const container = document.getElementById('self-eval-container');
  container.innerHTML = topics.map(topic => `
    <div class="self-eval-topic">
      <div class="topic-header">
        <span class="topic-name">${escapeHtml(topic.name)}</span>
        <span class="topic-desc">${escapeHtml(topic.description)}</span>
      </div>
      <div class="rating-group" data-topic-id="${topic.id}">
        ${[1,2,3,4,5].map(n => `
          <label class="rating-option">
            <input type="radio" name="topic-${topic.id}" value="${n}">
            <span class="rating-btn">${n}</span>
          </label>
        `).join('')}
      </div>
    </div>
  `).join('');

  container.querySelectorAll('input[type="radio"]').forEach(input => {
    input.addEventListener('change', () => {
      const topicId = parseInt(input.closest('.rating-group').dataset.topicId);
      const rating = parseInt(input.value);
      state.selfEval[topicId] = rating;
      input.closest('.rating-group').querySelectorAll('.rating-btn').forEach((btn, i) => {
        btn.classList.toggle('selected', i < rating);
      });
    });
  });
}

function renderMCQ(questions) {
  const container = document.getElementById('mcq-container');
  container.innerHTML = questions.map((q, qi) => `
    <div class="mcq-question" data-question-id="${q.id}">
      <p class="question-text"><span class="question-num">${qi + 1}.</span> ${escapeHtml(q.text)}</p>
      <div class="options-group">
        ${q.options.map((opt, oi) => `
          <label class="option-label">
            <input type="radio" name="mcq-${q.id}" value="${oi}">
            <span class="option-text">${escapeHtml(opt)}</span>
          </label>
        `).join('')}
      </div>
    </div>
  `).join('');

  container.querySelectorAll('input[type="radio"]').forEach(input => {
    input.addEventListener('change', () => {
      const questionId = parseInt(input.closest('.mcq-question').dataset.questionId);
      state.mcq[questionId] = parseInt(input.value);
    });
  });
}

function renderOpenQuestions(openQuestions) {
  const container = document.getElementById('open-container');
  container.innerHTML = openQuestions.map((q, qi) => `
    <div class="open-question">
      <label class="open-label" for="open-${q.id}">
        <span class="question-num">${qi + 1}.</span> ${escapeHtml(q.text)}
      </label>
      <textarea
        id="open-${q.id}"
        class="open-textarea"
        data-question-id="${q.id}"
        placeholder="Write your answer here..."
        rows="5"
      ></textarea>
    </div>
  `).join('');

  container.querySelectorAll('textarea').forEach(ta => {
    ta.addEventListener('input', () => {
      state.open[parseInt(ta.dataset.questionId)] = ta.value;
    });
  });
}

function renderResults(results) {
  const container = document.getElementById('results-container');
  container.innerHTML = `
    <div class="results-header">
      <div class="results-check">✓</div>
      <h2>Thank You!</h2>
      <p class="results-code">Your evaluation has been submitted.</p>
      <p class="results-code">Participant Code: <strong>${escapeHtml(results.participantCode)}</strong></p>
    </div>
  `;
}

// ─── Validation ───────────────────────────────────────────────────────────────
function validateSelfEval() {
  const missing = state.data.topics.filter(t => !state.selfEval[t.id]);
  if (missing.length > 0) {
    alert(`Please rate all topics before continuing.\nMissing: ${missing.map(t => t.name).join(', ')}`);
    return false;
  }
  return true;
}

function validateMCQ() {
  const missing = state.data.questions.filter(q => state.mcq[q.id] === undefined);
  if (missing.length > 0) {
    alert(`Please answer all ${missing.length} remaining question(s) before continuing.`);
    return false;
  }
  return true;
}

// ─── Local Storage ────────────────────────────────────────────────────────────
function saveSubmissionLocally(results) {
  const key = 'redi_submissions';
  const existing = JSON.parse(localStorage.getItem(key) || '[]');
  existing.push({ ...results, timestamp: new Date().toISOString() });
  localStorage.setItem(key, JSON.stringify(existing));
}

// ─── Submit ───────────────────────────────────────────────────────────────────
async function handleSubmit() {
  const btn = document.getElementById('btn-submit');
  btn.disabled = true;
  btn.textContent = 'Submitting…';

  const payload = {
    participantCode: state.code,
    selfEvaluation: state.data.topics.map(t => ({
      topicId: t.id,
      topicName: t.name,
      rating: state.selfEval[t.id] ?? 0
    })),
    multipleChoiceAnswers: state.data.questions.map(q => ({
      questionId: q.id,
      selectedOption: state.mcq[q.id] ?? null
    })),
    openAnswers: state.data.openQuestions.map(q => ({
      questionId: q.id,
      questionText: q.text,
      text: state.open[q.id] ?? ''
    }))
  };

  try {
    const results = await postSubmission(payload);
    saveSubmissionLocally(results);
    renderResults(results);
    showSection('section-results');
  } catch (err) {
    console.error(err);
    alert('Something went wrong submitting your evaluation. Please try again.');
    btn.disabled = false;
    btn.textContent = 'Submit Evaluation';
  }
}

// ─── Init ─────────────────────────────────────────────────────────────────────
async function init() {
  try {
    state.data = await fetchQuestions();
  } catch (err) {
    alert('Failed to load the evaluation. Please refresh the page.');
    return;
  }

  // Login
  const codeInput = document.getElementById('participant-code');
  const codeError = document.getElementById('code-error');

  document.getElementById('login-form').addEventListener('submit', e => {
    e.preventDefault();
    const code = codeInput.value.trim();
    if (!code) {
      codeError.classList.remove('hidden');
      codeInput.focus();
      return;
    }
    codeError.classList.add('hidden');
    state.code = code;

    renderSelfEval(state.data.topics);
    renderMCQ(state.data.questions);
    renderOpenQuestions(state.data.openQuestions);

    showSection('section-test');
    showPart('part-a');
  });

  // Part navigation
  document.getElementById('btn-to-mcq').addEventListener('click', () => {
    if (validateSelfEval()) showPart('part-b');
  });
  document.getElementById('btn-back-to-self').addEventListener('click', () => showPart('part-a'));
  document.getElementById('btn-to-open').addEventListener('click', () => {
    if (validateMCQ()) showPart('part-c');
  });
  document.getElementById('btn-back-to-mcq').addEventListener('click', () => showPart('part-b'));
  document.getElementById('btn-submit').addEventListener('click', handleSubmit);
}

init();
