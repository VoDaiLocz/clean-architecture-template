import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';

@Component({
  selector: 'app-placement',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './placement.component.html',
  styles: []
})
export class PlacementComponent implements OnInit {
  private learnerApi = inject(LearnerApiService);
  private router = inject(Router);

  sessionId: string | null = null;
  questions: any[] = [];
  currentIndex = 0;
  answers: any[] = [];
  selectedOptionId: string | null = null;
  loading = true;
  submitting = false;

  ngOnInit() {
    this.learnerApi.startPlacement({ learnerId: 'demo-learner' }).subscribe({
      next: (res: any) => {
        this.sessionId = res.sessionId || (res.session && res.session.id);
        this.questions = res.questions || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  get currentQuestion() {
    return this.questions[this.currentIndex];
  }

  get progress() {
    if (!this.questions.length) return 0;
    return ((this.currentIndex + 1) / this.questions.length) * 100;
  }

  selectOption(optionId: string) {
    this.selectedOptionId = optionId;
  }

  nextQuestion() {
    if (!this.currentQuestion || !this.selectedOptionId) return;

    this.answers.push({
      questionId: this.currentQuestion.id,
      answer: this.selectedOptionId
    });

    this.selectedOptionId = null;

    if (this.currentIndex < this.questions.length - 1) {
      this.currentIndex++;
    } else {
      this.finishPlacement();
    }
  }

  skipQuestion() {
    if (!this.currentQuestion) return;
    
    this.answers.push({
      questionId: this.currentQuestion.id,
      answer: 'SKIPPED'
    });

    this.selectedOptionId = null;

    if (this.currentIndex < this.questions.length - 1) {
      this.currentIndex++;
    } else {
      this.finishPlacement();
    }
  }

  finishPlacement() {
    if (!this.sessionId) return;
    this.submitting = true;
    this.learnerApi.scorePlacement({
      learnerId: 'demo-learner',
      sessionId: this.sessionId,
      answers: this.answers
    }).subscribe({
      next: (res: any) => {
        this.submitting = false;
        this.router.navigate(['/learner/placement-result'], {
          state: { nextAction: res.nextAction, result: res }
        });
      },
      error: () => {
        this.submitting = false;
      }
    });
  }
}
