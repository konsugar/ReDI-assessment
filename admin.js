const STORAGE_KEY = 'redi_submissions';
const NOTES_KEY = 'redi_teacher_notes';
const QUESTIONS_KEY = 'redi_questions';

function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function loadSubmissions() {
  return JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
}

function loadNotes() {
  return JSON.parse(localStorage.getItem(NOTES_KEY) || '{}');
}

function saveNote(submissionTimestamp, questionId, text) {
  const notes = loadNotes();
  const key = `${submissionTimestamp}::${questionId}`;
  if (text) {
    notes[key] = text;
  } else {
    delete notes[key];
  }
  localStorage.setItem(NOTES_KEY, JSON.stringify(notes));
}

function getNote(submissionTimestamp, questionId) {
  const notes = loadNotes();
  return notes[`${submissionTimestamp}::${questionId}`] || '';
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
    const savedNote = getNote(s.timestamp, oa.questionId);
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
            data-submission-ts="${escapeHtml(s.timestamp)}"
            data-question-id="${escapeHtml(String(oa.questionId))}"
          >${escapeHtml(savedNote)}</textarea>
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

  container.querySelectorAll('.teacher-note textarea').forEach(ta => {
    ta.addEventListener('input', () => {
      saveNote(ta.dataset.submissionTs, parseInt(ta.dataset.questionId), ta.value);
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
      rows.push([s.participantCode, s.timestamp, 'Open Answer', oa.questionText || `Question ${oa.questionId}`, oa.text || '', '', getNote(s.timestamp, oa.questionId)]);
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
function loadQuestions() {
  try {
    const stored = JSON.parse(localStorage.getItem(QUESTIONS_KEY) || 'null');
    if (stored && Array.isArray(stored.topics) && Array.isArray(stored.questions) && Array.isArray(stored.openQuestions)) {
      return stored;
    }
  } catch (_) {}
  return cloneDefaults();
}

function saveQuestions(data) {
  localStorage.setItem(QUESTIONS_KEY, JSON.stringify(data));
}

function cloneDefaults() {
  return JSON.parse(JSON.stringify(window.REDI_DEFAULT_QUESTIONS));
}

function nextId(items, fallback) {
  if (!items.length) return fallback;
  return Math.max(...items.map(i => Number(i.id) || 0)) + 1;
}

let questions = loadQuestions();
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
    b.addEventListener('click', () => { editing.topic = Number(b.dataset.id); renderTopicsList(); });
  });
  list.querySelectorAll('.btn-delete').forEach(b => {
    b.addEventListener('click', () => {
      if (confirm('Delete this topic?')) {
        questions.topics = questions.topics.filter(t => t.id !== Number(b.dataset.id));
        saveQuestions(questions);
        renderTopicsList();
      }
    });
  });
  list.querySelectorAll('.btn-cancel').forEach(b => {
    b.addEventListener('click', () => {
      const id = Number(b.dataset.id);
      const t = questions.topics.find(x => x.id === id);
      if (t && !t.name && !t.description) {
        questions.topics = questions.topics.filter(x => x.id !== id);
      }
      editing.topic = null;
      renderTopicsList();
    });
  });
  list.querySelectorAll('.btn-save').forEach(b => {
    b.addEventListener('click', () => {
      const id = Number(b.dataset.id);
      const card = b.closest('.editor-card');
      const name = card.querySelector('.topic-name-input').value.trim();
      const desc = card.querySelector('.topic-desc-input').value.trim();
      if (!name) { alert('Topic name is required.'); return; }
      const t = questions.topics.find(x => x.id === id);
      t.name = name;
      t.description = desc;
      saveQuestions(questions);
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
    b.addEventListener('click', () => { editing.mcq = Number(b.dataset.id); renderMcqList(); });
  });
  list.querySelectorAll('.btn-delete').forEach(b => {
    b.addEventListener('click', () => {
      if (confirm('Delete this question?')) {
        questions.questions = questions.questions.filter(q => q.id !== Number(b.dataset.id));
        saveQuestions(questions);
        renderMcqList();
      }
    });
  });
  list.querySelectorAll('.btn-cancel').forEach(b => {
    b.addEventListener('click', () => {
      const id = Number(b.dataset.id);
      const q = questions.questions.find(x => x.id === id);
      if (q && !q.text && q.options.every(o => !o)) {
        questions.questions = questions.questions.filter(x => x.id !== id);
      }
      editing.mcq = null;
      renderMcqList();
    });
  });
  list.querySelectorAll('.btn-save').forEach(b => {
    b.addEventListener('click', () => {
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
      saveQuestions(questions);
      editing.mcq = null;
      renderMcqList();
    });
  });
  list.querySelectorAll('.btn-add-option').forEach(b => {
    b.addEventListener('click', () => {
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
    b.addEventListener('click', () => {
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
    b.addEventListener('click', () => { editing.open = Number(b.dataset.id); renderOpenList(); });
  });
  list.querySelectorAll('.btn-delete').forEach(b => {
    b.addEventListener('click', () => {
      if (confirm('Delete this open question?')) {
        questions.openQuestions = questions.openQuestions.filter(q => q.id !== Number(b.dataset.id));
        saveQuestions(questions);
        renderOpenList();
      }
    });
  });
  list.querySelectorAll('.btn-cancel').forEach(b => {
    b.addEventListener('click', () => {
      const id = Number(b.dataset.id);
      const q = questions.openQuestions.find(x => x.id === id);
      if (q && !q.text) {
        questions.openQuestions = questions.openQuestions.filter(x => x.id !== id);
      }
      editing.open = null;
      renderOpenList();
    });
  });
  list.querySelectorAll('.btn-save').forEach(b => {
    b.addEventListener('click', () => {
      const id = Number(b.dataset.id);
      const card = b.closest('.editor-card');
      const text = card.querySelector('.open-text-input').value.trim();
      if (!text) { alert('Question text is required.'); return; }
      const q = questions.openQuestions.find(x => x.id === id);
      q.text = text;
      saveQuestions(questions);
      editing.open = null;
      renderOpenList();
    });
  });
}

// ─── Add buttons ─────────────────────────────────────────────────────────────
function addTopic() {
  const id = nextId(questions.topics, 1);
  questions.topics.push({ id, name: '', description: '' });
  editing.topic = id;
  renderTopicsList();
}

function addMcq() {
  const id = nextId(questions.questions, 1);
  questions.questions.push({ id, text: '', options: ['', '', '', ''], correctIndex: 0 });
  editing.mcq = id;
  renderMcqList();
}

function addOpen() {
  const id = nextId(questions.openQuestions, 101);
  questions.openQuestions.push({ id, text: '' });
  editing.open = id;
  renderOpenList();
}

// ─── Import / Export / Reset ─────────────────────────────────────────────────
function exportQuestions() {
  const blob = new Blob([JSON.stringify(questions, null, 2)], { type: 'application/json' });
  triggerDownload(blob, `redi_questions_${new Date().toISOString().slice(0,10)}.json`);
}

function importQuestions(file) {
  const reader = new FileReader();
  reader.onload = () => {
    try {
      const data = JSON.parse(reader.result);
      if (!Array.isArray(data.topics) || !Array.isArray(data.questions) || !Array.isArray(data.openQuestions)) {
        throw new Error('Invalid file: must contain topics, questions, and openQuestions arrays.');
      }
      if (!confirm('Replace the current question set with the imported one? This cannot be undone.')) return;
      questions = data;
      saveQuestions(questions);
      editing.topic = editing.mcq = editing.open = null;
      renderQuestionEditor();
      alert('Question set imported successfully.');
    } catch (err) {
      alert('Could not import file: ' + err.message);
    }
  };
  reader.readAsText(file);
}

function resetQuestions() {
  if (!confirm('Reset all questions to the built-in defaults? Your current edits will be lost.')) return;
  questions = cloneDefaults();
  saveQuestions(questions);
  editing.topic = editing.mcq = editing.open = null;
  renderQuestionEditor();
}

// ─── Init ─────────────────────────────────────────────────────────────────────
function init() {
  // Tabs
  document.querySelectorAll('.admin-tab').forEach(t => {
    t.addEventListener('click', () => activateTab(t.dataset.tab));
  });

  // Submissions
  const submissions = loadSubmissions();
  if (!submissions.length) {
    document.getElementById('empty-state').classList.remove('hidden');
  } else {
    document.getElementById('submission-count').textContent = submissions.length;
    document.getElementById('section-table').classList.remove('hidden');
    renderTable(submissions);
  }

  document.getElementById('btn-back').addEventListener('click', showTable);
  document.getElementById('btn-export').addEventListener('click', () => exportCSV(loadSubmissions()));
  document.getElementById('btn-export-detailed').addEventListener('click', () => exportDetailedCSV(loadSubmissions()));
  document.getElementById('btn-clear').addEventListener('click', () => {
    if (confirm('Delete all submissions and teacher notes? This cannot be undone.')) {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(NOTES_KEY);
      location.reload();
    }
  });

  // Question manager
  renderQuestionEditor();
  document.getElementById('btn-add-topic').addEventListener('click', addTopic);
  document.getElementById('btn-add-mcq').addEventListener('click', addMcq);
  document.getElementById('btn-add-open').addEventListener('click', addOpen);
  document.getElementById('btn-export-questions').addEventListener('click', exportQuestions);
  document.getElementById('btn-reset-questions').addEventListener('click', resetQuestions);

  const importInput = document.getElementById('import-file');
  document.getElementById('btn-import').addEventListener('click', () => importInput.click());
  importInput.addEventListener('change', e => {
    const file = e.target.files[0];
    if (file) importQuestions(file);
    importInput.value = '';
  });
}

init();
