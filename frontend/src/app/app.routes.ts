import { Routes } from '@angular/router';
import { AppShellComponent } from './core/layout/app-shell/app-shell.component';

export const routes: Routes = [
  { path: '', redirectTo: 'learner/today', pathMatch: 'full' },
  { 
    path: 'learner', 
    component: AppShellComponent,
    children: [
      // empty for now, will add features later
    ]
  }
];
