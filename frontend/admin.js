const CONFIG = {
    apiBase: window.REDI_API_BASE || 'https://localhost:7101/api',
    authTokenKey: 'redi_admin_token'
};

function getAdminToken() {
    return sessionStorage.getItem(CONFIG.authTokenKey);
}

function setAdminToken(token) {
    sessionStorage.setItem(CONFIG.authTokenKey, token);
}

function clearAdminToken() {
    sessionStorage.removeItem(CONFIG.authTokenKey);
}

async function authFetch(url, options = {}) {
    const token = getAdminToken();

    const headers = {
        ...(options.headers || {})
    };

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const res = await fetch(url, {
        ...options,
        headers
    });

    if (res.status === 401 || res.status === 403) {
        clearAdminToken();
        showLoginScreen();
        throw new Error('Unauthorized');
    }

    return res;
}

async function loginAdmin(username, password) {
    const res = await fetch(`${CONFIG.apiBase}/admin/auth/login`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username,
            password
        })
    });

    if (!res.ok) {
        throw new Error('Invalid username or password');
    }

    const data = await res.json();

    if (!data.accessToken) {
        throw new Error('Login response does not contain accessToken');
    }

    setAdminToken(data.accessToken);
}


function initLogin() {
    const form = document.getElementById('admin-login-form');
    const error = document.getElementById('admin-login-error');

    form.addEventListener('submit', async e => {
        e.preventDefault();

        const username = document.getElementById('admin-username').value.trim();
        const password = document.getElementById('admin-password').value;

        try {
            await loginAdmin(username, password);
            error.classList.add('hidden');
            showAdminApp();
            await initAdminData().catch(reportError);
        } catch (err) {
            error.textContent = 'Login failed. Please check your credentials.';
            error.classList.remove('hidden');
        }
    });

    document.getElementById('btn-logout').addEventListener('click', logoutAdmin);
}

function showLoginScreen() {
    document.getElementById('admin-login-screen').classList.remove('hidden');
    document.getElementById('admin-app').classList.add('hidden');
    document.getElementById('admin-tabs-nav').classList.add('hidden');
}

function showAdminApp() {
    document.getElementById('admin-login-screen').classList.add('hidden');
    document.getElementById('admin-app').classList.remove('hidden');
    document.getElementById('admin-tabs-nav').classList.remove('hidden');
}

function logoutAdmin() {
    clearAdminToken();
    showLoginScreen();

    document.getElementById('table-body').innerHTML = '';
    document.getElementById('detail-container').innerHTML = '';
    document.getElementById('section-table').classList.add('hidden');
    document.getElementById('section-detail').classList.add('hidden');
    document.getElementById('empty-state').classList.add('hidden');
}

async function initApp() {
    initLogin();

    if (!getAdminToken()) {
        showLoginScreen();
        return;
    }

    showAdminApp();
    await initAdminData();
}

//
// main admin logic
//

let selectedAssessmentId = null;
let bankVersion = null;
let bankMetadata = null;
let bankBusy = false;
let savedQuestions = null;
let selectionRevision = 0;

function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

async function api(path, options = {}) {
  const res = await authFetch(`${CONFIG.apiBase}${path}`, {
    ...options, headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }
  });
  if (!res.ok) {
    const text = await res.text();
    let message = text;
    try { const data = JSON.parse(text); message = data.message || data.title || text; } catch (_) {}
    throw new Error(message || `Request failed (${res.status})`);
  }
  return res.status === 204 ? null : res.json();
}

async function loadSubmissions() {
  return api(`/assessments/${selectedAssessmentId}/submissions/details`);
}

function reportError(err) {
  document.getElementById('admin-status').textContent = err.message;
}

async function refreshSubmissions() {
  const assessmentId = selectedAssessmentId;
  const submissions = await loadSubmissions();
  if (assessmentId !== selectedAssessmentId) return submissions;
  renderTable(submissions);
  document.getElementById('submission-count').textContent = submissions.length;
  document.getElementById('empty-state').classList.toggle('hidden', submissions.length > 0);
  return submissions;
}

