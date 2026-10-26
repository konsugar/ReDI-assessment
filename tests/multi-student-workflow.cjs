const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
if (process.env.REDI_TEST_DATABASE !== 'ReDI_Consolidation_Test_20261026_7c91') throw new Error('Use the isolated test database.');
const api = 'http://127.0.0.1:15069/api';
const web = 'http://127.0.0.1:18000';
const auth = JSON.parse(fs.readFileSync(path.join(__dirname, '../backend/appsettings.Development.json'), 'utf8')).AdminAuth;
const stamp = Date.now();
const title = `Three-student workflow ${stamp}`;
const students = [
  { code: `ALL-CORRECT-${stamp}`, choices: [0, 1], score: 2 },
  { code: `PARTIAL-${stamp}`, choices: [0, 0], score: 1 },
  { code: `ALL-WRONG-${stamp}`, choices: [1, 0], score: 0 }
];
async function main() {
  const browser = await chromium.launch({ headless: true, ...(process.env.REDI_TEST_BROWSER ? { channel: process.env.REDI_TEST_BROWSER } : {}) });
  const errors = [];
  async function pageFor(url) {
    const context = await browser.newContext();
    await context.addInitScript(value => { window.REDI_API_BASE = value; }, api);
    const page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(url);
    return page;
  }
  async function login(page) {
    await page.locator('#admin-username').fill(auth.Username);
    await page.locator('#admin-password').fill(auth.Password);
    await page.locator('#admin-login-form button').click();
    await page.waitForFunction(() => bankMetadata !== null);
  }
  try {
    const teacher = await pageFor(web + '/admin.html');
    teacher.on('dialog', dialog => dialog.accept(dialog.type() === 'prompt' ? title : undefined));
    await login(teacher);
    await teacher.locator('#btn-new-assessment').click();
    await teacher.waitForFunction(value => bankMetadata?.title === value, title);
    const id = await teacher.evaluate(() => selectedAssessmentId);
    const bank = {
      topics: [{ id: 1, name: `Workflow topic ${stamp}`, description: 'Database-backed workflow' }],
      questions: [
        { id: 1, text: 'Choose A', options: ['A', 'B'], correctIndex: 0 },
        { id: 2, text: 'Choose B', options: ['A', 'B'], correctIndex: 1 }
      ],
      openQuestions: [{ id: 3, text: 'Explain your answers' }]
    };
    await teacher.locator('#import-file').setInputFiles({ name: 'workflow.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(bank)) });
    await teacher.waitForFunction(() => document.querySelectorAll('#mcq-list .btn-edit').length === 2);
    await teacher.locator('#btn-publication').click();
    await teacher.waitForFunction(() => bankMetadata.isPublished);
    for (const person of students) {
      const student = await pageFor(`${web}/evaluation.html?assessmentId=${id}`);
      await student.waitForFunction(() => state.data !== null);
      await student.locator('#participant-code').fill(person.code);
      await student.locator('#login-form button').click();
      await student.locator('.rating-option').filter({ has: student.locator('input[value="4"]') }).click();
      await student.locator('#btn-to-mcq').click();
      for (let i = 0; i < person.choices.length; i++) {
        await student.locator('.mcq-question').nth(i).locator('.option-label')
          .filter({ has: student.locator(`input[value="${person.choices[i]}"]`) }).click();
      }
      await student.locator('#btn-to-open').click();
      await student.locator('.open-textarea').fill(`Written answer from ${person.code}`);
      await student.locator('#btn-submit').click();
      await student.locator('#section-results').waitFor({ state: 'visible' });
      assert.equal(await student.evaluate(() => localStorage.length), 0);
      await student.context().close();
    }
    await teacher.locator('[data-tab="submissions"]').click();
    await teacher.locator('#btn-refresh').click();
    await teacher.waitForFunction(() => document.querySelectorAll('#table-body tr').length === 3);
    for (const [index, person] of students.entries()) {
      const row = teacher.locator('#table-body tr').filter({ hasText: person.code });
      assert.match(await row.textContent(), new RegExp(`${person.score}/2`));
      await row.locator('.btn-view').click();
      await teacher.locator('.teacher-note textarea').fill(`Review ${index + 1}`);
      await teacher.locator('.answer-evaluation').selectOption(index === 0 ? 'true' : 'false');
      await teacher.locator('.btn-save-review').click();
      await teacher.waitForFunction(() => document.querySelector('.review-status').textContent === 'Saved to database');
      await teacher.locator('#btn-back').click();
    }
    for (const [button, detailed] of [['#btn-export', false], ['#btn-export-detailed', true]]) {
      const downloading = teacher.waitForEvent('download');
      await teacher.locator(button).click();
      const download = await downloading;
      const content = fs.readFileSync(await download.path(), 'utf8');
      for (const person of students) assert.ok(content.includes(person.code));
      if (detailed) { for (let i = 1; i <= 3; i++) assert.ok(content.includes(`Review ${i}`)); }
      const lines = content.trim().split(/\r?\n/);
      assert.equal(lines.length, detailed ? 13 : 4);
    }
    const fresh = await pageFor(web + '/admin.html');
    await login(fresh);
    await fresh.locator('#assessment-select').selectOption(String(id));
    await fresh.waitForFunction(value => bankMetadata?.id === value, id);
    for (const [index, person] of students.entries()) {
      await fresh.locator('#table-body tr').filter({ hasText: person.code }).locator('.btn-view').click();
      assert.equal(await fresh.locator('.teacher-note textarea').inputValue(), `Review ${index + 1}`);
      assert.equal(await fresh.locator('.answer-evaluation').inputValue(), index === 0 ? 'true' : 'false');
      await fresh.locator('#btn-back').click();
    }
    assert.deepEqual(errors, []);
    console.log(`PASS: draft creation, import, publication, three independent students (2/2, 1/2, 0/2), teacher review, summary/detailed CSV, fresh-session persistence. Assessment ${id}.`);
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
