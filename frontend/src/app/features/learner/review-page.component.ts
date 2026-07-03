import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import {
  ApiClientService,
  RepairReviewResponse,
  ReviewQueueGroup,
  ReviewQueueItem,
} from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

type ReviewState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'toeic-review-page',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Review"
      title="Repair mistakes before unlocking"
      description="Review blockers and repair results are backend-owned. The UI cannot clear a blocker locally."
    />

    @if (state() === 'loading') {
      <section class="panel state-panel" aria-live="polite">
        <p class="eyebrow">Loading</p>
        <h2>Loading review queue</h2>
        <p>The backend decides which mistakes are still blockers.</p>
      </section>
    } @else if (state() === 'error') {
      <section class="panel state-panel" role="alert">
        <p class="eyebrow">Content unavailable</p>
        <h2>Review cannot load right now</h2>
        <p>{{ loadError() }}</p>
        <a class="secondary-action" routerLink="/today">Back to Today</a>
      </section>
    } @else if (groups().length === 0) {
      <section class="panel state-panel">
        <p class="eyebrow">Review clear</p>
        <h2>No active review blocker</h2>
        <p>When wrong answers create review work, repair evidence appears here.</p>
        <a class="primary-action action-link" routerLink="/today">Back to Today</a>
      </section>
    } @else {
      <section class="repair-layout">
        <aside class="panel review-queue" data-testid="ReviewQueue">
          <p class="eyebrow">Review queue</p>
          @for (group of groups(); track group.blockerId) {
            <button class="queue-item" type="button" (click)="selectGroup(group)">
              <strong>Part {{ group.part }} · {{ group.unitTitle }}</strong>
              <span>{{ group.skill }}</span>
              <span>{{ group.items.length }} item(s)</span>
            </button>
          }
        </aside>

        @if (selectedGroup(); as group) {
          @if (selectedItem(); as item) {
            <article class="panel mistake-repair" data-testid="MistakeRepair">
              <p class="eyebrow">Mistake repair</p>
              <h2>{{ group.unitTitle }}</h2>
              <p class="assignment-context">Part {{ group.part }} · {{ group.skill }}</p>
              <p id="blocker-reason" data-testid="blocker-reason">{{ group.blockerReason }}</p>

              @if (item.audioUrl) {
                <audio controls [src]="item.audioUrl"></audio>
              }

              @if (item.passage) {
                <article class="reading-panel passage-box">
                  <p>{{ item.passage }}</p>
                </article>
              }

              <div class="repair-grid">
                <section>
                  <p class="eyebrow">Original context</p>
                  <p>{{ item.questionContext }}</p>
                </section>
                <section>
                  <p class="eyebrow">Your answer</p>
                  <h3>{{ item.learnerAnswer }}</h3>
                </section>
                <section>
                  <p class="eyebrow">Correct answer</p>
                  <h3>{{ item.correctAnswer }}</h3>
                </section>
                <section>
                  <p class="eyebrow">Explanation</p>
                  <p>{{ item.explanation }}</p>
                </section>
              </div>

              <section class="evidence-box">
                <p class="eyebrow">Evidence</p>
                <p>{{ item.evidence }}</p>
              </section>

              @if (repairResult(); as result) {
                <section class="result-panel" data-testid="repair-result">
                  <h3>{{ result.learnerMessage }}</h3>
                  <p>{{ result.status }} · {{ result.nextAction.code }}</p>
                  <p>{{ result.nextAction.reason }}</p>
                </section>
              } @else {
                <button class="primary-action" type="button" (click)="submitRepair(item)">Submit repair</button>
              }
            </article>
          }
        }
      </section>
    }
  `,
})
export class ReviewPageComponent {
  private readonly api = inject(ApiClientService);

  protected readonly state = signal<ReviewState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly groups = signal<ReviewQueueGroup[]>([]);
  protected readonly selectedBlockerId = signal<string | null>(null);
  protected readonly repairResult = signal<RepairReviewResponse | null>(null);
  protected readonly selectedGroup = computed(() => {
    const selectedBlockerId = this.selectedBlockerId();
    return this.groups().find((group) => group.blockerId === selectedBlockerId) ?? this.groups()[0] ?? null;
  });
  protected readonly selectedItem = computed(() => this.selectedGroup()?.items[0] ?? null);

  constructor() {
    this.api.getReviewQueue().subscribe({
      next: (queue) => {
        this.groups.set(queue.groups);
        this.selectedBlockerId.set(queue.groups[0]?.blockerId ?? null);
        this.state.set('ready');
      },
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'The review queue is unavailable.');
        this.state.set('error');
      },
    });
  }

  protected selectGroup(group: ReviewQueueGroup): void {
    this.selectedBlockerId.set(group.blockerId);
    this.repairResult.set(null);
  }

  protected submitRepair(item: ReviewQueueItem): void {
    this.api.repairReviewItem(item.reviewItemId).subscribe({
      next: (result) => this.repairResult.set(result),
      error: (error) => {
        this.loadError.set(error?.error?.message ?? 'The repair attempt could not be submitted.');
        this.state.set('error');
      },
    });
  }
}