// ─── Tabs ─────────────────────────────────────────────────────────────────────
function activateTab(name) {
  document.querySelectorAll('.admin-tab').forEach(t => {
    const active = t.dataset.tab === name;
    t.classList.toggle('active', active);
    t.setAttribute('aria-selected', active ? 'true' : 'false');
  });
  document.querySelectorAll('.admin-tab-panel').forEach(p => p.classList.add('hidden'));
  document.getElementById(`tab-${name}`).classList.remove('hidden');
  if (name === 'submissions') showTable();
}

// ─── Submissions ─────────────────────────────────────────────────────────────
function showTable() {
  document.getElementById('section-detail').classList.add('hidden');
  document.getElementById('section-table').classList.remove('hidden');
}

function showDetail(submission) {
  document.getElementById('section-table').classList.add('hidden');
  renderDetail(submission);
  document.getElementById('section-detail').classList.remove('hidden');
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function formatDate(iso) {
  const d = new Date(iso);
  return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function selfRatingSummary(selfEvaluation) {
  if (!selfEvaluation?.length) return '—';
  const avg = selfEvaluation.reduce((s, e) => s + e.rating, 0) / selfEvaluation.length;
  return `avg ${avg.toFixed(1)}/5`;
}

function renderTable(submissions) {
  const tbody = document.getElementById('table-body');
  tbody.innerHTML = submissions.map((s, i) => `
    <tr>
      <td>${i + 1}</td>
      <td><strong>${escapeHtml(s.participantCode)}</strong></td>
      <td>${escapeHtml(formatDate(s.timestamp))}</td>
      <td>${escapeHtml(String(s.score))}/${escapeHtml(String(s.total))} <span class="pct">(${escapeHtml(String(s.percentage))}%)</span></td>
      <td>${escapeHtml(selfRatingSummary(s.selfEvaluation))}</td>
      <td><button class="btn-view" data-index="${i}">View →</button></td>
    </tr>
  `).join('');

  tbody.querySelectorAll('.btn-view').forEach(btn => {
    btn.addEventListener('click', () => showDetail(submissions[parseInt(btn.dataset.index)]));
  });
}

function renderDetail(s) {
  const selfHtml = (s.selfEvaluation || []).map(se => `
    <div class="detail-row">
      <span class="detail-label">${escapeHtml(se.topicName || `Topic ${se.topicId}`)}</span>
      <span class="detail-value">${escapeHtml(String(se.rating))}/5</span>
    </div>
  `).join('');

  const mcqHtml = (s.questionResults || []).map((r, i) => {
    const selectedText = r.selectedOption !== null && r.selectedOption !== undefined
      ? r.options[r.selectedOption]
      : 'Not answered';
    return `
      <div class="detail-mcq ${r.correct ? 'correct' : 'incorrect'}">
        <span class="result-icon">${r.correct ? '✓' : '✗'}</span>
        <div>
          <p class="detail-q">${i + 1}. ${escapeHtml(r.questionText)}</p>
          <p class="detail-answer">Student: <strong>${escapeHtml(selectedText)}</strong></p>
          ${!r.correct ? `<p class="detail-correct">Correct: <strong>${escapeHtml(r.options[r.correctIndex])}</strong></p>` : ''}
        </div>
      </div>
    `;
  }).join('');

  const openHtml = (s.openAnswers || []).map((oa, i) => {
    const savedNote = oa.teacherNote || '';
    const prompt = oa.questionText || `Question ${oa.questionId}`;
    const answer = oa.text
      ? escapeHtml(oa.text)
      : '<em>No answer provided</em>';
    return `
      <div class="detail-open">
        <p class="detail-q">${i + 1}. ${escapeHtml(prompt)}</p>
        <div class="detail-open-answer">${answer}</div>
        <div class="teacher-note">
          <label>Teacher notes:</label>
          <textarea
            placeholder="Add evaluation notes here…"
            rows="2"
            maxlength="2000" data-answer-id="${oa.attemptQuestionId}"
          >${escapeHtml(savedNote)}</textarea>
          <label>Evaluation
            <select class="answer-evaluation">
              <option value="" ${oa.isCorrect == null ? 'selected' : ''}>Not reviewed</option>
              <option value="true" ${oa.isCorrect === true ? 'selected' : ''}>Correct</option>
              <option value="false" ${oa.isCorrect === false ? 'selected' : ''}>Incorrect</option>
            </select>
          </label>
          <button class="btn-save-review" type="button">Save review</button>
          <span class="review-status" role="status"></span>
        </div>
      </div>
    `;
  }).join('');

  const container = document.getElementById('detail-container');
  container.innerHTML = `
    <div class="detail-card">
      <h2>${escapeHtml(s.participantCode)}</h2>
      <p class="detail-meta">Submitted: ${escapeHtml(formatDate(s.timestamp))}</p>

      <div class="detail-section">
        <h3>Score</h3>
        <p class="score-big">${escapeHtml(String(s.score))}/${escapeHtml(String(s.total))} (${escapeHtml(String(s.percentage))}%)</p>
      </div>

      <div class="detail-section">
        <h3>Self-Evaluation</h3>
        ${selfHtml}
      </div>

      <div class="detail-section">
        <h3>Knowledge Test</h3>
        ${mcqHtml}
      </div>

      <div class="detail-section">
        <h3>Written Answers</h3>
        ${openHtml}
      </div>
    </div>
  `;

  container.querySelectorAll('.teacher-note').forEach(card => {
    const textarea = card.querySelector('textarea');
    const select = card.querySelector('select');
    const button = card.querySelector('button');
    const status = card.querySelector('.review-status');
    const dirty = () => { status.textContent = 'Unsaved changes'; };
    textarea.addEventListener('input', dirty);
    select.addEventListener('change', dirty);
    button.addEventListener('click', async () => {
      button.disabled = true;
      textarea.disabled = select.disabled = true;
      status.textContent = 'Saving…';
      const path = `/assessments/${selectedAssessmentId}/submissions/${s.attemptId}/open-answers/${textarea.dataset.answerId}`;
      try {
        await api(`${path}/teacher-note`, { method: 'PUT', body: JSON.stringify({ teacherNote: textarea.value }) });
        await api(`${path}/evaluation`, { method: 'PUT', body: JSON.stringify({ isCorrect: select.value === '' ? null : select.value === 'true' }) });
        status.textContent = 'Saved to database';
        await refreshSubmissions();
      } catch (err) { status.textContent = `Save incomplete: ${err.message}. Retry to save both fields.`; }
      finally { button.disabled = textarea.disabled = select.disabled = false; }
    });
  });
}

// ─── CSV export ──────────────────────────────────────────────────────────────
function csvCell(v) {
  const s = v === null || v === undefined ? '' : String(v);
  return `"${s.replace(/"/g, '""')}"`;
}

function exportCSV(submissions) {
  if (!submissions.length) return;
  const rows = [['Participant Code', 'Timestamp', 'Score', 'Total Questions', 'Score %', 'Self-Eval Avg']];
  submissions.forEach(s => {
    const avg = s.selfEvaluation?.length
      ? (s.selfEvaluation.reduce((a, e) => a + e.rating, 0) / s.selfEvaluation.length).toFixed(1)
      : '';
    rows.push([s.participantCode, s.timestamp, s.score, s.total, s.percentage, avg]);
  });
  downloadCsv(rows, `redi_results_summary_${new Date().toISOString().slice(0,10)}.csv`);
}

function exportDetailedCSV(submissions) {
  if (!submissions.length) return;
  const rows = [['Participant Code', 'Timestamp', 'Section', 'Item', 'Answer', 'Correct', 'Teacher Notes']];
  submissions.forEach(s => {
    (s.selfEvaluation || []).forEach(se => {
      rows.push([s.participantCode, s.timestamp, 'Self-Evaluation', se.topicName || `Topic ${se.topicId}`, `${se.rating}/5`, '', '']);
    });
    (s.questionResults || []).forEach(r => {
      const selected = r.selectedOption !== null && r.selectedOption !== undefined
        ? r.options[r.selectedOption]
        : 'Not answered';
      rows.push([s.participantCode, s.timestamp, 'Multiple Choice', r.questionText, selected, r.correct ? 'yes' : 'no', '']);
    });
    (s.openAnswers || []).forEach(oa => {
      rows.push([s.participantCode, s.timestamp, 'Open Answer', oa.questionText || `Question ${oa.questionId}`, oa.text || '', oa.isCorrect == null ? '' : (oa.isCorrect ? 'yes' : 'no'), oa.teacherNote || '']);
    });
  });
  downloadCsv(rows, `redi_results_detailed_${new Date().toISOString().slice(0,10)}.csv`);
}

function downloadCsv(rows, filename) {
  const csv = rows.map(r => r.map(csvCell).join(',')).join('\n');
  const blob = new Blob([csv], { type: 'text/csv' });
  triggerDownload(blob, filename);
}

function triggerDownload(blob, filename) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

// ─── Question bank ───────────────────────────────────────────────────────────
async function loadQuestions() {
  const assessmentId = selectedAssessmentId;
  const result = await api(`/assessments/${assessmentId}/question-bank`);
  if (assessmentId !== selectedAssessmentId) return;
  acceptBank(result);
}

function acceptBank(result) {
  bankVersion = result.version;
  bankMetadata = result.bank;
  questions = { topics: result.bank.topics, questions: result.bank.questions, openQuestions: result.bank.openQuestions };
  savedQuestions = structuredClone(questions);
  editing.topic = editing.mcq = editing.open = null;
  updateBankStatus();
}

function updateBankStatus() {
  for (const id of ['assessment-select', 'btn-new-assessment', 'btn-clone', 'btn-publication', 'btn-reload-bank', 'btn-export-questions']) document.getElementById(id).disabled = bankBusy;
  const locked = bankMetadata.isPublished || bankMetadata.attemptCount > 0 || bankMetadata.hasUnsupportedQuestions;
  document.getElementById('question-editor').disabled = locked || bankBusy;
  document.getElementById('bank-status').textContent = bankMetadata.hasUnsupportedQuestions
    ? 'This test contains question types this editor does not support. Its content is preserved.'
    : bankMetadata.attemptCount > 0 ? 'Answers have already been submitted. Clone this test to edit a new draft.'
    : bankMetadata.isPublished ? 'Published. Unpublish before editing.' : 'Draft. Save changes, then publish to accept answers.';
  document.getElementById('btn-publication').textContent = bankMetadata.isPublished ? 'Unpublish' : 'Publish';
  document.getElementById('student-link').href = `evaluation.html?assessmentId=${selectedAssessmentId}`;
  document.getElementById('student-link').classList.toggle('hidden', !bankMetadata.isPublished);
}

async function saveQuestions(data) {
  if (bankBusy) return false;
  bankBusy = true;
  updateBankStatus();
  document.getElementById('admin-status').textContent = 'Saving question bank…';
  try {
    const result = await api(`/assessments/${selectedAssessmentId}/question-bank`, {
      method: 'PUT', body: JSON.stringify({ ...data, version: bankVersion })
    });
    acceptBank(result);
    renderQuestionEditor();
    document.getElementById('admin-status').textContent = 'Question bank saved to database.';
    return true;
  } catch (err) { reportError(err); return false; }
  finally { bankBusy = false; updateBankStatus(); }
}

function cloneDefaults() {
  return JSON.parse(JSON.stringify(window.REDI_DEFAULT_QUESTIONS));
}

function nextId(items, fallback) {
  if (!items.length) return fallback;
  return Math.max(...items.map(i => Number(i.id) || 0)) + 1;
}

let questions = { topics: [], questions: [], openQuestions: [] };
const editing = { topic: null, mcq: null, open: null };

function renderQuestionEditor() {
  renderTopicsList();
  renderMcqList();
  renderOpenList();
}

function renderTopicsList() {
  const list = document.getElementById('topics-list');
  if (!questions.topics.length) {
    list.innerHTML = '<p class="editor-empty">No topics yet. Click "+ Add Topic" to create one.</p>';
    return;
  }
  list.innerHTML = questions.topics.map(t => {
    if (editing.topic === t.id) return topicForm(t);
    return `
      <div class="editor-card">
        <div class="editor-card-body">
          <div class="editor-title">${escapeHtml(t.name || '(unnamed)')}</div>
          <div class="editor-sub">${escapeHtml(t.description || '')}</div>
        </div>
        <div class="editor-card-actions">
          <button class="btn-edit" data-id="${t.id}">Edit</button>
          <button class="btn-delete" data-id="${t.id}">Delete</button>
        </div>
      </div>
    `;
  }).join('');
  bindTopicEvents();
}

function topicForm(t) {
  return `
    <div class="editor-card editing">
      <div class="editor-form">
        <label>Topic name
          <input type="text" class="topic-name-input" value="${escapeHtml(t.name || '')}" placeholder="e.g. HTML">
        </label>
        <label>Description
          <input type="text" class="topic-desc-input" value="${escapeHtml(t.description || '')}" placeholder="Short description">
        </label>
        <div class="editor-form-actions">
          <button class="btn-save" data-id="${t.id}">Save</button>
          <button class="btn-cancel" data-id="${t.id}">Cancel</button>
        </div>
      </div>
    </div>
  `;
}

function bindTopicEvents() {
  const list = document.getElementById('topics-list');
  list.querySelectorAll('.btn-edit').forEach(b => {
    b.addEventListener('click', async () => { if (Object.values(editing).some(id => id !== null)) return; editing.topic = Number(b.dataset.id); renderTopicsList(); });
  });
  list.querySelectorAll('.btn-delete').forEach(b => {
    b.addEventListener('click', async () => {
      if (Object.values(editing).some(id => id !== null)) { alert('Save or cancel the current edit first.'); return; }
      if (confirm('Delete this topic?')) {
        const candidate = structuredClone(questions);
        candidate.topics = candidate.topics.filter(t => t.id !== Number(b.dataset.id));
        if (!await saveQuestions(candidate)) return;
        renderTopicsList();
      }
    });
  });
  list.querySelectorAll('.btn-cancel').forEach(b => {
    b.addEventListener('click', () => {
      questions = structuredClone(savedQuestions);
      editing.topic = editing.mcq = editing.open = null;
      renderQuestionEditor();
    });
  });
  list.querySelectorAll('.btn-save').forEach(b => {
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.id);
      const card = b.closest('.editor-card');
      const name = card.querySelector('.topic-name-input').value.trim();
      const desc = card.querySelector('.topic-desc-input').value.trim();
      if (!name) { alert('Topic name is required.'); return; }
      const t = questions.topics.find(x => x.id === id);
      t.name = name;
      t.description = desc;
      if (!await saveQuestions(questions)) return;
      editing.topic = null;
      renderTopicsList();
    });
  });
}

