import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import {
  ApiClientService,
  LearnerNextAction,
  OnboardingRequest,
  OnboardingResponse,
} from '../../core/api/api-client.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'toeic-onboarding-page',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <toeic-page-header
      eyebrow="Onboarding"
      title="Set up your TOEIC path"
      description="Your profile is sent to the learner API. The backend returns the next required action."
    />

    <section class="journey-layout">
      <form class="panel form-panel" [formGroup]="form" (ngSubmit)="submitProfile()" novalidate>
        <label>
          <span>Target score</span>
          <input type="number" min="10" max="990" step="5" formControlName="targetScore" />
          @if (showError('targetScore')) {
            <small>Enter a TOEIC target score from 10 to 990.</small>
          }
        </label>

        <label>
          <span>Current estimate</span>
          <input type="number" min="10" max="990" step="5" formControlName="currentEstimatedScore" />
          @if (showError('currentEstimatedScore')) {
            <small>Enter your current estimated score from 10 to 990.</small>
          }
        </label>

        <label>
          <span>Daily study minutes</span>
          <input type="number" min="15" max="240" step="5" formControlName="dailyStudyMinutes" />
          @if (showError('dailyStudyMinutes')) {
            <small>Choose a realistic daily target from 15 to 240 minutes.</small>
          }
        </label>

        <label>
          <span>Timezone</span>
          <select formControlName="timeZoneId">
            <option value="Asia/Ho_Chi_Minh">Asia/Ho_Chi_Minh</option>
            <option value="Asia/Bangkok">Asia/Bangkok</option>
            <option value="UTC">UTC</option>
          </select>
        </label>

        <label class="wide-field">
          <span>Study goal</span>
          <textarea rows="4" formControlName="studyGoal"></textarea>
          @if (showError('studyGoal')) {
            <small>Describe the outcome you want from this TOEIC path.</small>
          }
        </label>

        @if (apiError()) {
          <p class="form-error" role="alert">{{ apiError() }}</p>
        }

        <button class="primary-action" type="submit" [disabled]="isSaving()">
          {{ isSaving() ? 'Saving profile' : 'Save profile' }}
        </button>
      </form>

      <aside class="panel next-action-panel">
        <p class="eyebrow">Backend next action</p>
        @if (savedProfile(); as profile) {
          <h2 data-testid="next-action">{{ profile.nextAction.code }}</h2>
          <p>{{ profile.nextAction.reason }}</p>
          @if (canStartPlacement(profile.nextAction)) {
            <button class="secondary-action" type="button" [disabled]="isStartingPlacement()" (click)="startPlacement()">
              {{ isStartingPlacement() ? 'Starting placement' : 'Start placement' }}
            </button>
          } @else {
            <button class="secondary-action" type="button" (click)="goToday()">Go to Today</button>
          }
        } @else {
          <h2>Profile required</h2>
          <p>Save your TOEIC target and routine before the system opens placement or Today.</p>
        }
      </aside>
    </section>
  `,
})
export class OnboardingPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiClientService);
  private readonly router = inject(Router);

  protected readonly isSaving = signal(false);
  protected readonly isStartingPlacement = signal(false);
  protected readonly apiError = signal<string | null>(null);
  protected readonly savedProfile = signal<OnboardingResponse | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    targetScore: [850, [Validators.required, Validators.min(10), Validators.max(990)]],
    currentEstimatedScore: [600, [Validators.required, Validators.min(10), Validators.max(990)]],
    dailyStudyMinutes: [45, [Validators.required, Validators.min(15), Validators.max(240)]],
    timeZoneId: ['Asia/Ho_Chi_Minh', [Validators.required]],
    studyGoal: ['', [Validators.required, Validators.minLength(12)]],
  });

  protected showError(controlName: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  protected submitProfile(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    this.isSaving.set(true);
    this.apiError.set(null);
    this.api.onboardLearner(this.toRequest()).subscribe({
      next: (response) => {
        this.savedProfile.set(response);
        this.isSaving.set(false);
      },
      error: (error) => {
        this.apiError.set(error?.error?.message ?? 'The learner profile could not be saved. Try again later.');
        this.isSaving.set(false);
      },
    });
  }

  protected canStartPlacement(nextAction: LearnerNextAction): boolean {
    return nextAction.code === 'StartPlacement' || nextAction.code === 'ResumePlacement';
  }

  protected startPlacement(): void {
    const profile = this.savedProfile();
    if (!profile) {
      return;
    }

    this.isStartingPlacement.set(true);
    this.api.startPlacement({ learnerId: profile.learnerId }).subscribe({
      next: (response) => {
        this.isStartingPlacement.set(false);
        void this.router.navigate(['/placement', response.sessionId]);
      },
      error: (error) => {
        this.apiError.set(error?.error?.message ?? 'Placement could not be started. Try again later.');
        this.isStartingPlacement.set(false);
      },
    });
  }

  protected goToday(): void {
    void this.router.navigate(['/today']);
  }

  private toRequest(): OnboardingRequest {
    const value = this.form.getRawValue();
    return {
      learnerId: 'demo-learner',
      displayName: 'TOEIC Learner',
      email: 'learner@example.com',
      targetScore: value.targetScore,
      currentEstimatedScore: value.currentEstimatedScore,
      dailyStudyMinutes: value.dailyStudyMinutes,
      timeZoneId: value.timeZoneId,
      studyGoal: value.studyGoal,
    };
  }
}
