import './styles.css';

type DashboardResponse = {
  rawSourceCount: number;
  learningItemCount: number;
  validationIssueCount: number;
};

type PublishResponse = {
  canPublish: boolean;
  needsReview: boolean;
  issueCodes: string[];
};

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';

const app = document.querySelector<HTMLDivElement>('#app');

if (!app) {
  throw new Error('App root not found');
}

app.innerHTML = `
  <main class="shell">
    <section class="topbar">
      <div>
        <p class="eyebrow">TOEIC LR Knowledge Base</p>
        <h1>Validated normalization pipeline</h1>
      </div>
      <button id="refreshButton" class="iconButton" title="Refresh dashboard" aria-label="Refresh dashboard">
        <span aria-hidden="true">↻</span>
      </button>
    </section>

    <section class="metrics" aria-label="Normalization metrics">
      <article class="metric">
        <span class="label">Raw sources</span>
        <strong id="rawSourceCount">0</strong>
      </article>
      <article class="metric">
        <span class="label">Learning items</span>
        <strong id="learningItemCount">0</strong>
      </article>
      <article class="metric warning">
        <span class="label">Validation issues</span>
        <strong id="validationIssueCount">0</strong>
      </article>
    </section>

    <section class="workspace">
      <form id="sourceForm" class="panel">
        <div class="panelHeader">
          <h2>Register source</h2>
          <span class="statusPill">Raw DB</span>
        </div>
        <label>
          Source ID
          <input name="sourceId" value="sheet-row-1" />
        </label>
        <label>
          Title
          <input name="title" value="Từ vựng Part 2 - TOEIC Practice Club" />
        </label>
        <label>
          URL
          <input name="url" value="https://drive.google.com/file/d/example/view" />
        </label>
        <label>
          Status
          <select name="status">
            <option value="opens">opens</option>
            <option value="needs_access">needs_access</option>
            <option value="not_found">not_found</option>
            <option value="missing_hyperlink">missing_hyperlink</option>
          </select>
        </label>
        <button type="submit">Register</button>
      </form>

      <form id="itemForm" class="panel">
        <div class="panelHeader">
          <h2>Publish draft item</h2>
          <span class="statusPill">Validation gate</span>
        </div>
        <label>
          Prompt
          <textarea name="prompt">The manager ____ the report yesterday.</textarea>
        </label>
        <div class="split">
          <label>
            Correct answer
            <input name="correctAnswer" value="B" maxlength="1" />
          </label>
          <label>
            Confidence
            <input name="confidence" value="0.92" inputmode="decimal" />
          </label>
        </div>
        <label>
          Explanation
          <textarea name="explanation">Yesterday requires the past tense form.</textarea>
        </label>
        <button type="submit">Validate publish</button>
      </form>

      <section class="panel resultPanel" aria-live="polite">
        <div class="panelHeader">
          <h2>Last result</h2>
          <span id="resultState" class="statusPill muted">Idle</span>
        </div>
        <pre id="resultOutput">No publish attempt yet.</pre>
      </section>
    </section>
  </main>
`;

const rawSourceCount = element<HTMLElement>('rawSourceCount');
const learningItemCount = element<HTMLElement>('learningItemCount');
const validationIssueCount = element<HTMLElement>('validationIssueCount');
const resultOutput = element<HTMLPreElement>('resultOutput');
const resultState = element<HTMLSpanElement>('resultState');
const refreshButton = element<HTMLButtonElement>('refreshButton');
const sourceForm = element<HTMLFormElement>('sourceForm');
const itemForm = element<HTMLFormElement>('itemForm');

refreshButton.addEventListener('click', () => {
  void refreshDashboard();
});

sourceForm.addEventListener('submit', (event) => {
  event.preventDefault();
  void registerSource(new FormData(sourceForm));
});

itemForm.addEventListener('submit', (event) => {
  event.preventDefault();
  void publishItem(new FormData(itemForm));
});

void refreshDashboard();

function element<T extends HTMLElement>(id: string): T {
  const target = document.getElementById(id);
  if (!target) {
    throw new Error(`Missing element: ${id}`);
  }
  return target as T;
}

async function refreshDashboard(): Promise<void> {
  const response = await fetch(`${API_BASE}/api/dashboard`);
  if (!response.ok) {
    showResult('API unavailable', { status: response.status }, false);
    return;
  }

  const dashboard = (await response.json()) as DashboardResponse;
  rawSourceCount.textContent = String(dashboard.rawSourceCount);
  learningItemCount.textContent = String(dashboard.learningItemCount);
  validationIssueCount.textContent = String(dashboard.validationIssueCount);
}

async function registerSource(form: FormData): Promise<void> {
  const payload = {
    sourceId: stringValue(form, 'sourceId'),
    title: stringValue(form, 'title'),
    url: stringValue(form, 'url'),
    status: stringValue(form, 'status'),
  };

  const response = await fetch(`${API_BASE}/api/raw-sources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  showResult('Raw source registered', { status: response.status, payload }, response.ok);
  await refreshDashboard();
}

async function publishItem(form: FormData): Promise<void> {
  const payload = {
    itemType: 'Question',
    skill: 'Reading',
    part: 5,
    prompt: stringValue(form, 'prompt'),
    options: {
      A: 'submit',
      B: 'submitted',
      C: 'submitting',
      D: 'submission',
    },
    correctAnswer: stringValue(form, 'correctAnswer').trim().toUpperCase(),
    explanation: stringValue(form, 'explanation'),
    sourceRef: {
      sourceId: 'sheet-row-1',
      fileId: 'drive-file-1',
      page: 12,
      blockId: 'p12-b3',
    },
    confidence: Number(stringValue(form, 'confidence')),
    groupRef: null,
    word: '',
    meaning: '',
  };

  const response = await fetch(`${API_BASE}/api/learning-items`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  const body = (await response.json()) as PublishResponse;
  showResult(body.canPublish ? 'Published' : 'Rejected', body, body.canPublish);
  await refreshDashboard();
}

function stringValue(form: FormData, key: string): string {
  const value = form.get(key);
  return typeof value === 'string' ? value : '';
}

function showResult(title: string, data: unknown, ok: boolean): void {
  resultState.textContent = title;
  resultState.className = ok ? 'statusPill success' : 'statusPill danger';
  resultOutput.textContent = JSON.stringify(data, null, 2);
}