function renderMcqList() {
  const list = document.getElementById('mcq-list');
  if (!questions.questions.length) {
    list.innerHTML = '<p class="editor-empty">No multiple choice questions yet. Click "+ Add Multiple Choice Question" to create one.</p>';
    return;
  }
  list.innerHTML = questions.questions.map((q, idx) => {
    if (editing.mcq === q.id) return mcqForm(q);
    const opts = q.options.map((opt, i) =>
      `<li class="${i === q.correctIndex ? 'correct-option' : ''}">${i === q.correctIndex ? '✓ ' : ''}${escapeHtml(opt)}</li>`
    ).join('');
    return `
      <div class="editor-card">
        <div class="editor-card-body">
          <div class="editor-title">${idx + 1}. ${escapeHtml(q.text)}</div>
          <ul class="editor-options">${opts}</ul>
        </div>
        <div class="editor-card-actions">
          <button class="btn-edit" data-id="${q.id}">Edit</button>
          <button class="btn-delete" data-id="${q.id}">Delete</button>
        </div>
      </div>
    `;
  }).join('');
  bindMcqEvents();
}

function mcqForm(q) {
  const options = q.options.length ? q.options : ['', '', '', ''];
  while (options.length < 2) options.push('');
  const optionRows = options.map((opt, i) => `
    <div class="mcq-option-row">
      <input type="radio" name="mcq-correct-${q.id}" value="${i}" ${i === q.correctIndex ? 'checked' : ''}>
      <input type="text" class="mcq-option-input" data-index="${i}" value="${escapeHtml(opt)}" placeholder="Option ${i + 1}">
      <button class="btn-remove-option" data-index="${i}" ${options.length <= 2 ? 'disabled' : ''} title="Remove option">×</button>
    </div>
  `).join('');
  return `
    <div class="editor-card editing">
      <div class="editor-form">
        <label>Question text
          <textarea class="mcq-text-input" rows="2" placeholder="Type the question…">${escapeHtml(q.text || '')}</textarea>
        </label>
        <label>Options <span class="editor-hint">(mark the correct one)</span>
          <div class="mcq-options-edit" data-id="${q.id}">${optionRows}</div>
        </label>
        <button class="btn-add-option" data-id="${q.id}">+ Add option</button>
        <div class="editor-form-actions">
          <button class="btn-save" data-id="${q.id}">Save</button>
          <button class="btn-cancel" data-id="${q.id}">Cancel</button>
        </div>
      </div>
    </div>
  `;
}

