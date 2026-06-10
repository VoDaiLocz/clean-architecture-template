import './styles.css';

type CorpusManifest = {
  corpusId: string;
  title: string;
  sheetTabs: number;
  sheetRows: number;
  pdfBooks: number;
  pdfPages: number;
  audioFiles: number;
  targetLearningItems: number;
};

type NormalizationStage = {
  stageKey: string;
  displayName: string;
  totalCount: number;
  completedCount: number;
  rejectedCount: number;
  remainingCount: number;
};

type DashboardResponse = {
  rawSourceCount: number;
  learningItemCount: number;
  validationIssueCount: number;
  corpus: CorpusManifest;
  normalizationStages: NormalizationStage[];
};

type PublishResponse = {
  canPublish: boolean;
  needsReview: boolean;
  issueCodes: string[];
};

type PracticeQuestion = {
  id: string;
  part: number;
  skill: 'Listening' | 'Reading';
  prompt: string;
  options: Record<string, string>;
  correctAnswer: string;
  explanation: string;
  source: string;
};

type VocabCard = {
  word: string;
  meaning: string;
  example: string;
  source: string;
};

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

const fallbackDashboard: DashboardResponse = {
  rawSourceCount: 0,
  learningItemCount: 0,
  validationIssueCount: 0,
  corpus: {
    corpusId: 'toeic-master',
    title: 'Google Sheet + PDF book library awaiting authenticated scan',
    sheetTabs: 3,
    sheetRows: 18000,
    pdfBooks: 64,
    pdfPages: 12800,
    audioFiles: 0,
    targetLearningItems: 54000,
  },
  normalizationStages: [
    stage('inventory', 'Inventory scan', 18867, 0, 0),
    stage('extraction', 'Text extraction', 12800, 0, 0),
    stage('normalization', 'Item normalization', 54000, 0, 0),
    stage('validation', 'Validation gate', 54000, 0, 0),
    stage('publish', 'Publish queue', 54000, 0, 0),
  ],
};

const questions: PracticeQuestion[] = [
  {
    id: 'p5-001',
    part: 5,
    skill: 'Reading',
    prompt: 'The manager ____ the report yesterday before sending it to the client.',
    options: {
      A: 'submit',
      B: 'submitted',
      C: 'submitting',
      D: 'submission',
    },
    correctAnswer: 'B',
    explanation: 'Yesterday signals past time, so the verb must be the past-tense form submitted.',
    source: 'sheet-row-1 / page 12 / block p12-b3',
  },
  {
    id: 'p5-002',
    part: 5,
    skill: 'Reading',
    prompt: 'Employees must ____ company policy when using shared equipment.',
    options: {
      A: 'abide by',
      B: 'depend to',
      C: 'result for',
      D: 'approve at',
    },
    correctAnswer: 'A',
    explanation: 'Abide by means follow or comply with a rule, policy, or agreement.',
    source: 'sheet-row-1 / vocab group 04 / verified',
  },
];

const vocabCards: VocabCard[] = [
  {
    word: 'abide by',
    meaning: 'tuân thủ',
    example: 'All employees must abide by safety regulations.',
    source: '600 Essential Words style item',
  },
  {
    word: 'submit',
    meaning: 'nộp, trình lên',
    example: 'Please submit the report before Friday.',
    source: 'Business communication vocabulary',
  },
  {
    word: 'equipment',
    meaning: 'thiết bị',
    example: 'The office equipment is checked every month.',
    source: 'Workplace topic',
  },
];

const app = document.querySelector<HTMLDivElement>('#app');
if (!app) {
  throw new Error('App root not found');
}

const appRoot = app;
let dashboardState = fallbackDashboard;
let apiState: 'loading' | 'online' | 'offline' = 'loading';
let lastAdminResult: { title: string; data: unknown; ok: boolean } | null = null;

if (location.pathname.startsWith('/admin')) {
  renderAdmin();
} else {
  renderLearner();
}

void refreshDashboard();

function renderLearner(): void {
  const route = learnerRoute();
  appRoot.innerHTML = `
    <main class="learnerShell">
      <aside class="studyNav" aria-label="TOEIC sections">
        <a class="brandBlock" href="/">
          <span class="brandMark"><i></i></span>
          <span>
            <strong>TOEIC LR</strong>
            <small>${formatCompact(dashboardState.corpus.targetLearningItems)} item corpus</small>
          </span>
        </a>
        <nav class="navList">
          ${navLink('/', 'Tổng quan', route)}
          ${navLink('/practice', 'Luyện đề', route)}
          ${navLink('/listening', 'Listening', route)}
          ${navLink('/vocabulary', 'Từ vựng', route)}
          ${navLink('/review', 'Review lỗi', route)}
          <a href="/admin">Admin</a>
        </nav>
        <div class="navTelemetry">
          <span>${apiStateLabel()}</span>
          <strong>${formatNumber(dashboardState.learningItemCount)}</strong>
          <small>published items</small>
        </div>
      </aside>

      <section class="studyMain">
        ${renderLearnerScreen(route)}
      </section>
    </main>
  `;

  if (route === '/practice') {
    wirePractice();
  }
}

