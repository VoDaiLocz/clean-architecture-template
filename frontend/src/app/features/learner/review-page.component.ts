import { Component } from '@angular/core';

import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-review-page',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Review"
      title="Repair mistakes before unlocking"
      description="Review blockers and repair results are backend-owned. The UI cannot clear a blocker locally."
    />
    <section class="panel">
      <h2>No active review blocker</h2>
      <p>When a wrong answer creates review work, the evidence, explanation, and repair action appear here.</p>
    </section>
  `,
})
export class ReviewPageComponent {}