function readMcqForm(card, id) {
  const text = card.querySelector('.mcq-text-input').value.trim();
  const options = Array.from(card.querySelectorAll('.mcq-option-input')).map(i => i.value.trim());
  const correctRadio = card.querySelector(`input[name="mcq-correct-${id}"]:checked`);
  const correctIndex = correctRadio ? Number(correctRadio.value) : 0;
  return { text, options, correctIndex };
}

function bindMcqEvents() {
  const list = document.getElementById('mcq-list');
  list.querySelectorAll('.btn-edit').forEach(b => {
    b.addEventListener('click', async () => { if (Object.values(editing).some(id => id !== null)) return; editing.mcq = Number(b.dataset.id); renderMcqList(); });
  });
  list.querySelectorAll('.btn-delete').forEach(b => {
    b.addEventListener('click', async () => {
      if (Object.values(editing).some(id => id !== null)) { alert('Save or cancel the current edit first.'); return; }
      if (confirm('Delete this question?')) {
        const candidate = structuredClone(questions);
        candidate.questions = candidate.questions.filter(q => q.id !== Number(b.dataset.id));
        if (!await saveQuestions(candidate)) return;
        renderMcqList();
      }
    });
  });
  list.querySelectorAll('.btn-cancel').forEach(b => {
    b.addEventListener('click', () => {
      questions = structuredClone(savedQuestions);
      editing.topic = editing.mcq = editing.open = null;
      renderQuestionEditor();
    });
  });
  list.querySelectorAll('.btn-save').forEach(b => {
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.id);
      const card = b.closest('.editor-card');
      const { text, options, correctIndex } = readMcqForm(card, id);
      if (!text) { alert('Question text is required.'); return; }
      if (!options[correctIndex]) { alert('The correct option must have text.'); return; }
      const kept = options.map((opt, i) => ({ opt, i })).filter(x => x.opt);
      if (kept.length < 2) { alert('At least two options are required.'); return; }
      const trimmedOptions = kept.map(x => x.opt);
      const newCorrectIndex = kept.findIndex(x => x.i === correctIndex);
      const q = questions.questions.find(x => x.id === id);
      q.text = text;
      q.options = trimmedOptions;
      q.correctIndex = newCorrectIndex;
      if (!await saveQuestions(questions)) return;
      editing.mcq = null;
      renderMcqList();
    });
  });
  list.querySelectorAll('.btn-add-option').forEach(b => {
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.id);
      const card = b.closest('.editor-card');
      const q = questions.questions.find(x => x.id === id);
      const current = readMcqForm(card, id);
      q.text = current.text;
      q.options = [...current.options, ''];
      q.correctIndex = current.correctIndex;
      renderMcqList();
    });
  });
  list.querySelectorAll('.btn-remove-option').forEach(b => {
    b.addEventListener('click', async () => {
      const idx = Number(b.dataset.index);
      const card = b.closest('.editor-card');
      const id = Number(card.querySelector('.btn-save').dataset.id);
      const q = questions.questions.find(x => x.id === id);
      const current = readMcqForm(card, id);
      if (current.options.length <= 2) return;
      q.text = current.text;
      q.options = current.options.filter((_, i) => i !== idx);
      q.correctIndex = current.correctIndex >= q.options.length ? 0 : (current.correctIndex > idx ? current.correctIndex - 1 : current.correctIndex);
      renderMcqList();
    });
  });
}

