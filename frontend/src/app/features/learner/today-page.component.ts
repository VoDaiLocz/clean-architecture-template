import { Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';

import { ApiClientService } from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-today-page',
  standalone: true,
  imports: [AsyncPipe, PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Today"
      title="Your TOEIC next action"
      description="The backend decides the next required step. The UI only displays the plan, blocker, and progress."
    />

    @if (home$ | async; as home) {
      <section class="primary-panel">
        <div>
          <p class="eyebrow">Primary assignment</p>
          <h2>{{ home.nextAction }}</h2>
          <p>{{ home.currentUnit }}</p>
        </div>
        <button class="primary-action" type="button">{{ home.nextAction }}</button>
      </section>

      <section class="grid two">
        <article class="panel">
          <p class="eyebrow">Blocker</p>
          <h3>{{ home.blocker ?? 'No active blocker' }}</h3>
          <p>When review blockers exist, they appear here before new lessons unlock.</p>
        </article>
        <article class="panel">
          <p class="eyebrow">Path progress</p>
          <h3>{{ home.progressPercent }}%</h3>
          <p>Progress comes from persisted learner state, not local UI math.</p>
        </article>
      </section>
    }
  `,
})
export class TodayPageComponent {
  private readonly api = inject(ApiClientService);
  protected readonly home$ = this.api.getLearnerHome();
}
