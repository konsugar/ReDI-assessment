// Run only against the isolated test API described in tests/README.md.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const api = 'http://127.0.0.1:15069/api';
const web = 'http://127.0.0.1:18000';
if (process.env.REDI_TEST_DATABASE !== 'ReDI_Consolidation_Test_20261026_7c91') throw new Error('Explicit isolated test database acknowledgement required.');
const settings = JSON.parse(fs.readFileSync(path.join(__dirname, '../backend/appsettings.Development.json'), 'utf8')).AdminAuth;
let token;
async function request(url, method = 'GET', body, expected = 200) {
  const response = await fetch(api + url, { method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  assert.equal(response.status, expected, `${method} ${url}: ${text.slice(0, 1500)}`);
  if (!text) return null;
  try { return JSON.parse(text); } catch { return text; }
}
async function main() {
  token = (await request('/admin/auth/login', 'POST', { username: settings.Username, password: settings.Password })).accessToken;
  const stamp = Date.now();
  const a = await request('/assessments', 'POST', { title: `Database UI test ${stamp}` }, 201);
  const bankPath = `/assessments/${a.id}/question-bank`;
  let snapshot = await request(bankPath);
  const initial = { version: snapshot.version, topics: [{ id: 1, name: `Test topic ${stamp}`, description: 'Shared through database' }], questions: [{ id: 1, text: 'Choose the correct answer', options: ['Correct', 'Incorrect'], correctIndex: 0 }], openQuestions: [{ id: 2, text: 'Explain your answer' }] };
  const saved = await request(bankPath, 'PUT', initial);
  await request(bankPath, 'PUT', initial, 409);
  assert.equal((await request(bankPath)).version, saved.version, 'Stale write changed content');
  await request(bankPath, 'PUT', { ...initial, version: saved.version, questions: [{ ...initial.questions[0], correctIndex: 9 }] }, 400);
  assert.equal((await request(bankPath)).version, saved.version, 'Invalid import changed content');
  const oldToken = token; token = undefined;
  await request(bankPath, 'GET', undefined, 401);
  token = oldToken;
  const browser = await chromium.launch({ headless: true, ...(process.env.REDI_TEST_BROWSER ? { channel: process.env.REDI_TEST_BROWSER } : {}) });
  try {
    const context = await browser.newContext();
    await context.addInitScript(base => { window.REDI_API_BASE = base; }, api);
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', err => errors.push(err.message));
    page.on('dialog', dialog => dialog.accept());
    async function login(p) {
      await p.goto(web + '/admin.html');
      await p.locator('#admin-username').fill(settings.Username);
      await p.locator('#admin-password').fill(settings.Password);
      await p.locator('#admin-login-form button').click();
      await p.locator(`#assessment-select option[value="${a.id}"]`).waitFor({ state: 'attached' });
      await p.locator('#assessment-select').selectOption(String(a.id));
      await p.waitForFunction(id => bankMetadata?.id === id, a.id);
    }
    await login(page);
    await page.locator('[data-tab="questions"]').click();
    await page.locator('#open-list .btn-edit').click();
    await page.locator('.open-text-input').fill('Edited in a real browser');
    await page.locator('#open-list .btn-save').click();
    await page.waitForFunction(() => document.querySelector('#admin-status').textContent === 'Question bank saved to database.');
    assert.equal((await request(bankPath)).bank.openQuestions[0].text, 'Edited in a real browser');

    // Failed writes must not claim success or update server data.
    await page.locator('#open-list .btn-edit').click();
    await page.locator('.open-text-input').fill('Must not be saved');
    await page.route('**/question-bank', route => route.request().method() === 'PUT' ? route.fulfill({ status: 503, body: 'Test service unavailable' }) : route.continue());
    await page.locator('#open-list .btn-save').click();
    await page.waitForFunction(() => document.querySelector('#admin-status').textContent.includes('Test service unavailable'));
    assert.equal((await request(bankPath)).bank.openQuestions[0].text, 'Edited in a real browser');
    await page.unroute('**/question-bank');
    await page.locator('#btn-reload-bank').click();
    await page.waitForFunction(() => !document.querySelector('.open-text-input'));

    await page.locator('#topics-list .btn-edit').click();
    await page.locator('.topic-desc-input').fill('Topic description saved from browser');
    await page.locator('#topics-list .btn-save').click();
    await page.waitForFunction(() => !document.querySelector('.topic-desc-input'));
    assert.equal((await request(bankPath)).bank.topics[0].description, 'Topic description saved from browser');
    await page.locator('#mcq-list .btn-edit').click();
    await page.locator('.mcq-text-input').fill('Choice question edited in browser');
    await page.locator('#mcq-list .btn-save').click();
    await page.waitForFunction(() => !document.querySelector('.mcq-text-input'));
    assert.equal((await request(bankPath)).bank.questions[0].text, 'Choice question edited in browser');

    // Add/delete and JSON import persist rather than merely editing a local array.
    await page.locator('#btn-add-open').click();
    await page.locator('.open-text-input').fill('Temporary extra question');
    await page.locator('#open-list .btn-save').click();
    await page.waitForFunction(() => document.querySelectorAll('#open-list .btn-delete').length === 2);
    await page.locator('#open-list .btn-delete').last().click();
    await page.waitForFunction(() => document.querySelectorAll('#open-list .btn-delete').length === 1);
    snapshot = await request(bankPath);
    const imported = { topics: snapshot.bank.topics, questions: snapshot.bank.questions, openQuestions: [{ id: 999, text: 'Imported and persisted' }] };
    await page.locator('#btn-reset-questions').click();
    await page.waitForFunction(() => document.querySelectorAll('#mcq-list .btn-delete').length === 8);
    assert.equal((await request(bankPath)).bank.questions.length, 8, 'Reset did not persist');
    await page.locator('#import-file').setInputFiles({ name: 'bank.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(imported)) });
    await page.waitForFunction(() => document.querySelector('#open-list').textContent.includes('Imported and persisted'));
    assert.equal((await request(bankPath)).bank.openQuestions[0].text, 'Imported and persisted');
    await page.locator('#btn-publication').click();
    await page.waitForFunction(() => bankMetadata.isPublished);
    snapshot = await request(bankPath);
    await request(bankPath, 'PUT', { ...imported, version: snapshot.version }, 409);

    const student = await context.newPage();
    student.on('pageerror', err => errors.push(err.message));
    await student.goto(`${web}/evaluation.html?assessmentId=${a.id}`);
    await student.waitForFunction(() => state.data !== null);
    await student.locator('#participant-code').fill(`UI-${stamp}`);
    await student.locator('#login-form button').click();
    await student.locator('.rating-option').filter({ has: student.locator('input[value="3"]') }).click();
    await student.locator('#btn-to-mcq').click();
    await student.locator('.option-label').filter({ has: student.locator('input[value="0"]') }).click();
    await student.locator('#btn-to-open').click();
    await student.locator('.open-textarea').fill('Answer saved from the student page');
    await student.locator('#btn-submit').click();
    await student.locator('#section-results').waitFor({ state: 'visible' });
    assert.equal(await student.evaluate(() => localStorage.length), 0, 'Student answers were copied to browser storage');

    await page.locator('[data-tab="submissions"]').click();
    await page.locator('#btn-refresh').click();
    await page.locator('#table-body').getByText(`UI-${stamp}`, { exact: true }).waitFor();
    await page.locator('.btn-view').click();
    await page.locator('.teacher-note textarea').fill('Teacher note from browser');
    await page.locator('.answer-evaluation').selectOption('true');
    await page.locator('.btn-save-review').click();
    await page.waitForFunction(() => document.querySelector('.review-status').textContent === 'Saved to database');
    const second = await browser.newContext();
    await second.addInitScript(base => { window.REDI_API_BASE = base; }, api);
    const other = await second.newPage();
    await login(other);
    await other.locator('#table-body').getByText(`UI-${stamp}`, { exact: true }).waitFor();
    await other.locator('.btn-view').click();
    assert.equal(await other.locator('.teacher-note textarea').inputValue(), 'Teacher note from browser');
    assert.equal(await other.locator('.answer-evaluation').inputValue(), 'true');
    const downloading = other.waitForEvent('download');
    await other.locator('#btn-export-detailed').click();
    const download = await downloading;
    assert.match(fs.readFileSync(await download.path(), 'utf8'), /Teacher note from browser/);
    assert.equal(await other.evaluate(() => localStorage.length), 0);
    await other.locator('[data-tab="questions"]').click();
    assert.match(await other.locator('#open-list').textContent(), /Imported and persisted/);
    assert.equal(await other.locator('#btn-add-open').isDisabled(), true);
    await other.locator('#btn-publication').click();
    await other.waitForFunction(() => !bankMetadata.isPublished);
    snapshot = await request(bankPath);
    await request(bankPath, 'PUT', { ...imported, version: snapshot.version }, 409);
    assert.deepEqual(errors, []);
    console.log(`PASS: atomic saves, invalid/stale/unauthorized/locked writes, browser topic/MCQ/open edits, failure feedback, add/delete/reset/import, publication, student submission, review, CSV export, fresh-session persistence; assessment ${a.id}.`);
  } finally { await browser.close(); }
}
main().catch(err => { console.error(err); process.exitCode = 1; });