function learnerRoute(): string {
  const path = location.pathname.replace(/\/$/, '') || '/';
  return ['/', '/practice', '/listening', '/vocabulary', '/review'].includes(path) ? path : '/';
}

function renderLearnerScreen(route: string): string {
  if (route === '/practice') return practiceScreen();
  if (route === '/listening') return listeningScreen();
  if (route === '/vocabulary') return vocabularyScreen();
  if (route === '/review') return reviewScreen();
  return overviewScreen();
}

function navLink(href: string, label: string, route: string): string {
  return `<a class="${route === href ? 'active' : ''}" href="${href}">${label}</a>`;
}

function pageHeader(eyebrow: string, title: string, meta = 'TOEIC LR'): string {
  return `
    <header class="pageHeader">
      <div>
        <p class="eyebrow">${eyebrow}</p>
        <h1>${title}</h1>
      </div>
      <div class="targetBox">
        <span>${meta}</span>
        <strong>${formatCompact(dashboardState.corpus.targetLearningItems)}</strong>
      </div>
    </header>
  `;
}

function overviewScreen(): string {
  const corpus = dashboardState.corpus;
  const publishedRatio = ratio(dashboardState.learningItemCount, corpus.targetLearningItems);
  return `
    ${pageHeader('Corpus command center', 'Kho học liệu lớn, học theo luồng riêng', 'target items')}
    <section class="corpusHero">
      <div class="heroCopy">
        <span class="liveChip">${apiStateLabel()}</span>
        <h2>TOEIC Master Corpus</h2>
        <p><strong>${corpus.title}</strong>. Home chỉ điều phối. Nội dung học nằm trong từng màn riêng; dữ liệu gốc đi qua inventory, extraction, normalization, validation rồi mới publish vào DB học.</p>
      </div>
      <div class="radarPanel" aria-label="Corpus coverage">
        <div class="radarRing">
          <span>${publishedRatio}%</span>
          <small>published</small>
        </div>
        <div class="radarStats">
          ${statTile('Sheet rows', corpus.sheetRows)}
          ${statTile('PDF pages', corpus.pdfPages)}
          ${statTile('PDF books', corpus.pdfBooks)}
          ${statTile('Issues', dashboardState.validationIssueCount)}
        </div>
      </div>
    </section>

    <section class="overviewGrid">
      <article class="focusPanel">
        <div class="panelHeader">
          <div>
            <h2>Work queues</h2>
            <span class="subtle">Không nhét toàn bộ chức năng vào trang chủ.</span>
          </div>
          <span class="statusPill success">${formatNumber(dashboardState.learningItemCount)} live</span>
        </div>
        <div class="focusList">
          <a href="/practice"><strong>Reading practice</strong><span>Part 5/6/7 từ item đã publish</span></a>
          <a href="/listening"><strong>Listening queue</strong><span>Part 1-4 chỉ hiện khi có transcript/audio trace</span></a>
          <a href="/vocabulary"><strong>Vocabulary bank</strong><span>Từ vựng chuẩn hóa theo nguồn, ví dụ, nghĩa</span></a>
          <a href="/review"><strong>Review lỗi</strong><span>Nhóm theo source, kỹ năng, loại lỗi</span></a>
        </div>
      </article>

      <aside class="qualityPanel">
        <h2>Import pipeline</h2>
        <div class="stageStack">
          ${normalizationStages().map(stageCard).join('')}
        </div>
      </aside>
    </section>
  `;
}

