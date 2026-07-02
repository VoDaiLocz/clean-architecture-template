import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ApiClientService, LearnerHome } from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

type TodayScreenState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'toeic-today-page',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  template: `
    <section class="today-screen" data-testid="TodayScreen">
      <toeic-page-header
        eyebrow="Today"
        title="Your TOEIC next action"
        description="The backend decides the next required step. The UI only displays the plan, blocker, and progress."
      />

      @if (screenState() === 'loading') {
        <section class="panel state-panel" aria-live="polite">
          <p class="eyebrow">Loading</p>
          <h2>Loading your assigned work</h2>
          <p>Today uses persisted learner state, not browser-only recommendations.</p>
        </section>
      } @else if (screenState() === 'error') {
        <section class="panel state-panel" role="alert">
          <p class="eyebrow">Content unavailable</p>
          <h2>Today cannot load right now</h2>
          <p>{{ loadError() }}</p>
          <a class="secondary-action" routerLink="/onboarding">Check onboarding</a>
        </section>
      } @else if (home(); as learnerHome) {
        <section
          class="primary-panel today-primary"
          data-testid="primaryAssignment"
          data-mobile-order="primary"
        >
          <div>
            <p class="eyebrow">Primary assignment</p>
            <h2>{{ learnerHome.nextActivity.title }}</h2>
            <p class="assignment-context">
              Part {{ learnerHome.currentPart || 'setup' }} · {{ learnerHome.currentUnitTitle }} ·
              {{ learnerHome.nextActivity.activityType }}
            </p>
            <p>{{ primaryReason() }}</p>
          </div>
          <a class="primary-action action-link" [routerLink]="primaryRoute()">{{ primaryCtaLabel() }}</a>
        </section>

        @if (learnerHome.activeSession; as activeSession) {
          <section class="panel active-session-panel" data-testid="active-session">
            <p class="eyebrow">Active session</p>
            <h3>{{ activeSession.label }}</h3>
            <a class="secondary-action" [routerLink]="activeSession.route">Resume</a>
          </section>
        }

        <section class="today-grid">
          <article class="panel blocker-panel" data-testid="blockers" data-mobile-order="blockers">
            <p class="eyebrow">Blockers</p>
            @if (learnerHome.lockedNextUnit; as lockedUnit) {
              <h3>{{ lockedUnit.title }}</h3>
              <p>{{ lockedUnit.learnerMessage }}</p>
              <div class="reason-list" aria-label="Lock reasons">
                @for (reasonCode of lockedUnit.reasonCodes; track reasonCode) {
                  <span>{{ reasonCode }}</span>
                }
              </div>
            } @else if (learnerHome.reviewCount > 0) {
              <h3>{{ learnerHome.reviewCount }} review items waiting</h3>
              <p>Repair open review items before the backend unlocks more advanced work.</p>
            } @else {
              <h3>No active blocker</h3>
              <p>When review or unlock blockers exist, the learner API shows them here.</p>
            }
          </article>

          <article class="panel progress-panel" data-mobile-order="progress">
            <p class="eyebrow">Progress</p>
            @if (learnerHome.pathProgress; as pathProgress) {
              <div class="metric" data-testid="path-progress">
                <strong>{{ pathProgress.percent }}%</strong>
                <span>{{ pathProgress.completedUnits }} / {{ pathProgress.totalUnits }} units</span>
              </div>
            } @else {
              <h3 data-testid="path-progress">Path not generated yet</h3>
              <p>Complete onboarding and placement before path progress appears.</p>
            }

            @if (learnerHome.dailyTarget; as dailyTarget) {
              <div class="metric" data-testid="daily-target">
                <strong>{{ dailyTarget.completedMinutes }} / {{ dailyTarget.targetMinutes }} minutes</strong>
                <span>Daily target progress</span>
              </div>
            } @else {
              <p data-testid="daily-target">Daily target appears after the learner path is generated.</p>
            }
          </article>

          <article class="panel weakness-panel" data-testid="weakest-areas" data-mobile-order="weakness">
            <p class="eyebrow">Weakest areas</p>
            @if (weakestAreas().length > 0) {
              @for (area of weakestAreas(); track area.part + area.skill) {
                <p class="weakness-line">Part {{ area.part }} · {{ area.skill }}</p>
              }
            } @else {
              <h3>Waiting for diagnostic evidence</h3>
              <p>The backend has not returned part or skill weaknesses yet.</p>
            }
          </article>
        </section>
      }
    </section>
  `,
})
export class TodayPageComponent {
  private readonly api = inject(ApiClientService);

  protected readonly screenState = signal<TodayScreenState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly home = signal<LearnerHome | null>(null);
  protected readonly weakestAreas = computed(() => this.home()?.weakestAreas ?? []);
  protected readonly primaryCtaLabel = computed(() => {
    const learnerHome = this.home();
    return learnerHome?.primaryAssignment?.ctaLabel ?? this.defaultCtaLabel(learnerHome);
  });
  protected readonly primaryRoute = computed(() => {
    const learnerHome = this.home();
    return learnerHome?.primaryAssignment?.route ?? this.defaultRoute(learnerHome);
  });
  protected readonly primaryReason = computed(() => {
    const learnerHome = this.home();
    return learnerHome?.primaryAssignment?.reason ?? learnerHome?.nextActivity.description ?? '';
  });

  constructor() {
    this.api.getLearnerHome().subscribe({
      next: (home) => {
        this.home.set(home);
        this.screenState.set('ready');
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'The learner home API is unavailable. The UI will not invent work.');
        this.screenState.set('error');
      },
    });
  }

  private defaultCtaLabel(home: LearnerHome | null): string {
    if (!home) {
      return 'Open learner path';
    }

    if (home.nextActivity.activityType === 'Onboarding') {
      return 'Start onboarding';
    }

    if (home.nextActivity.activityType === 'Placement') {
      return 'Start placement';
    }

    return `Start ${home.nextActivity.activityType.toLowerCase()}`;
  }

  private defaultRoute(home: LearnerHome | null): string {
    if (!home) {
      return '/onboarding';
    }

    if (home.nextActivity.activityType === 'Onboarding' || home.nextActivity.activityType === 'Placement') {
      return '/onboarding';
    }

    return `/learn/${home.nextActivity.activityId}`;
  }
}
