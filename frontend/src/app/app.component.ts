import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

type NavItem = {
  label: string;
  path: string;
};

@Component({
  selector: 'toeic-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app-shell">
      <header class="topbar">
        <a class="brand" routerLink="/today" aria-label="TOEIC Ocean Classroom home">
          <span class="brand-mark">TO</span>
          <span>
            <strong>TOEIC Ocean</strong>
            <small>Classroom</small>
          </span>
        </a>

        <nav class="topnav" aria-label="Learner navigation">
          @for (item of learnerNav; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: item.path === '/today' }"
            >
              {{ item.label }}
            </a>
          }
        </nav>

        <a class="admin-link" routerLink="/admin/source-inventory">Admin</a>
      </header>

      <main class="route-shell">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AppComponent {
  protected readonly learnerNav: NavItem[] = [
    { label: 'Today', path: '/today' },
    { label: 'Learn', path: '/learn' },
    { label: 'Practice', path: '/practice' },
    { label: 'Review', path: '/review' },
    { label: 'Tests', path: '/tests' },
    { label: 'Progress', path: '/progress' },
  ];
}