function renderOpenList() {
  const list = document.getElementById('open-list');
  if (!questions.openQuestions.length) {
    list.innerHTML = '<p class="editor-empty">No open questions yet. Click "+ Add Open Question" to create one.</p>';
    return;
  }
  list.innerHTML = questions.openQuestions.map((q, idx) => {
    if (editing.open === q.id) return openForm(q);
    return `
      <div class="editor-card">
        <div class="editor-card-body">
          <div class="editor-title">${idx + 1}. ${escapeHtml(q.text || '(empty)')}</div>
        </div>
        <div class="editor-card-actions">
          <button class="btn-edit" data-id="${q.id}">Edit</button>
          <button class="btn-delete" data-id="${q.id}">Delete</button>
        </div>
      </div>
    `;
  }).join('');
  bindOpenEvents();
}

function openForm(q) {
  return `
    <div class="editor-card editing">
      <div class="editor-form">
        <label>Question text
          <textarea class="open-text-input" rows="3" placeholder="Type the open question…">${escapeHtml(q.text || '')}</textarea>
        </label>
        <div class="editor-form-actions">
          <button class="btn-save" data-id="${q.id}">Save</button>
          <button class="btn-cancel" data-id="${q.id}">Cancel</button>
        </div>
      </div>
    </div>
  `;
}

