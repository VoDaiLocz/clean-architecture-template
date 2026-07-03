import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ApiClientService, ToeicPartOverviewItem } from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

type PartOverviewState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'toeic-practice-page',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Practice"
      title="Seven TOEIC parts"
      description="Part navigation is structured around backend progress, lock reasons, and test availability."
    />

    @if (state() === 'loading') {
      <section class="panel state-panel" aria-live="polite">
        <p class="eyebrow">Loading</p>
        <h2>Loading TOEIC part progress</h2>
        <p>The overview waits for backend progress and lock state.</p>
      </section>
    } @else if (state() === 'error') {
      <section class="panel state-panel" role="alert">
        <p class="eyebrow">Content unavailable</p>
        <h2>Part overview cannot load right now</h2>
        <p>{{ loadError() }}</p>
      </section>
    } @else if (parts().length === 0) {
      <section class="panel state-panel">
        <p class="eyebrow">No published part data</p>
        <h2>Practice is not available yet</h2>
        <p>The UI will not create placeholder TOEIC part progress without API data.</p>
      </section>
    } @else {
      <section class="part-overview" data-testid="PartOverview" aria-label="TOEIC part overview">
        @for (part of parts(); track part.toeicPart) {
          <article class="panel part-row" [attr.data-testid]="'toeicPart-' + part.toeicPart">
            <div>
              <span class="part-badge">Part {{ part.toeicPart }}</span>
              <h2>{{ part.name }}</h2>
              <p class="assignment-context">{{ part.skillType }} · {{ part.currentUnitTitle }}</p>
            </div>

            <div class="metric compact-metric">
              <strong>{{ part.progressPercent }}%</strong>
              <span>Progress</span>
            </div>

            <div class="part-meta-list">
              @for (testName of part.availableTests; track testName) {
                <span>{{ testName }}</span>
              }
              @for (tag of part.weaknessTags; track tag) {
                <span>{{ tag }}</span>
              }
            </div>

            @if (part.isLocked) {
              <p class="locked-reason" data-testid="lockedReason">{{ part.lockedReason }}</p>
            } @else {
              <a class="primary-action action-link" [routerLink]="part.nextAction.route">{{ part.nextAction.label }}</a>
            }
          </article>
        }
      </section>
    }
  `,
})
export class PracticePageComponent {
  private readonly api = inject(ApiClientService);

  protected readonly state = signal<PartOverviewState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly parts = signal<ToeicPartOverviewItem[]>([]);

  constructor() {
    this.api.getToeicPartOverview().subscribe({
      next: (response) => {
        this.parts.set(response.parts);
        this.state.set('ready');
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'The TOEIC part overview API is unavailable.');
        this.state.set('error');
      },
    });
  }
}
