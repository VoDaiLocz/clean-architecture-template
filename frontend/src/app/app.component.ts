import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

type NavItem = {
  label: string;
  path: string;
  shortLabel?: string;
};

@Component({
  selector: 'toeic-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app-shell" data-testid="AppShell">
      <header class="topbar">
        <a class="brand" routerLink="/today" aria-label="TOEIC Ocean Classroom home">
          <span class="brand-mark">TO</span>
          <span>
            <strong>TOEIC Ocean</strong>
            <small>Classroom</small>
          </span>
        </a>

        <nav class="topnav" aria-label="Learner navigation" data-testid="LearnerNavigation">
          @for (item of learnerNav; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: item.path === '/today' }"
            >
              {{ item.label }}
            </a>
          }
          <a routerLink="/practice" routerLinkActive="active">7-Part Overview</a>
        </nav>

        <div class="shell-actions">
          <a class="admin-link" routerLink="/admin/source-inventory">Admin</a>
          <details class="user-menu" data-testid="user-menu">
            <summary>
              <span class="avatar" aria-hidden="true">L</span>
              <span>Profile</span>
            </summary>
            <div class="user-menu-panel">
              <a routerLink="/progress">Profile</a>
              <a routerLink="/progress">Settings</a>
              <button type="button">Logout</button>
            </div>
          </details>
        </div>
      </header>

      <section class="global-error-banner" data-testid="global-error-banner" role="status">
        <strong>API connection issue</strong>
        <span>Production error handling is ready; correlation id appears here when the backend returns one.</span>
      </section>

      <main class="route-shell" aria-live="polite">
        <div class="route-loading-skeleton" data-testid="route-loading-skeleton" aria-hidden="true">
          <span></span>
          <span></span>
          <span></span>
        </div>
        <router-outlet />
      </main>

      <nav class="mobile-nav" aria-label="Mobile learner navigation" data-testid="mobile-learner-navigation">
        @for (item of mobileNav; track item.path) {
          <a [routerLink]="item.path" routerLinkActive="active">
            {{ item.shortLabel ?? item.label }}
          </a>
        }
      </nav>
    </div>
  `,
})
export class AppComponent {
  protected readonly learnerNav: NavItem[] = [
    { label: 'Today', path: '/today', shortLabel: 'Today' },
    { label: 'Learn', path: '/learn', shortLabel: 'Learn' },
    { label: 'Practice', path: '/practice', shortLabel: 'Practice' },
    { label: 'Review', path: '/review', shortLabel: 'Review' },
    { label: 'Tests', path: '/tests', shortLabel: 'Tests' },
    { label: 'Progress', path: '/progress', shortLabel: 'Progress' },
  ];

  protected readonly mobileNav: NavItem[] = [
    { label: 'Today', path: '/today' },
    { label: 'Practice', path: '/practice' },
    { label: 'Review', path: '/review' },
    { label: 'Tests', path: '/tests' },
    { label: 'Progress', path: '/progress' },
  ];
}