function bindOpenEvents() {
  const list = document.getElementById('open-list');
  list.querySelectorAll('.btn-edit').forEach(b => {
    b.addEventListener('click', async () => { if (Object.values(editing).some(id => id !== null)) return; editing.open = Number(b.dataset.id); renderOpenList(); });
  });
  list.querySelectorAll('.btn-delete').forEach(b => {
    b.addEventListener('click', async () => {
      if (Object.values(editing).some(id => id !== null)) { alert('Save or cancel the current edit first.'); return; }
      if (confirm('Delete this open question?')) {
        const candidate = structuredClone(questions);
        candidate.openQuestions = candidate.openQuestions.filter(q => q.id !== Number(b.dataset.id));
        if (!await saveQuestions(candidate)) return;
        renderOpenList();
      }
    });
  });
  list.querySelectorAll('.btn-cancel').forEach(b => {
    b.addEventListener('click', () => {
      questions = structuredClone(savedQuestions);
      editing.topic = editing.mcq = editing.open = null;
      renderQuestionEditor();
    });
  });
  list.querySelectorAll('.btn-save').forEach(b => {
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.id);
      const card = b.closest('.editor-card');
      const text = card.querySelector('.open-text-input').value.trim();
      if (!text) { alert('Question text is required.'); return; }
      const q = questions.openQuestions.find(x => x.id === id);
      q.text = text;
      if (!await saveQuestions(questions)) return;
      editing.open = null;
      renderOpenList();
    });
  });
}

