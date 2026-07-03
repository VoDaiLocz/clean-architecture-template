import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import {
  ApiClientService,
  PracticeActivityResponse,
  PracticeQuestion,
  SubmitAttemptAnswer,
  SubmitAttemptResponse,
} from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

type PracticeState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'toeic-drill-mini-test-page',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  template: `
    <section class="practice-answer-screen" data-testid="DrillMiniTestScreen">
      <toeic-page-header
        eyebrow="Practice"
        title="Drill and mini test"
        description="Answers are submitted to the TOEIC engine. Results, review work, and unlocks come from the backend."
      />

      @if (state() === 'loading') {
        <section class="panel state-panel" aria-live="polite">
          <p class="eyebrow">Loading</p>
          <h2>Loading assigned practice</h2>
          <p>The UI waits for the assigned activity and lock state.</p>
        </section>
      } @else if (state() === 'error') {
        <section class="panel state-panel" role="alert">
          <p class="eyebrow">Content unavailable</p>
          <h2>Practice cannot load right now</h2>
          <p>{{ loadError() }}</p>
          <a class="secondary-action" routerLink="/practice">Back to Practice</a>
        </section>
      } @else if (activity(); as practiceActivity) {
        @if (practiceActivity.isLocked) {
          <section class="panel state-panel" data-testid="locked-practice-state">
            <p class="eyebrow">Locked</p>
            <h2>{{ practiceActivity.title }}</h2>
            <p>{{ practiceActivity.lockReason }}</p>
            <a class="secondary-action" routerLink="/today">Back to Today</a>
          </section>
        } @else if (result(); as attemptResult) {
          <section class="primary-panel" data-testid="SubmitAttemptResult">
            <div>
              <p class="eyebrow">Result</p>
              <h2>{{ attemptResult.resultLabel }}</h2>
              <p>{{ attemptResult.answeredCount }} / {{ attemptResult.totalCount }} answered · {{ attemptResult.scorePercent }}%</p>
              <p>{{ attemptResult.nextAction.reason }}</p>
            </div>
            <a class="primary-action action-link" [routerLink]="attemptResult.nextAction.apiRoute">
              {{ attemptResult.nextAction.code }}
            </a>
          </section>
        } @else if (currentQuestion(); as question) {
          <section class="practice-layout">
            <article class="panel question-panel">
              <div class="question-meta">
                <span class="part-badge">Part {{ practiceActivity.part }}</span>
                <span>{{ practiceActivity.mode }}</span>
                <span data-testid="question-progress">Question {{ currentIndex() + 1 }} of {{ totalQuestions() }}</span>
              </div>
              <h2>{{ practiceActivity.title }}</h2>
              <p class="assignment-context">{{ practiceActivity.skill }}</p>

              @if (question.audioUrl) {
                <audio controls [src]="question.audioUrl"></audio>
              }

              @if (question.passage) {
                <article class="reading-panel passage-box">
                  <p>{{ question.passage }}</p>
                </article>
              }

              <h3>{{ question.prompt }}</h3>
              <fieldset class="choice-list">
                <legend>Choose one answer</legend>
                @for (choice of question.choices; track choice) {
                  <label>
                    <input
                      type="radio"
                      [name]="'practice-choice-' + question.id"
                      [checked]="selectedChoice(question.id) === choice"
                      (change)="selectChoice(question.id, choice)"
                    />
                    <span>{{ choice }}</span>
                  </label>
                }
              </fieldset>
            </article>

            <aside class="panel sticky-controls">
              <p class="eyebrow">Attempt controls</p>
              <p class="unanswered-indicator">{{ unansweredCount() }} unanswered</p>
              <div class="control-row">
                <button class="secondary-action" type="button" (click)="previousQuestion()" [disabled]="currentIndex() === 0">
                  Previous
                </button>
                @if (practiceActivity.allowSkip) {
                  <button class="secondary-action" type="button" (click)="skipQuestion()">Skip question</button>
                }
              </div>

              @if (isLastQuestion()) {
                <button class="primary-action" type="button" (click)="openSubmitConfirmation()">
                  Submit {{ practiceActivity.mode === 'MiniTest' ? 'mini test' : 'drill' }}
                </button>
              } @else {
                <button class="primary-action" type="button" (click)="nextQuestion()">Next question</button>
              }

              @if (isConfirmingSubmit()) {
                <div class="confirm-box" role="alertdialog" aria-label="Confirm attempt submit">
                  <h3>Submit attempt?</h3>
                  <p>Result and review work will be returned by the backend after submission.</p>
                  <button class="primary-action" type="button" (click)="submitAttempt()">Confirm submit</button>
                </div>
              }
            </aside>
          </section>
        } @else {
          <section class="panel state-panel">
            <p class="eyebrow">No questions</p>
            <h2>Practice content is not available</h2>
            <p>The UI will not create local questions when the API returns none.</p>
          </section>
        }
      }
    </section>
  `,
})
export class DrillMiniTestPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClientService);

  protected readonly state = signal<PracticeState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly activity = signal<PracticeActivityResponse | null>(null);
  protected readonly result = signal<SubmitAttemptResponse | null>(null);
  protected readonly currentIndex = signal(0);
  protected readonly answers = signal<Record<string, SubmitAttemptAnswer>>({});
  protected readonly isConfirmingSubmit = signal(false);
  protected readonly totalQuestions = computed(() => this.activity()?.questions.length ?? 0);
  protected readonly currentQuestion = computed<PracticeQuestion | null>(() => {
    const activity = this.activity();
    return activity?.questions[this.currentIndex()] ?? null;
  });
  protected readonly isLastQuestion = computed(() => this.currentIndex() === this.totalQuestions() - 1);
  protected readonly unansweredCount = computed(() => {
    const activity = this.activity();
    if (!activity) {
      return 0;
    }

    return activity.questions.filter((question) => !this.answers()[question.id]).length;
  });

  constructor() {
    const activityId = this.route.snapshot.paramMap.get('activityId') ?? 'active';
    this.api.getPracticeActivity(activityId).subscribe({
      next: (activity) => {
        this.activity.set(activity);
        this.state.set('ready');
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'Assigned practice content is unavailable.');
        this.state.set('error');
      },
    });
  }

  protected selectedChoice(questionId: string): string | null {
    return this.answers()[questionId]?.selectedChoice ?? null;
  }

  protected selectChoice(questionId: string, choice: string): void {
    this.answers.update((answers) => ({
      ...answers,
      [questionId]: { questionId, selectedChoice: choice, skipped: false },
    }));
  }

  protected skipQuestion(): void {
    const question = this.currentQuestion();
    if (!question) {
      return;
    }

    this.answers.update((answers) => ({
      ...answers,
      [question.id]: { questionId: question.id, selectedChoice: null, skipped: true },
    }));
    this.nextQuestion();
  }

  protected previousQuestion(): void {
    this.currentIndex.update((index) => Math.max(0, index - 1));
  }

  protected nextQuestion(): void {
    this.currentIndex.update((index) => Math.min(this.totalQuestions() - 1, index + 1));
  }

  protected openSubmitConfirmation(): void {
    this.isConfirmingSubmit.set(true);
  }

  protected submitAttempt(): void {
    const activity = this.activity();
    if (!activity?.activitySessionId) {
      this.loadError.set('The API did not return an activity session for submission.');
      this.state.set('error');
      return;
    }

    this.api
      .submitPracticeAttempt(activity.activitySessionId, {
        answers: Object.values(this.answers()),
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.isConfirmingSubmit.set(false);
        },
        error: (error) => {
          this.loadError.set(error?.error?.message ?? 'The attempt could not be submitted.');
          this.state.set('error');
        },
      });
  }
}
