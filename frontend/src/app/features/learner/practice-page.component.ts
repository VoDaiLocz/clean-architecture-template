import { Component } from '@angular/core';

import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-practice-page',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Practice"
      title="Seven TOEIC parts"
      description="Part navigation is structured around backend progress, lock reasons, and test availability."
    />
    <section class="part-grid" aria-label="TOEIC parts">
      @for (part of parts; track part.id) {
        <article class="panel part-card">
          <span class="part-badge">Part {{ part.id }}</span>
          <h2>{{ part.name }}</h2>
          <p>{{ part.skill }}</p>
          <span class="status">Awaiting backend progress</span>
        </article>
      }
    </section>
  `,
})
export class PracticePageComponent {
  protected readonly parts = [
    { id: 1, name: 'Photographs', skill: 'Listening' },
    { id: 2, name: 'Question Response', skill: 'Listening' },
    { id: 3, name: 'Conversations', skill: 'Listening' },
    { id: 4, name: 'Talks', skill: 'Listening' },
    { id: 5, name: 'Incomplete Sentences', skill: 'Reading' },
    { id: 6, name: 'Text Completion', skill: 'Reading' },
    { id: 7, name: 'Reading Comprehension', skill: 'Reading' },
  ];
}
