import { Component, input } from '@angular/core';

@Component({
  selector: 'toeic-page-header',
  standalone: true,
  template: `
    <section class="page-header">
      <p class="eyebrow">{{ eyebrow() }}</p>
      <h1>{{ title() }}</h1>
      <p>{{ description() }}</p>
    </section>
  `,
})
export class PageHeaderComponent {
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly description = input.required<string>();
}
