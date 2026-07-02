import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'today',
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/login-page.component').then((m) => m.LoginPageComponent),
  },
  {
    path: 'today',
    loadComponent: () => import('./features/learner/today-page.component').then((m) => m.TodayPageComponent),
  },
  {
    path: 'learn',
    loadComponent: () => import('./features/learner/learn-page.component').then((m) => m.LearnPageComponent),
  },
  {
    path: 'practice',
    loadComponent: () => import('./features/learner/practice-page.component').then((m) => m.PracticePageComponent),
  },
  {
    path: 'review',
    loadComponent: () => import('./features/learner/review-page.component').then((m) => m.ReviewPageComponent),
  },
  {
    path: 'tests',
    loadComponent: () => import('./features/learner/tests-page.component').then((m) => m.TestsPageComponent),
  },
  {
    path: 'progress',
    loadComponent: () => import('./features/learner/progress-page.component').then((m) => m.ProgressPageComponent),
  },
  {
    path: 'admin/source-inventory',
    loadComponent: () =>
      import('./features/admin/source-inventory-page.component').then((m) => m.SourceInventoryPageComponent),
  },
  {
    path: '**',
    redirectTo: 'today',
  },
];
