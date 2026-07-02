import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import {
  ApiClientService,
  PlacementAnswer,
  PlacementQuestion,
  PlacementResultResponse,
  PlacementSessionResponse,
} from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-placement-page',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Placement"
      title="TOEIC placement diagnosis"
      description="This is a diagnostic estimate. It is not an official TOEIC score and it does not reveal answers before submit."
    />

    @if (isLoading()) {
      <section class="panel state-panel" aria-live="polite">
        <p class="eyebrow">Loading</p>
        <h2>Resuming your placement session</h2>
        <p>Answers already held by the backend stay attached to the active placement session.</p>
      </section>
    } @else if (loadError()) {
      <section class="panel state-panel" role="alert">
        <p class="eyebrow">Content unavailable</p>
        <h2>Placement cannot load right now</h2>
        <p>{{ loadError() }}</p>
        <a class="secondary-action" routerLink="/today">Back to Today</a>
      </section>
    } @else if (result(); as placementResult) {
      <section class="primary-panel result-panel">
        <div>
          <p class="eyebrow">Estimated score band</p>
          <h2>{{ placementResult.estimateBand }}</h2>
          <p>{{ placementResult.nextAction.reason }}</p>
        </div>
        <a class="primary-action action-link" routerLink="/today" data-testid="placement-result-next-action">
          {{ placementResult.nextAction.code }}
        </a>
      </section>

      <section class="grid two">
        <article class="panel">
          <p class="eyebrow">Result type</p>
          <h2>{{ placementResult.label }}</h2>
          <p>This label is an estimate for path generation, not an official TOEIC score.</p>
        </article>
        <article class="panel">
          <p class="eyebrow">Weaknesses</p>
          @for (weakness of placementResult.weaknesses; track weakness.part + weakness.skill) {
            <p class="weakness-line">Part {{ weakness.part }} · {{ weakness.skill }}</p>
          }
        </article>
      </section>
    } @else if (currentQuestion(); as question) {
      <section class="placement-layout">
        <article class="panel question-panel">
          <div class="question-meta">
            <span class="part-badge">Part {{ question.part }}</span>
            <span data-testid="placement-progress">Question {{ currentIndex() + 1 }} of {{ totalQuestions() }}</span>
          </div>

          <h2>{{ question.prompt }}</h2>

          <fieldset class="choice-list">
            <legend>Choose one answer or skip</legend>
            @for (choice of question.choices; track choice) {
              <label>
                <input
                  type="radio"
                  [name]="'placement-choice-' + question.id"
                  [checked]="selectedChoice(question.id) === choice"
                  (change)="selectChoice(question.id, choice)"
                />
                <span>{{ choice }}</span>
              </label>
            }
          </fieldset>
        </article>

        <aside class="panel placement-controls">
          <p class="eyebrow">Session controls</p>
          <p>Use explicit skip when you do not know the answer. The final result appears only after submit.</p>
          <div class="control-row">
            <button class="secondary-action" type="button" (click)="previousQuestion()" [disabled]="currentIndex() === 0">
              Previous
            </button>
            <button class="secondary-action" type="button" (click)="skipQuestion()">Skip question</button>
          </div>
          @if (isLastQuestion()) {
            <button class="primary-action" type="button" (click)="openSubmitConfirmation()">Submit placement</button>
          } @else {
            <button class="primary-action" type="button" (click)="nextQuestion()">Next question</button>
          }

          @if (isConfirmingSubmit()) {
            <div class="confirm-box" role="alertdialog" aria-label="Confirm placement submit">
              <h3>Submit diagnostic?</h3>
              <p>Skipped answers remain skipped. You cannot see result feedback until the backend accepts submission.</p>
              <button class="primary-action" type="button" (click)="submitPlacement()">Confirm submit</button>
            </div>
          }
        </aside>
      </section>
    } @else {
      <section class="panel state-panel">
        <p class="eyebrow">No placement content</p>
        <h2>Placement is not available yet</h2>
        <p>The learner API did not return placement questions. The UI will not create fake diagnostic items.</p>
      </section>
    }
  `,
})
export class PlacementPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClientService);

  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly session = signal<PlacementSessionResponse | null>(null);
  protected readonly result = signal<PlacementResultResponse | null>(null);
  protected readonly currentIndex = signal(0);
  protected readonly answers = signal<Record<string, PlacementAnswer>>({});
  protected readonly isConfirmingSubmit = signal(false);

  protected readonly totalQuestions = computed(() => this.session()?.questions.length ?? 0);
  protected readonly currentQuestion = computed<PlacementQuestion | null>(() => {
    const session = this.session();
    return session?.questions[this.currentIndex()] ?? null;
  });
  protected readonly isLastQuestion = computed(() => this.currentIndex() === this.totalQuestions() - 1);

  constructor() {
    const sessionId = this.route.snapshot.paramMap.get('sessionId') ?? 'active';
    this.api.getPlacementSession(sessionId).subscribe({
      next: (session) => {
        this.session.set(session);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'The placement content endpoint is not ready or is temporarily unavailable.');
        this.isLoading.set(false);
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

  protected submitPlacement(): void {
    const session = this.session();
    if (!session) {
      return;
    }

    this.api
      .submitPlacement(session.sessionId, {
        answers: Object.values(this.answers()),
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.isConfirmingSubmit.set(false);
        },
        error: (error) => {
          this.loadError.set(error?.error?.message ?? 'The placement result could not be submitted. Try again later.');
          this.isConfirmingSubmit.set(false);
        },
      });
  }
}
