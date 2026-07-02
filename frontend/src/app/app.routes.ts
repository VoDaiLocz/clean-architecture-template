import { Routes } from '@angular/router';

import { ProtectedRouteGuard } from './core/auth/auth.guard';

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
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/today-page.component').then((m) => m.TodayPageComponent),
  },
  {
    path: 'learn',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/learn-page.component').then((m) => m.LearnPageComponent),
  },
  {
    path: 'practice',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/practice-page.component').then((m) => m.PracticePageComponent),
  },
  {
    path: 'review',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/review-page.component').then((m) => m.ReviewPageComponent),
  },
  {
    path: 'tests',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/tests-page.component').then((m) => m.TestsPageComponent),
  },
  {
    path: 'progress',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/progress-page.component').then((m) => m.ProgressPageComponent),
  },
  {
    path: 'admin/source-inventory',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () =>
      import('./features/admin/source-inventory-page.component').then((m) => m.SourceInventoryPageComponent),
  },
  {
    path: '**',
    redirectTo: 'today',
  },
];
