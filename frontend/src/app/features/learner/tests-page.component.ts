import { Component } from '@angular/core';

import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-tests-page',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Tests"
      title="Practice test cockpit"
      description="Exam sessions will use backend-owned timing, frozen question assignment, and final submit state."
    />
    <section class="panel">
      <h2>Exam shell baseline</h2>
      <p>Timer, question palette, unanswered count, and submit confirmation belong in this route family.</p>
    </section>
  `,
})
export class TestsPageComponent {}
