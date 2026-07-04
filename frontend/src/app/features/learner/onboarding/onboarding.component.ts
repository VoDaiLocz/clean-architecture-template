import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './onboarding.component.html',
})
export class OnboardingComponent {
  private apiService = inject(LearnerApiService);
  private router = inject(Router);

  currentStep = 1;
  targetScore: number = 700;
  dailyMinutes: number = 30;

  scores = [500, 700, 900];
  minutesList = [15, 30, 60];

  nextStep() {
    this.currentStep++;
  }

  prevStep() {
    this.currentStep--;
  }

  finish() {
    this.apiService.onboardLearner({
      learnerId: 'demo-learner',
      targetScore: this.targetScore,
      currentEstimate: 0,
      dailyMinutes: this.dailyMinutes,
      timezone: 'UTC',
      studyGoal: 'general'
    }).subscribe({
      next: (response) => {
        if (response && response.nextAction === 'StartPlacement') {
          this.router.navigate(['/learner/placement']);
        } else {
          this.router.navigate(['/learner/today']);
        }
      },
      error: (err) => {
        console.error('Onboarding failed', err);
      }
    });
  }
}
