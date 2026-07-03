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
    path: 'onboarding',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/onboarding-page.component').then((m) => m.OnboardingPageComponent),
  },
  {
    path: 'placement',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/placement-page.component').then((m) => m.PlacementPageComponent),
  },
  {
    path: 'placement/:sessionId',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/placement-page.component').then((m) => m.PlacementPageComponent),
  },
  {
    path: 'learn',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/learn-page.component').then((m) => m.LearnPageComponent),
  },
  {
    path: 'learn/:lessonId',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/lesson-page.component').then((m) => m.LessonPageComponent),
  },
  {
    path: 'practice',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () => import('./features/learner/practice-page.component').then((m) => m.PracticePageComponent),
  },
  {
    path: 'practice/:activityId',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () =>
      import('./features/learner/drill-mini-test-page.component').then((m) => m.DrillMiniTestPageComponent),
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
    path: 'tests/:activityId',
    canMatch: [ProtectedRouteGuard],
    loadComponent: () =>
      import('./features/learner/drill-mini-test-page.component').then((m) => m.DrillMiniTestPageComponent),
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