function practiceScreen(): string {
  return `
    ${pageHeader('Reading practice', 'Part 5 drill', `${formatNumber(dashboardState.learningItemCount)} published`)}
    <section class="practiceWorkspace">
      <article class="practicePanel">
        <div class="panelHeader">
          <div>
            <h2>Bài luyện</h2>
            <span id="questionMeta" class="subtle"></span>
          </div>
          <span id="answerState" class="statusPill muted">Chưa chọn</span>
        </div>
        <p id="questionPrompt" class="questionPrompt"></p>
        <div id="answerOptions" class="answerOptions"></div>
        <div class="practiceActions">
          <button id="checkAnswerButton" type="button">Kiểm tra</button>
          <button id="nextQuestionButton" type="button" class="secondaryButton">Câu tiếp</button>
        </div>
        <div id="explanationBox" class="explanationBox" hidden></div>
      </article>

      <aside class="tracePanel">
        <h2>Trace & validation</h2>
        <div class="traceBlock">
          <span>Source</span>
          <strong id="sourceTrace">-</strong>
        </div>
        <ul class="checkList">
          <li class="pass">Prompt is complete</li>
          <li class="pass">Answer key matches options</li>
          <li class="pass">Explanation exists</li>
          <li class="pass">Confidence >= 0.80</li>
        </ul>
      </aside>
    </section>
  `;
}

function listeningScreen(): string {
  return `
    ${pageHeader('Listening queue', 'Nghe theo cụm đã có trace', `${dashboardState.corpus.audioFiles} audio files`)}
    <section class="twoColumn">
      <article class="panel listeningDeck">
        <div class="panelHeader">
          <h2>Audio lanes</h2>
          <span class="statusPill">Part 1-4</span>
        </div>
        <div class="waveform" aria-hidden="true">${'<i></i>'.repeat(12)}</div>
        <div class="audioRows">
          <button type="button" class="audioRow"><span>Part 1</span><strong>Photo description extraction</strong></button>
          <button type="button" class="audioRow"><span>Part 2</span><strong>Short response drills</strong></button>
          <button type="button" class="audioRow"><span>Part 3-4</span><strong>Transcript-group validation</strong></button>
        </div>
      </article>
      <aside class="panel">
        <h2>Publish rules</h2>
        <ul class="checkList">
          <li class="pass">Không hiện audio nếu thiếu transcript</li>
          <li class="pass">Không trộn Listening với Reading queue</li>
          <li class="pass">Mỗi câu giữ page/block/source trace</li>
        </ul>
      </aside>
    </section>
  `;
}

function vocabularyScreen(): string {
  return `
    ${pageHeader('Vocabulary', 'Ôn từ theo nguồn', `${formatCompact(dashboardState.corpus.targetLearningItems)} target`)}
    <section class="panel">
      <div class="panelHeader">
        <h2>Từ cần ôn</h2>
        <span class="statusPill">Spaced review</span>
      </div>
      <div class="vocabList">
        ${vocabCards
          .map(
            (card) => `
              <div class="vocabCard">
                <div>
                  <strong>${card.word}</strong>
                  <span>${card.meaning}</span>
                </div>
                <p>${card.example}</p>
                <small>${card.source}</small>
              </div>
            `,
          )
          .join('')}
      </div>
    </section>
  `;
}

function reviewScreen(): string {
  return `
    ${pageHeader('Review lỗi', 'Chỉ xử lý item cần xem lại', `${dashboardState.validationIssueCount} issues`)}
    <section class="twoColumn">
      <article class="panel">
        <div class="panelHeader">
          <h2>Issue queue</h2>
          <span class="statusPill ${dashboardState.validationIssueCount > 0 ? 'danger' : 'muted'}">${formatNumber(dashboardState.validationIssueCount)}</span>
        </div>
        <div class="issueBoard">
          ${issueLane('missing_source_ref', 'Source trace')}
          ${issueLane('answer_not_in_options', 'Answer key')}
          ${issueLane('low_confidence', 'Confidence')}
        </div>
      </article>
      <aside class="panel">
        <h2>Review rules</h2>
        <ul class="checkList">
          <li class="pass">Không mở PDF gốc trong flow học</li>
          <li class="pass">Không publish item thiếu đáp án</li>
          <li class="pass">Không trộn Reading và Listening khi review</li>
        </ul>
      </aside>
    </section>
  `;
}