// ─── Add buttons ─────────────────────────────────────────────────────────────
function addTopic() {
  if (Object.values(editing).some(id => id !== null)) { alert('Save or cancel the current edit first.'); return; }
  const id = nextId(questions.topics, 1);
  questions.topics.push({ id, name: '', description: '' });
  editing.topic = id;
  renderTopicsList();
}

function addMcq() {
  if (Object.values(editing).some(id => id !== null)) { alert('Save or cancel the current edit first.'); return; }
  const id = nextId(questions.questions, 1);
  questions.questions.push({ id, text: '', options: ['', '', '', ''], correctIndex: 0 });
  editing.mcq = id;
  renderMcqList();
}

function addOpen() {
  if (Object.values(editing).some(id => id !== null)) { alert('Save or cancel the current edit first.'); return; }
  const id = nextId(questions.openQuestions, 101);
  questions.openQuestions.push({ id, text: '' });
  editing.open = id;
  renderOpenList();
}

// ─── Import / Export / Reset ─────────────────────────────────────────────────
function importQuestions(file) {
  const reader = new FileReader();
  reader.onload = async () => {
    try {
      const data = JSON.parse(reader.result);
      if (!Array.isArray(data.topics) || !Array.isArray(data.questions) || !Array.isArray(data.openQuestions)) {
        throw new Error('Invalid file: must contain topics, questions, and openQuestions arrays.');
      }
      if (!confirm('Replace the current question set with the imported one? This cannot be undone.')) return;
      if (!await saveQuestions(data)) return;
      editing.topic = editing.mcq = editing.open = null;
      renderQuestionEditor();
      alert('Question set imported successfully.');
    } catch (err) {
      alert('Could not import file: ' + err.message);
    }
  };
  reader.readAsText(file);
}

async function resetQuestions() {
  if (!confirm('Reset all questions to the built-in defaults? Your current edits will be lost.')) return;
  if (!await saveQuestions(cloneDefaults())) return;
  editing.topic = editing.mcq = editing.open = null;
  renderQuestionEditor();
}

