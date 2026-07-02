import { Component } from '@angular/core';

import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-learn-page',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Learn"
      title="Study before practice"
      description="Lessons and guided examples render approved learning content only. Source documents are converted before learners study."
    />
    <section class="panel reading-panel">
      <h2>Lesson surface ready</h2>
      <p>
        This Angular route is prepared for concept lessons, guided examples, traps, and the next backend action.
      </p>
    </section>
  `,
})
export class LearnPageComponent {}
