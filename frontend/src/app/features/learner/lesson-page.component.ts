import { Component, Input, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import {
  ApiClientService,
  CompleteLessonResponse,
  GuidedExample,
  LearnerNextAction,
  LessonConcept,
  LessonMedia,
  LessonResponse,
} from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

type LessonState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'toeic-lesson-header',
  standalone: true,
  template: `
    <section class="panel lesson-header" data-testid="LessonHeader">
      <span class="part-badge">Part {{ part }}</span>
      <h2>{{ title }}</h2>
      <p>{{ objective }}</p>
      <p class="assignment-context">{{ skill }}</p>
    </section>
  `,
})
export class LessonHeaderComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) objective = '';
  @Input({ required: true }) part = 0;
  @Input({ required: true }) skill = '';
}

@Component({
  selector: 'toeic-lesson-content-body',
  standalone: true,
  template: `
    <section class="panel lesson-reader" data-testid="LessonContentBody">
      <p class="eyebrow">Concept</p>
      <h3>{{ concept.heading }}</h3>
      <p>{{ concept.body }}</p>

      @if (media?.audioUrl) {
        <audio class="lesson-audio" controls [src]="media?.audioUrl ?? ''"></audio>
      }

      @if (media?.imageUrl) {
        <img class="lesson-image" [src]="media?.imageUrl ?? ''" alt="Lesson visual evidence" />
      }

      @if (passage) {
        <article class="reading-panel passage-box">
          <p>{{ passage }}</p>
        </article>
      }
    </section>
  `,
})
export class LessonContentBodyComponent {
  @Input({ required: true }) concept!: LessonConcept;
  @Input() media: LessonMedia | null = null;
  @Input() passage: string | null = null;
}

@Component({
  selector: 'toeic-guided-example',
  standalone: true,
  template: `
    <section class="panel guided-example" data-testid="GuidedExample">
      <p class="eyebrow">Guided example</p>
      <h3>{{ example.prompt }}</h3>
      <p>{{ example.question }}</p>

      @if (!isRevealed()) {
        <button class="secondary-action" type="button" (click)="reveal()">Reveal guided answer</button>
      } @else {
        <div class="reveal-box">
          <p class="eyebrow">Answer after reveal</p>
          <h3>{{ example.answer }}</h3>
          <p>{{ example.rationale }}</p>
          @if (trap) {
            <p class="trap-note">{{ trap }}</p>
          }
        </div>
      }
    </section>
  `,
})
export class GuidedExampleComponent {
  @Input({ required: true }) example!: GuidedExample;
  @Input() trap: string | null = null;
  protected readonly isRevealed = signal(false);

  protected reveal(): void {
    this.isRevealed.set(true);
  }
}

@Component({
  selector: 'toeic-lesson-next-action-footer',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="panel next-action-footer" data-testid="LessonNextActionFooter">
      <div>
        <p class="eyebrow">Next action</p>
        <h3>{{ nextAction.code }}</h3>
        <p>{{ nextAction.reason }}</p>
      </div>
      <a class="primary-action action-link" [routerLink]="nextActionRoute">Continue</a>
    </section>
  `,
})
export class LessonNextActionFooterComponent {
  @Input({ required: true }) nextAction!: LearnerNextAction;

  protected get nextActionRoute(): string {
    return this.nextAction.apiRoute.startsWith('/') ? this.nextAction.apiRoute : '/today';
  }
}

@Component({
  selector: 'toeic-lesson-page',
  standalone: true,
  imports: [
    RouterLink,
    PageHeaderComponent,
    LessonHeaderComponent,
    LessonContentBodyComponent,
    GuidedExampleComponent,
    LessonNextActionFooterComponent,
  ],
  template: `
    <toeic-page-header
      eyebrow="Lesson"
      title="Study before practice"
      description="Published lessons teach the concept first, then reveal guided examples under learner control."
    />

    @if (state() === 'loading') {
      <section class="panel state-panel" aria-live="polite">
        <p class="eyebrow">Loading</p>
        <h2>Loading published lesson</h2>
        <p>The learner UI waits for approved lesson content from the API.</p>
      </section>
    } @else if (state() === 'error') {
      <section class="panel state-panel" role="alert">
        <p class="eyebrow">Content unavailable</p>
        <h2>Lesson cannot load right now</h2>
        <p>{{ loadError() }}</p>
        <a class="secondary-action" routerLink="/learn">Back to Learn</a>
      </section>
    } @else if (lesson(); as publishedLesson) {
      <section class="lesson-layout">
        <toeic-lesson-header
          [title]="publishedLesson.title"
          [objective]="publishedLesson.objective"
          [part]="publishedLesson.part"
          [skill]="publishedLesson.skill"
        />
        <toeic-lesson-content-body
          [concept]="publishedLesson.concept"
          [media]="publishedLesson.media"
          [passage]="publishedLesson.passage"
        />
        <toeic-guided-example [example]="publishedLesson.example" [trap]="publishedLesson.trap" />
      </section>

      @if (completion(); as completed) {
        <toeic-lesson-next-action-footer [nextAction]="completed.nextAction" />
      } @else {
        <section class="panel next-action-footer" data-testid="LessonNextActionFooter">
          <div>
            <p class="eyebrow">Required completion</p>
            <h3>{{ publishedLesson.nextAction.code }}</h3>
            <p>{{ publishedLesson.nextAction.reason }}</p>
          </div>
          <button class="primary-action" type="button" [disabled]="isCompleting()" (click)="completeLesson()">
            {{ isCompleting() ? 'Completing lesson' : 'Complete lesson' }}
          </button>
        </section>
      }
    }
  `,
})
export class LessonPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClientService);

  protected readonly state = signal<LessonState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly lesson = signal<LessonResponse | null>(null);
  protected readonly completion = signal<CompleteLessonResponse | null>(null);
  protected readonly isCompleting = signal(false);
  protected readonly lessonId = computed(() => this.route.snapshot.paramMap.get('lessonId') ?? 'active');

  constructor() {
    this.api.getLesson(this.lessonId()).subscribe({
      next: (lesson) => {
        this.lesson.set(lesson);
        this.state.set('ready');
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'Published lesson content is unavailable. The UI will not show source files as a substitute.');
        this.state.set('error');
      },
    });
  }

  protected completeLesson(): void {
    const lesson = this.lesson();
    if (!lesson) {
      return;
    }

    this.isCompleting.set(true);
    this.api.completeLesson(lesson.activitySessionId).subscribe({
      next: (completion) => {
        this.completion.set(completion);
        this.isCompleting.set(false);
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'The lesson completion could not be saved.');
        this.state.set('error');
        this.isCompleting.set(false);
      },
    });
  }
}
