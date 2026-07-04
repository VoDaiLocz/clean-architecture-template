import { Routes } from '@angular/router';
import { AppShellComponent } from './core/layout/app-shell/app-shell.component';

export const routes: Routes = [
  { path: '', redirectTo: 'learner/today', pathMatch: 'full' },
  { path: 'learner/onboarding', loadComponent: () => import('./features/learner/onboarding/onboarding.component').then(m => m.OnboardingComponent) },
  { path: 'learner/placement', loadComponent: () => import('./features/learner/placement/placement.component').then(m => m.PlacementComponent) },
  { path: 'learner/placement-result', loadComponent: () => import('./features/learner/placement-result/placement-result.component').then(m => m.PlacementResultComponent) },
  { path: 'learner/today', loadComponent: () => import('./features/learner/today/today.component').then(m => m.TodayComponent) },
  { path: 'learner/lesson', redirectTo: 'learner/lesson/demo', pathMatch: 'full' },
  { path: 'learner/lesson/:unitId', loadComponent: () => import('./features/learner/lesson/lesson.component').then(m => m.LessonComponent) },
  { path: 'learner/practice', redirectTo: 'learner/practice/demo', pathMatch: 'full' },
  { path: 'learner/practice/:sessionId', loadComponent: () => import('./features/learner/practice/practice.component').then(m => m.PracticeComponent) },
  { 
    path: 'learner', 
    component: AppShellComponent,
    children: [
      // empty for now, will add features later
    ]
  }
];
