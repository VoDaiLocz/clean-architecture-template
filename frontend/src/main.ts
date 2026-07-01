import './styles.css';
import { getToeicPart, toeicParts, type ToeicPart } from './studyCatalog';

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

type LearnerActivitySummary = {
  activityId: string;
  activityType: 'ConceptLesson' | 'FocusDrill' | 'MiniTest' | 'MistakeRepair';
  title: string;
  description: string;
};

type LockedUnit = {
  unitId: string;
  title: string;
  reasonCodes: string[];
  learnerMessage: string;
};

type LearnerHomeResponse = {
  learnerId: string;
  currentPart: number;
  currentUnitId: string;
  currentUnitTitle: string;
  nextActivity: LearnerActivitySummary;
  reviewCount: number;
  lockedNextUnit: LockedUnit | null;
};

type LearnerActivityResponse = {
  activityId: string;
  unitId: string;
  activityType: 'ConceptLesson' | 'FocusDrill' | 'MiniTest';
  title: string;
  instructions: string;
  lessonPoints: string[];
  question: LearnerQuestionResponse | null;
};

type LearnerQuestionResponse = {
  questionId: string;
  prompt: string;
  options: Record<string, string>;
  correctAnswer: string;
  explanation: string;
};

type LearnerAttemptResponse = {
  activityCompleted: boolean;
  unitCompleted: boolean;
  nextActivity: LearnerActivitySummary;
  reviewCount: number;
  message: string;
};

