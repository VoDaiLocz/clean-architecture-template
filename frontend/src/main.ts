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

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';

const app = document.querySelector<HTMLDivElement>('#app');

if (!app) {
  throw new Error('App root not found');
}

const appRoot = app;

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
    source: 'Validated sample - Part 5 grammar',
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
    source: 'Validated sample - TOEIC vocabulary',
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

if (location.pathname.startsWith('/admin')) {
  renderAdmin();
} else {
  renderLearner();
}

function renderLearner(): void {
  let activeQuestionIndex = 0;
  let selectedAnswer = '';
  let revealed = false;

  appRoot.innerHTML = `
    <main class="learnerShell">
      <aside class="studyNav" aria-label="TOEIC sections">
        <div class="brandBlock">
          <span class="brandMark">T</span>
          <div>
            <strong>TOEIC LR</strong>
            <span>Reading + Listening</span>
          </div>
        </div>
        <nav class="navList">
          <a class="active" href="/">Hôm nay</a>
          <a href="#reading">Reading</a>
          <a href="#listening">Listening</a>
          <a href="#vocab">Từ vựng</a>
          <a href="/admin">Admin</a>
        </nav>
      </aside>

      <section class="studyMain">
        <header class="studyHeader">
          <div>
            <p class="eyebrow">Lộ trình 2 kỹ năng</p>
            <h1>Ôn TOEIC hôm nay</h1>
          </div>
          <div class="targetBox">
            <span>Target</span>
            <strong>700+</strong>
          </div>
        </header>

        <section class="studyGrid">
          <article class="scorePanel">
            <div class="panelHeader">
              <h2>Tiến độ</h2>
              <span class="statusPill success">Đang học</span>
            </div>
            <div class="progressRows">
              <div>
                <span>Reading</span>
                <strong>Part 5</strong>
                <div class="bar"><i style="width: 42%"></i></div>
              </div>
              <div>
                <span>Listening</span>
                <strong>Part 2</strong>
                <div class="bar accent"><i style="width: 28%"></i></div>
              </div>
              <div>
                <span>Từ vựng</span>
                <strong>3 thẻ</strong>
                <div class="bar warm"><i style="width: 58%"></i></div>
              </div>
            </div>
          </article>

          <article class="practicePanel" id="reading">
            <div class="panelHeader">
              <div>
                <h2>Bài luyện nhanh</h2>
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

          <article class="planPanel">
            <div class="panelHeader">
              <h2>Buổi học</h2>
              <span class="statusPill">35 phút</span>
            </div>
            <ol class="taskList">
              <li><strong>10 câu Part 5</strong><span>Grammar + vocabulary</span></li>
              <li><strong>15 câu Part 2</strong><span>Question-response</span></li>
              <li><strong>Ôn 20 từ</strong><span>Business context</span></li>
              <li><strong>Review lỗi sai</strong><span>Chỉ từ item đã validate</span></li>
            </ol>
          </article>
        </section>

        <section class="lowerGrid">
          <article class="vocabPanel" id="vocab">
            <div class="panelHeader">
              <h2>Từ vựng cần ôn</h2>
              <span class="statusPill">Spaced review</span>
            </div>
            <div id="vocabList" class="vocabList"></div>
          </article>

          <article class="listeningPanel" id="listening">
            <div class="panelHeader">
              <h2>Listening queue</h2>
              <span class="statusPill muted">Chờ audio</span>
            </div>
            <div class="audioRows">
              <button type="button" class="audioRow"><span>Part 2</span><strong>Short response drills</strong></button>
              <button type="button" class="audioRow"><span>Part 3</span><strong>Conversation groups</strong></button>
              <button type="button" class="audioRow"><span>Part 4</span><strong>Talk transcript review</strong></button>
            </div>
          </article>
        </section>
      </section>
    </main>
  `;

  const questionMeta = element<HTMLElement>('questionMeta');
  const answerState = element<HTMLElement>('answerState');
  const questionPrompt = element<HTMLElement>('questionPrompt');
  const answerOptions = element<HTMLDivElement>('answerOptions');
  const explanationBox = element<HTMLDivElement>('explanationBox');
  const checkAnswerButton = element<HTMLButtonElement>('checkAnswerButton');
  const nextQuestionButton = element<HTMLButtonElement>('nextQuestionButton');
  const vocabList = element<HTMLDivElement>('vocabList');

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
  renderVocabulary();

  function renderQuestion(): void {
    const question = questions[activeQuestionIndex];
    questionMeta.textContent = `Part ${question.part} · ${question.skill} · ${question.source}`;
    questionPrompt.textContent = question.prompt;
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

  function renderVocabulary(): void {
    vocabList.innerHTML = vocabCards
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
      .join('');
  }
}

function renderAdmin(): void {
  appRoot.innerHTML = `
    <main class="shell">
      <section class="topbar">
        <div>
          <p class="eyebrow">TOEIC LR Knowledge Base</p>
          <h1>Normalization admin</h1>
        </div>
        <div class="topActions">
          <a class="textButton" href="/">Learner app</a>
          <button id="refreshButton" class="iconButton" title="Refresh dashboard" aria-label="Refresh dashboard">
            <span aria-hidden="true">↻</span>
          </button>
        </div>
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

  function showResult(title: string, data: unknown, ok: boolean): void {
    resultState.textContent = title;
    resultState.className = ok ? 'statusPill success' : 'statusPill danger';
    resultOutput.textContent = JSON.stringify(data, null, 2);
  }
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
