import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';

@Component({
  selector: 'app-practice',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './practice.component.html'
})
export class PracticeComponent implements OnInit {
  private api = inject(LearnerApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  sessionId = 'demo';
  sessionData: any = null;
  currentIndex = 0;
  answers: Record<string, string> = {};
  isLoading = true;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.sessionId = params.get('sessionId') || 'demo';
      this.loadSession();
    });
  }

  loadSession() {
    this.isLoading = true;
    this.api.getPracticeSession(this.sessionId).subscribe({
      next: (data) => {
        // Fallback demo data if the API returns empty/errors
        this.sessionData = data || this.getDemoData();
        this.isLoading = false;
      },
      error: () => {
        this.sessionData = this.getDemoData();
        this.isLoading = false;
      }
    });
  }

  getDemoData() {
    return {
      questions: [
        {
          id: 'q1',
          prompt: 'What is the synonym for "abundant"?',
          choices: [
            { id: 'A', text: 'Scarce' },
            { id: 'B', text: 'Plentiful' },
            { id: 'C', text: 'Brief' },
            { id: 'D', text: 'Heavy' }
          ]
        },
        {
          id: 'q2',
          passage: 'The company announced a significant increase in profits for the third quarter, driven largely by strong sales in the European market.',
          prompt: 'What was the main driver of the company\'s profit increase?',
          choices: [
            { id: 'A', text: 'A new product launch' },
            { id: 'B', text: 'Strong sales in Europe' },
            { id: 'C', text: 'Cost-cutting measures' },
            { id: 'D', text: 'A merger with a rival' }
          ]
        },
        {
          id: 'q3',
          audioUrl: 'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3',
          prompt: 'Listen to the audio. What is the speaker\'s main point?',
          choices: [
            { id: 'A', text: 'The weather is improving' },
            { id: 'B', text: 'The project is delayed' },
            { id: 'C', text: 'The meeting is canceled' },
            { id: 'D', text: 'The budget is approved' }
          ]
        }
      ]
    };
  }

  get currentQuestion() {
    return this.sessionData?.questions?.[this.currentIndex];
  }

  get isLastQuestion() {
    return this.currentIndex === (this.sessionData?.questions?.length || 1) - 1;
  }

  selectChoice(choiceId: string) {
    if (this.currentQuestion) {
      this.answers[this.currentQuestion.id] = choiceId;
    }
  }

  nextOrSubmit() {
    if (this.isLastQuestion) {
      this.api.submitPracticeSession(this.sessionId, this.answers).subscribe({
        next: () => this.router.navigate(['/learner/practice-result']),
        error: () => this.router.navigate(['/learner/practice-result'])
      });
    } else {
      this.currentIndex++;
    }
  }
}