// ─── Init ─────────────────────────────────────────────────────────────────────
async function selectAssessment(id) {
  const revision = ++selectionRevision;
  selectedAssessmentId = Number(id);
  document.getElementById('assessment-select').value = String(id);
  document.getElementById('admin-status').textContent = '';
  document.getElementById('question-editor').disabled = true;
  document.getElementById('assessment-select').disabled = true;
  try {
    const [result, submissions] = await Promise.all([
      api(`/assessments/${Number(id)}/question-bank`),
      api(`/assessments/${Number(id)}/submissions/details`)
    ]);
    if (revision !== selectionRevision) return;
    acceptBank(result);
    renderQuestionEditor();
    renderTable(submissions);
    document.getElementById('submission-count').textContent = submissions.length;
    document.getElementById('empty-state').classList.toggle('hidden', submissions.length > 0);
    showTable();
  } finally {
    if (revision === selectionRevision) document.getElementById('assessment-select').disabled = false;
  }
}

async function refreshAssessments(preferredId) {
  const items = await api('/assessments');
  const select = document.getElementById('assessment-select');
  select.innerHTML = items.map(a => `<option value="${a.id}">${escapeHtml(a.title)}</option>`).join('');
  if (!items.length) throw new Error('No assessments exist. Create a draft to get started.');
  await selectAssessment(items.some(a => a.id === Number(preferredId)) ? Number(preferredId) : items[0].id);
}

const safely = fn => async (...args) => { try { await fn(...args); } catch (err) { reportError(err); } };

async function initAdminData() {
  document.querySelectorAll('.admin-tab').forEach(t => { t.onclick = () => activateTab(t.dataset.tab); });
  document.getElementById('btn-back').onclick = showTable;
  document.getElementById('btn-refresh').onclick = safely(async () => { await refreshSubmissions(); showTable(); });
  document.getElementById('btn-export').onclick = safely(async () => exportCSV(await loadSubmissions()));
  document.getElementById('btn-export-detailed').onclick = safely(async () => exportDetailedCSV(await loadSubmissions()));
  document.getElementById('assessment-select').onchange = safely(e => selectAssessment(e.target.value));
  document.getElementById('btn-new-assessment').onclick = safely(async () => {
    const title = prompt('Title for the new assessment:');
    if (!title?.trim()) return;
    const created = await api('/assessments', { method: 'POST', body: JSON.stringify({ title: title.trim() }) });
    await refreshAssessments(created.id);
    activateTab('questions');
  });
  document.getElementById('btn-clone').onclick = safely(async () => {
    const created = await api(`/assessments/${selectedAssessmentId}/clone`, { method: 'POST' });
    await refreshAssessments(created.id);
    activateTab('questions');
  });
  document.getElementById('btn-publication').onclick = safely(async () => {
    if (Object.values(editing).some(id => id !== null)) throw new Error('Save or cancel the current edit first.');
    await api(`/assessments/${selectedAssessmentId}/publication`, { method: 'PUT', body: JSON.stringify({ isPublished: !bankMetadata.isPublished }) });
    await loadQuestions();
    renderQuestionEditor();
  });
  document.getElementById('btn-reload-bank').onclick = safely(async () => {
    if (!confirm('Reload the saved question bank? Unsaved edits will be discarded.')) return;
    await loadQuestions(); renderQuestionEditor();
  });
  document.getElementById('btn-add-topic').onclick = addTopic;
  document.getElementById('btn-add-mcq').onclick = addMcq;
  document.getElementById('btn-add-open').onclick = addOpen;
  document.getElementById('btn-export-questions').onclick = safely(async () => {
    const saved = await api(`/assessments/${selectedAssessmentId}/question-bank`);
    const { topics, questions, openQuestions } = saved.bank;
    triggerDownload(new Blob([JSON.stringify({ topics, questions, openQuestions }, null, 2)], { type: 'application/json' }), 'redi_questions.json');
  });
  document.getElementById('btn-reset-questions').onclick = safely(resetQuestions);
  const importInput = document.getElementById('import-file');
  document.getElementById('btn-import').onclick = () => importInput.click();
  importInput.onchange = e => { const file = e.target.files[0]; if (file) importQuestions(file); importInput.value = ''; };
  await refreshAssessments(selectedAssessmentId);
}

initApp().catch(reportError);