function wirePractice(): void {
  let activeQuestionIndex = 0;
  let selectedAnswer = '';
  let revealed = false;

  const questionMeta = element<HTMLElement>('questionMeta');
  const answerState = element<HTMLElement>('answerState');
  const questionPrompt = element<HTMLElement>('questionPrompt');
  const answerOptions = element<HTMLDivElement>('answerOptions');
  const explanationBox = element<HTMLDivElement>('explanationBox');
  const checkAnswerButton = element<HTMLButtonElement>('checkAnswerButton');
  const nextQuestionButton = element<HTMLButtonElement>('nextQuestionButton');
  const sourceTrace = element<HTMLElement>('sourceTrace');

  checkAnswerButton.addEventListener('click', () => {
    if (!selectedAnswer) {
      answerState.textContent = 'Chọn đáp án';
      answerState.className = 'statusPill danger';
      return;
    }

    revealed = true;
    renderQuestion();
  });

  nextQuestionButton.addEventListener('click', () => {
    activeQuestionIndex = (activeQuestionIndex + 1) % questions.length;
    selectedAnswer = '';
    revealed = false;
    renderQuestion();
  });

  renderQuestion();

  function renderQuestion(): void {
    const question = questions[activeQuestionIndex];
    questionMeta.textContent = `Part ${question.part} · ${question.skill} · ${question.id}`;
    questionPrompt.textContent = question.prompt;
    sourceTrace.textContent = question.source;
    answerOptions.innerHTML = Object.entries(question.options)
      .map(([label, text]) => {
        const isSelected = selectedAnswer === label;
        const isCorrect = revealed && question.correctAnswer === label;
        const isWrong = revealed && isSelected && question.correctAnswer !== label;
        const className = ['answerOption', isSelected ? 'selected' : '', isCorrect ? 'correct' : '', isWrong ? 'wrong' : '']
          .filter(Boolean)
          .join(' ');

        return `<button type="button" class="${className}" data-answer="${label}">
          <span>${label}</span>
          <strong>${text}</strong>
        </button>`;
      })
      .join('');

    answerOptions.querySelectorAll<HTMLButtonElement>('button').forEach((button) => {
      button.addEventListener('click', () => {
        if (revealed) return;
        selectedAnswer = button.dataset.answer ?? '';
        renderQuestion();
      });
    });

    if (!revealed) {
      answerState.textContent = selectedAnswer ? `Đã chọn ${selectedAnswer}` : 'Chưa chọn';
      answerState.className = selectedAnswer ? 'statusPill' : 'statusPill muted';
      explanationBox.hidden = true;
      explanationBox.textContent = '';
      return;
    }

    const isCorrect = selectedAnswer === question.correctAnswer;
    answerState.textContent = isCorrect ? 'Đúng' : 'Sai';
    answerState.className = isCorrect ? 'statusPill success' : 'statusPill danger';
    explanationBox.hidden = false;
    explanationBox.innerHTML = `<strong>Giải thích:</strong> ${question.explanation}`;
  }
}

function renderAdmin(): void {
  appRoot.innerHTML = `
    <main class="shell">
      <section class="topbar">
        <div>
          <p class="eyebrow">TOEIC LR Knowledge Base</p>
          <h1>Corpus ingestion console</h1>
        </div>
        <div class="topActions">
          <a class="textButton" href="/">Learner app</a>
          <button id="refreshButton" class="iconButton" title="Refresh dashboard" aria-label="Refresh dashboard">
            <span aria-hidden="true">↻</span>
          </button>
        </div>
      </section>

      <section class="metrics metricsWide" aria-label="Corpus metrics">
        ${metric('Target items', dashboardState.corpus.targetLearningItems)}
        ${metric('Sheet rows', dashboardState.corpus.sheetRows)}
        ${metric('PDF pages', dashboardState.corpus.pdfPages)}
        ${metric('Published', dashboardState.learningItemCount)}
        ${metric('Issues', dashboardState.validationIssueCount, 'warning')}
      </section>

      <section class="pipeline">
        ${normalizationStages().map(stageCard).join('')}
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
            <h2>Validate draft item</h2>
            <span class="statusPill">Gate</span>
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
            <h2>Gate result</h2>
            <span id="resultState" class="statusPill ${lastAdminResult ? (lastAdminResult.ok ? 'success' : 'danger') : 'muted'}">${lastAdminResult?.title ?? apiStateLabel()}</span>
          </div>
          <pre id="resultOutput">${JSON.stringify(lastAdminResult?.data ?? { apiBase: API_BASE, corpus: dashboardState.corpus }, null, 2)}</pre>
        </section>
      </section>
    </main>
  `;

  const refreshButton = element<HTMLButtonElement>('refreshButton');
  const sourceForm = element<HTMLFormElement>('sourceForm');
  const itemForm = element<HTMLFormElement>('itemForm');

  refreshButton.addEventListener('click', () => {
    void refreshDashboard(true);
  });

  sourceForm.addEventListener('submit', (event) => {
    event.preventDefault();
    void registerSource(new FormData(sourceForm));
  });

  itemForm.addEventListener('submit', (event) => {
    event.preventDefault();
    void publishItem(new FormData(itemForm));
  });
}

async function refreshDashboard(forceRender = false): Promise<void> {
  try {
    const response = await fetch(`${API_BASE}/api/dashboard`);
    if (!response.ok) {
      apiState = 'offline';
      if (forceRender) rerenderCurrentRoute();
      return;
    }

    dashboardState = normalizeDashboard((await response.json()) as Partial<DashboardResponse>);
    apiState = 'online';
  } catch {
    apiState = 'offline';
  }

  rerenderCurrentRoute();
}

