import { Component } from '@angular/core';

import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-source-inventory-page',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Admin"
      title="Source inventory"
      description="Operators inspect normalized source state, blocked rows, and extraction readiness here."
    />
    <section class="panel">
      <h2>Operational table baseline</h2>
      <p>Search, filters, status badges, pagination, and audit-aware actions are required before production use.</p>
    </section>
  `,
})
export class SourceInventoryPageComponent {}
