import { Component } from '@angular/core';

import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-progress-page',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Progress"
      title="Improvement, weaknesses, and next work"
      description="Progress charts must explain what changed and what the learner should do next."
    />
    <section class="grid two">
      <article class="panel">
        <p class="eyebrow">Diagnostic band</p>
        <h2>Pending placement</h2>
      </article>
      <article class="panel">
        <p class="eyebrow">Weakness tags</p>
        <h2>Awaiting attempts</h2>
      </article>
    </section>
  `,
})
export class ProgressPageComponent {}