async function registerSource(form: FormData): Promise<void> {
  const payload = {
    sourceId: stringValue(form, 'sourceId'),
    title: stringValue(form, 'title'),
    url: stringValue(form, 'url'),
    status: stringValue(form, 'status'),
  };

  try {
    const response = await fetch(`${API_BASE}/api/raw-sources`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    showAdminResult('Raw source registered', { status: response.status, payload }, response.ok);
    await refreshDashboard(true);
  } catch {
    showAdminResult('API offline', { apiBase: API_BASE, endpoint: '/api/raw-sources', payload }, false);
  }
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

  try {
    const response = await fetch(`${API_BASE}/api/learning-items`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    const body = (await response.json()) as PublishResponse;
    showAdminResult(body.canPublish ? 'Published' : 'Rejected', body, body.canPublish);
    await refreshDashboard(true);
  } catch {
    showAdminResult('API offline', { apiBase: API_BASE, endpoint: '/api/learning-items', payload }, false);
  }
}

function normalizeDashboard(value: Partial<DashboardResponse>): DashboardResponse {
  const corpus = { ...fallbackDashboard.corpus, ...value.corpus };
  const stages = value.normalizationStages?.length ? value.normalizationStages : fallbackDashboard.normalizationStages;
  return {
    rawSourceCount: value.rawSourceCount ?? 0,
    learningItemCount: value.learningItemCount ?? 0,
    validationIssueCount: value.validationIssueCount ?? 0,
    corpus,
    normalizationStages: stages.map((item) => ({
      ...item,
      remainingCount: item.remainingCount ?? Math.max(0, item.totalCount - item.completedCount - item.rejectedCount),
    })),
  };
}

function normalizationStages(): NormalizationStage[] {
  return dashboardState.normalizationStages.length ? dashboardState.normalizationStages : fallbackDashboard.normalizationStages;
}

function rerenderCurrentRoute(): void {
  if (location.pathname.startsWith('/admin')) {
    renderAdmin();
  } else {
    renderLearner();
  }
}

function showAdminResult(title: string, data: unknown, ok: boolean): void {
  lastAdminResult = { title, data, ok };
  const resultState = document.getElementById('resultState');
  const resultOutput = document.getElementById('resultOutput');
  if (!resultState || !resultOutput) return;
  resultState.textContent = title;
  resultState.className = ok ? 'statusPill success' : 'statusPill danger';
  resultOutput.textContent = JSON.stringify(data, null, 2);
}

function statTile(label: string, value: number): string {
  return `<div><span>${label}</span><strong>${formatNumber(value)}</strong></div>`;
}

function metric(label: string, value: number, tone = ''): string {
  return `
    <article class="metric ${tone}">
      <span class="label">${label}</span>
      <strong>${formatNumber(value)}</strong>
    </article>
  `;
}

function stageCard(stageItem: NormalizationStage): string {
  const completed = ratio(stageItem.completedCount, stageItem.totalCount);
  return `
    <article class="stageCard">
      <div>
        <span>${stageItem.stageKey}</span>
        <strong>${stageItem.displayName}</strong>
        <small>${formatNumber(stageItem.completedCount)} / ${formatNumber(stageItem.totalCount)}</small>
      </div>
      <b style="--bar-width:${completed}%"><i></i></b>
    </article>
  `;
}

function issueLane(code: string, label: string): string {
  return `
    <div class="issueLane">
      <span>${code}</span>
      <strong>${label}</strong>
    </div>
  `;
}

function stage(stageKey: string, displayName: string, totalCount: number, completedCount: number, rejectedCount: number): NormalizationStage {
  return {
    stageKey,
    displayName,
    totalCount,
    completedCount,
    rejectedCount,
    remainingCount: Math.max(0, totalCount - completedCount - rejectedCount),
  };
}

function apiStateLabel(): string {
  if (apiState === 'online') return 'API online';
  if (apiState === 'offline') return 'API offline';
  return 'API loading';
}

function ratio(value: number, total: number): number {
  if (total <= 0) return 0;
  return Math.min(100, Math.round((value / total) * 100));
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('en-US').format(value);
}

function formatCompact(value: number): string {
  return new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 }).format(value);
}

function element<T extends HTMLElement>(id: string): T {
  const target = document.getElementById(id);
  if (!target) {
    throw new Error(`Missing element: ${id}`);
  }
  return target as T;
}

function stringValue(form: FormData, key: string): string {
  const value = form.get(key);
  return typeof value === 'string' ? value : '';
}
