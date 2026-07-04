import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'learner/today', pathMatch: 'full' },
  // Lazy loaded learner routes will be added here
];