type LearnerReviewItem = {
  reviewItemId: string;
  unitId: string;
  questionId: string;
  errorTag: string;
  repairPrompt: string;
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
const LEGACY_DEMO_ONLY_FRONTEND_FALLBACK = true;
const LEGACY_DEMO_ONLY_REPLACEMENT_PHASE = 'P7';

const legacyDemoOnlyDashboardFallback: DashboardResponse = {
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

const legacyDemoOnlyLearnerHomeFallback: LearnerHomeResponse = {
  learnerId: 'demo-learner',
  currentPart: 5,
  currentUnitId: 'part5-word-form',
  currentUnitTitle: 'Word Form',
  nextActivity: {
    activityId: 'part5-word-form-lesson',
    activityType: 'ConceptLesson',
    title: 'Học Word Form',
    description: 'Học cách chọn đúng từ loại trước khi làm câu.',
  },
  reviewCount: 0,
  lockedNextUnit: {
    unitId: 'part5-verb-tense',
    title: 'Verb Tense',
    reasonCodes: ['previous_unit_incomplete'],
    learnerMessage: 'Hoàn thành 100% Word Form để mở khóa.',
  },
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
appRoot.dataset.legacyDemoOnlyFallback = String(LEGACY_DEMO_ONLY_FRONTEND_FALLBACK);
appRoot.dataset.legacyDemoOnlyReplacementPhase = LEGACY_DEMO_ONLY_REPLACEMENT_PHASE;

let dashboardState = legacyDemoOnlyDashboardFallback;
let learnerHomeState = legacyDemoOnlyLearnerHomeFallback;
let apiState: 'loading' | 'online' | 'offline' = 'loading';
let lastAdminResult: { title: string; data: unknown; ok: boolean } | null = null;

if (location.pathname.startsWith('/admin')) {
  renderAdmin();
} else {
  renderLearner();
}

void refreshLearnerHome();
void refreshDashboard();

function renderLearner(): void {
  const route = learnerRoute();
  appRoot.innerHTML = `
    <main class="learnerShell">
      <aside class="studyNav" aria-label="TOEIC sections">
        <a class="brandBlock" href="/">
          <span class="brandMark"><i></i></span>
          <span>
            <strong>TOEIC Pro</strong>
            <small>Luyện 7 Part TOEIC</small>
          </span>
        </a>
        <nav class="navList">
          ${navLink('/', 'Hôm nay', route)}
          ${navLink('/practice', '7 Part', route)}
          ${navLink('/listening', 'Nghe', route)}
          ${navLink('/vocabulary', 'Từ vựng', route)}
          ${navLink('/review', 'Lỗi sai', route)}
        </nav>
        <div class="navTelemetry">
          <span>Mục tiêu hôm nay</span>
          <strong>45'</strong>
          <small>1 Part + 1 review set</small>
        </div>
      </aside>

      <section class="studyMain">
        ${renderLearnerScreen(route)}
      </section>
    </main>
  `;

  if (route === '/part/5') {
    wirePractice();
  }

  if (route.startsWith('/learn/')) {
    void wireLearnerActivity(route.replace('/learn/', ''));
  }

  if (route === '/review') {
    void wireReviewScreen();
  }
}

function learnerRoute(): string {
  const path = location.pathname.replace(/\/$/, '') || '/';
  if (/^\/learn\/[^/]+$/.test(path)) {
    return path;
  }

  if (/^\/part\/[1-7]$/.test(path)) {
    return path;
  }

  return ['/', '/practice', '/listening', '/vocabulary', '/review'].includes(path) ? path : '/';
}

function renderLearnerScreen(route: string): string {
  if (route.startsWith('/learn/')) {
    return learnerActivityScreen(route.replace('/learn/', ''));
  }

  if (route.startsWith('/part/')) {
    return partDetailScreen(Number(route.split('/').at(-1)));
  }

  if (route === '/practice') return practiceScreen();
  if (route === '/listening') return listeningScreen();
  if (route === '/vocabulary') return vocabularyScreen();
  if (route === '/review') return reviewScreen();
  return overviewScreen();
}

function navLink(href: string, label: string, route: string): string {
  const isActive = route === href || (href === '/practice' && route.startsWith('/part/')) || (href === '/' && route.startsWith('/learn/'));
  return `<a class="${isActive ? 'active' : ''}" href="${href}">${label}</a>`;
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
        <strong>990</strong>
      </div>
    </header>
  `;
}

function overviewScreen(): string {
  const nextActivity = learnerHomeState.nextActivity;
  return `
    ${pageHeader('Hôm nay', 'Tiếp tục học để mở khóa bài sau', `Part ${learnerHomeState.currentPart}`)}
    <section class="studyHero">
      <div class="studyHeroCopy">
        <span class="liveChip">${nextActivity.activityType}</span>
        <h2>${nextActivity.title}</h2>
        <p>${nextActivity.description}</p>
        <div class="heroActions">
          <a class="primaryLinkButton" href="${activityHref(nextActivity)}">Tiếp tục học</a>
          <a class="secondaryLinkButton" href="/practice">Xem 7 Part</a>
        </div>
      </div>
      <div class="todayPlan" aria-label="Today's TOEIC plan">
        <div class="planScore">
          <span>Đang học</span>
          <strong>Part ${learnerHomeState.currentPart}</strong>
          <small>${learnerHomeState.currentUnitTitle}</small>
        </div>
        <div class="unlockBox">
          <span>Bài tiếp theo</span>
          <strong>${learnerHomeState.lockedNextUnit?.title ?? 'Đã mở khóa'}</strong>
          <small>${learnerHomeState.lockedNextUnit?.learnerMessage ?? 'Bạn có thể học bài kế tiếp.'}</small>
        </div>
        <a class="compactPartRow" href="/review">
          <span>Review</span>
          <strong>${learnerHomeState.reviewCount} lỗi cần sửa</strong>
          <small>Sửa lỗi để unlock</small>
        </a>
      </div>
    </section>

    <section class="learnerSection">
      <div class="panelHeader">
        <div>
          <h2>7 Part TOEIC</h2>
          <span class="subtle">Xem tổng quan từng Part. Hệ thống vẫn dẫn bài tiếp theo theo tiến độ của bạn.</span>
        </div>
        <span class="statusPill success">${totalAvailableTests()} đề luyện</span>
      </div>
      <div class="partGrid">
        ${toeicParts.map(partCard).join('')}
      </div>
    </section>

    <section class="learningLayout">
      <article class="panel">
        <h2>Luyện đề</h2>
        <div class="utilityGrid">
          <a href="/practice"><strong>Mini test</strong><span>Mở sau từng unit.</span></a>
          <a href="/practice"><strong>Part test</strong><span>Mở khi hoàn thành đủ unit của Part.</span></a>
          <a href="/practice"><strong>Full TOEIC test</strong><span>Dùng để đo tiến độ 800+ sau khi có nền.</span></a>
        </div>
      </article>
      <aside class="panel">
        <h2>Ôn lỗi</h2>
        <p class="bodyText">Lỗi sai sẽ quay lại đúng lúc. Nếu còn lỗi chặn, bài tiếp theo vẫn khóa.</p>
      </aside>
    </section>
  `;
}

function practiceScreen(): string {
  return `
    ${pageHeader('Kho luyện tập', 'Chọn Part muốn tăng điểm', `${totalAvailableTests()} đề`)}
    <section class="learnerSection">
      <div class="partGrid large">
        ${toeicParts.map(partCard).join('')}
      </div>
    </section>
  `;
}

function learnerActivityScreen(activityId: string): string {
  return `
    ${pageHeader('Bài học', 'Đang tải hoạt động học', learnerHomeState.currentUnitTitle)}
    <section class="practiceWorkspace">
      <article id="learnerActivityPanel" class="practicePanel" data-activity-id="${activityId}">
        <div class="panelHeader">
          <div>
            <h2>Đang tải...</h2>
            <span class="subtle">Hệ thống đang lấy bài học tiếp theo.</span>
          </div>
          <span class="statusPill muted">Loading</span>
        </div>
      </article>

      <aside class="tracePanel">
        <h2>Điều kiện mở khóa</h2>
        <ul class="checkList">
          <li>Học lesson</li>
          <li>Hoàn thành drill</li>
          <li>Mini test đạt 80%</li>
          <li>Sửa hết lỗi sai</li>
        </ul>
      </aside>
    </section>
  `;
}

function renderLearnerActivity(activity: LearnerActivityResponse): string {
  const question = activity.question;
  return `
    <div class="panelHeader">
      <div>
        <h2>${activity.title}</h2>
        <span class="subtle">${activity.instructions}</span>
      </div>
      <span class="statusPill">${activity.activityType}</span>
    </div>
    ${
      activity.lessonPoints.length > 0
        ? `<div class="lessonPoints">${activity.lessonPoints.map((point) => `<p>${point}</p>`).join('')}</div>`
        : ''
    }
    ${
      question
        ? `<div class="questionBlock">
            <p class="questionPrompt">${question.prompt}</p>
            <div class="answerOptions static">
              ${Object.entries(question.options)
                .map(
                  ([label, text]) => `<div class="answerOption">
                    <span>${label}</span>
                    <strong>${text}</strong>
                  </div>`,
                )
                .join('')}
            </div>
            <div class="explanationBox"><strong>Giải thích:</strong> ${question.explanation}</div>
          </div>`
        : ''
    }
    <div class="practiceActions">
      ${activityActionButtons(activity)}
    </div>
    <div id="activityResult" class="activityResult" aria-live="polite"></div>
  `;
}

function activityActionButtons(activity: LearnerActivityResponse): string {
  if (activity.activityType === 'ConceptLesson') {
    return `<button type="button" data-activity-outcome="lesson">Đã hiểu lesson</button>`;
  }

  if (activity.activityType === 'FocusDrill') {
    return `<button type="button" data-activity-outcome="drill">Hoàn thành drill 15/15</button>`;
  }

  return `
    <button type="button" data-activity-outcome="mini-fail" class="secondaryButton">Nộp 7/10</button>
    <button type="button" data-activity-outcome="mini-pass">Nộp 9/10</button>
  `;
}

function partDetailScreen(partId: number): string {
  const part = getToeicPart(partId) ?? toeicParts[0];
  return `
    ${pageHeader(part.skill, part.title, `${part.availableTests} đề`)}
    <section class="partDetailHero">
      <div>
        <span class="statusPill">${part.level}</span>
        <h2>${part.shortName}</h2>
        <p>${part.userOutcome}</p>
        <div class="partStats">
          ${partStat('Câu/de', part.questionCount)}
          ${partStat('Đề luyện', part.availableTests)}
          ${partStat('Phút/de', part.durationMinutes)}
        </div>
        <div class="heroActions">
          <a class="primaryLinkButton" href="#study-now">Vào học Part ${part.id}</a>
          <a class="secondaryLinkButton" href="/practice">Đổi Part</a>
        </div>
      </div>
      <aside class="partMiniPlan">
        <h2>Học Part ${part.id} như thế nào?</h2>
        <div class="roadmapList">
          ${part.roadmap.map((step, index) => roadmapItem(`Bước ${index + 1}`, step, part.studyActions[index] ?? 'Luyện và sửa lỗi')).join('')}
        </div>
      </aside>
    </section>

    <section id="study-now" class="practiceWorkspace">
      <article class="practicePanel">
        ${part.id === 5 ? partFiveDrillMarkup() : partComingSoonMarkup(part)}
      </article>

      <aside class="tracePanel">
        <h2>Cần tránh</h2>
        <ul class="checkList">
          ${part.commonMistakes.map((mistake) => `<li>${mistake}</li>`).join('')}
        </ul>
      </aside>
    </section>
  `;
}

function partFiveDrillMarkup(): string {
  return `
    <div class="panelHeader">
      <div>
        <h2>Bài luyện mẫu</h2>
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
  `;
}

function partComingSoonMarkup(part: ToeicPart): string {
  return `
    <div class="panelHeader">
      <div>
        <h2>Buổi học Part ${part.id}</h2>
        <span class="subtle">${part.availableTests} đề luyện đã lên kế hoạch</span>
      </div>
      <span class="statusPill">Ready</span>
    </div>
    <div class="lessonPreview">
      ${part.studyActions.map((action) => `<button type="button">${action}</button>`).join('')}
    </div>
  `;
}

function partCard(part: ToeicPart): string {
  return `
    <a class="partCard ${part.skill.toLowerCase()}" href="/part/${part.id}">
      <span>${part.skill}</span>
      <strong>${part.title}</strong>
      <small>${part.shortName}</small>
      <p>${part.userOutcome}</p>
      <div>
        <b>${part.availableTests} đề</b>
        <b>${part.questionCount} câu/de</b>
      </div>
    </a>
  `;
}

function roadmapItem(step: string, title: string, detail: string): string {
  return `
    <div class="roadmapItem">
      <span>${step}</span>
      <strong>${title}</strong>
      <small>${detail}</small>
    </div>
  `;
}

function partStat(label: string, value: number): string {
  return `
    <div>
      <span>${label}</span>
      <strong>${value}</strong>
    </div>
  `;
}

function totalAvailableTests(): number {
  return toeicParts.reduce((sum, part) => sum + part.availableTests, 0);
}

function listeningParts(): ToeicPart[] {
  return toeicParts.filter((part) => part.skill === 'Listening');
}

function activityHref(activity: LearnerActivitySummary): string {
  if (activity.activityType === 'MistakeRepair') {
    return '/review';
  }

  return `/learn/${activity.activityId}`;
}

function listeningScreen(): string {
  return `
    ${pageHeader('Listening', 'Luyện nghe Part 1-4', '4 Part')}
    <section class="twoColumn">
      <article class="panel listeningDeck">
        <div class="panelHeader">
          <h2>Chọn dạng nghe</h2>
          <span class="statusPill">${listeningParts().reduce((sum, part) => sum + part.availableTests, 0)} đề</span>
        </div>
        <div class="waveform" aria-hidden="true">${'<i></i>'.repeat(12)}</div>
        <div class="audioRows">
          ${listeningParts().map((part) => `<a class="audioRow" href="/part/${part.id}"><span>${part.title}</span><strong>${part.shortName}</strong></a>`).join('')}
        </div>
      </article>
      <aside class="panel">
        <h2>Cách luyện nghe</h2>
        <ul class="checkList">
          <li class="pass">Nghe lần 1 để chọn đáp án, không dừng audio</li>
          <li class="pass">Nghe lại câu sai và đọc transcript</li>
          <li class="pass">Lưu bẫy âm, số liệu, người nói vào lỗi sai</li>
        </ul>
      </aside>
    </section>
  `;
}

function vocabularyScreen(): string {
  return `
    ${pageHeader('Vocabulary', 'Từ vựng hay gặp trong TOEIC', 'Daily')}
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
    ${pageHeader('Review lỗi', 'Sửa lỗi để lần sau không mất điểm', 'Smart review')}
    <section class="twoColumn">
      <article class="panel">
        <div class="panelHeader">
          <h2>Lỗi cần ôn</h2>
          <span id="reviewCountPill" class="statusPill muted">Đang tải</span>
        </div>
        <div id="reviewList" class="issueBoard"></div>
      </article>
      <aside class="panel">
        <h2>Luật review</h2>
        <ul class="checkList">
          <li class="pass">Làm lại câu sai sau 24 giờ</li>
          <li class="pass">Ghi lý do sai bằng một dòng ngắn</li>
          <li class="pass">Chỉ tăng độ khó khi đạt 80% mini test</li>
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

async function refreshLearnerHome(forceRender = false): Promise<void> {
  try {
    const response = await fetch(`${API_BASE}/api/learner/home`);
    if (!response.ok) {
      if (forceRender) rerenderCurrentRoute();
      return;
    }

    learnerHomeState = (await response.json()) as LearnerHomeResponse;
  } catch {
    if (forceRender) rerenderCurrentRoute();
    return;
  }

  if (forceRender) {
    rerenderCurrentRoute();
  }
}

async function wireLearnerActivity(activityId: string): Promise<void> {
  const panel = document.getElementById('learnerActivityPanel');
  if (!panel) return;

  try {
    const response = await fetch(`${API_BASE}/api/learner/activities/${activityId}`);
    if (!response.ok) {
      panel.innerHTML = `<h2>Không tìm thấy bài học</h2><p class="bodyText">Bài này chưa được mở hoặc không tồn tại.</p>`;
      return;
    }

    const activity = (await response.json()) as LearnerActivityResponse;
    panel.innerHTML = renderLearnerActivity(activity);
    panel.querySelectorAll<HTMLButtonElement>('[data-activity-outcome]').forEach((button) => {
      button.addEventListener('click', () => {
        void submitLearnerActivity(activity, button.dataset.activityOutcome ?? '');
      });
    });
  } catch {
    panel.innerHTML = `<h2>Không kết nối được bài học</h2><p class="bodyText">Kiểm tra API backend rồi thử lại.</p>`;
  }
}

async function submitLearnerActivity(activity: LearnerActivityResponse, outcome: string): Promise<void> {
  const payload =
    outcome === 'mini-fail'
      ? { correctCount: 7, totalCount: 10, wrongItemIds: ['p5-word-form-007'], errorTag: 'word_form' }
      : outcome === 'mini-pass'
        ? { correctCount: 9, totalCount: 10, wrongItemIds: [], errorTag: 'word_form' }
        : outcome === 'drill'
          ? { correctCount: 15, totalCount: 15, wrongItemIds: [], errorTag: 'word_form' }
          : {};

  const response = await fetch(`${API_BASE}/api/learner/activities/${activity.activityId}/attempts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  const result = (await response.json()) as LearnerAttemptResponse;
  const resultBox = document.getElementById('activityResult');
  if (resultBox) {
    resultBox.innerHTML = `
      <strong>${result.unitCompleted ? 'Đã mở khóa bài tiếp theo' : 'Đã lưu tiến độ'}</strong>
      <p>${result.message}</p>
      <a class="primaryLinkButton" href="${activityHref(result.nextActivity)}">${result.nextActivity.title}</a>
    `;
  }

  await refreshLearnerHome();
}

async function wireReviewScreen(): Promise<void> {
  const reviewList = document.getElementById('reviewList');
  const reviewCountPill = document.getElementById('reviewCountPill');
  if (!reviewList || !reviewCountPill) return;

  const response = await fetch(`${API_BASE}/api/learner/review`);
  const items = (await response.json()) as LearnerReviewItem[];
  reviewCountPill.textContent = `${items.length} lỗi`;

  if (items.length === 0) {
    reviewList.innerHTML = `<p class="bodyText">Không còn lỗi chặn. Quay lại học tiếp để mở khóa bài sau.</p>`;
    return;
  }

  reviewList.innerHTML = items
    .map(
      (item) => `
        <div class="issueLane">
          <span>${item.errorTag}</span>
          <strong>${item.repairPrompt}</strong>
          <button type="button" data-review-id="${item.reviewItemId}">Đã sửa lỗi này</button>
        </div>
      `,
    )
    .join('');

  reviewList.querySelectorAll<HTMLButtonElement>('[data-review-id]').forEach((button) => {
    button.addEventListener('click', () => {
      void submitReviewItem(button.dataset.reviewId ?? '');
    });
  });
}

async function submitReviewItem(reviewItemId: string): Promise<void> {
  const response = await fetch(`${API_BASE}/api/learner/review/${encodeURIComponent(reviewItemId)}/attempts`, {
    method: 'POST',
  });
  const result = (await response.json()) as LearnerAttemptResponse;
  await refreshLearnerHome();
  const reviewList = document.getElementById('reviewList');
  if (reviewList) {
    reviewList.innerHTML = `
      <p class="bodyText">${result.message}</p>
      <a class="primaryLinkButton" href="${activityHref(result.nextActivity)}">${result.nextActivity.title}</a>
    `;
  }
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
  const corpus = { ...legacyDemoOnlyDashboardFallback.corpus, ...value.corpus };
  const stages = value.normalizationStages?.length
    ? value.normalizationStages
    : legacyDemoOnlyDashboardFallback.normalizationStages;
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
  return dashboardState.normalizationStages.length
    ? dashboardState.normalizationStages
    : legacyDemoOnlyDashboardFallback.normalizationStages;
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
