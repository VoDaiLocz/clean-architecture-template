import { Routes } from '@angular/router';
import { AppShellComponent } from './core/layout/app-shell/app-shell.component';

export const routes: Routes = [
  { path: '', redirectTo: 'learner/today', pathMatch: 'full' },
  { path: 'learner/onboarding', loadComponent: () => import('./features/learner/onboarding/onboarding.component').then(m => m.OnboardingComponent) },
  { path: 'learner/placement', loadComponent: () => import('./features/learner/placement/placement.component').then(m => m.PlacementComponent) },
  { 
    path: 'learner', 
    component: AppShellComponent,
    children: [
      // empty for now, will add features later
    ]
  }
];
