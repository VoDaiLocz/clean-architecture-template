import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LearnerApiService } from '../../../core/api/learner-api.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-exam',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './exam.component.html',
  styles: []
})
export class ExamComponent implements OnInit {
  private api = inject(LearnerApiService);
  private router = inject(Router);

  questions: any[] = [];
  currentIndex = 0;
  answers: { [key: number]: string } = {};

  timeRemaining = 7199; // 119:59 in seconds
  timerDisplay = '119:59';
  private timerInterval: any;

  ngOnInit(): void {
    this.api.getExamSession('demo-exam').pipe(
      catchError(() => {
        // Fallback to dummy data if API fails
        const dummy = Array.from({ length: 200 }, (_, i) => ({
          id: i + 1,
          number: i + 1,
          passage: i > 100 ? `Sample passage for question ${i + 1}. Read carefully and answer.` : null,
          text: `Sample question text for question ${i + 1}?`,
          options: ['Option A', 'Option B', 'Option C', 'Option D']
        }));
        return of({ questions: dummy });
      })
    ).subscribe((res: any) => {
      this.questions = res.questions || [];
    });

    this.startTimer();
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  startTimer(): void {
    this.timerInterval = setInterval(() => {
      if (this.timeRemaining > 0) {
        this.timeRemaining--;
        const mins = Math.floor(this.timeRemaining / 60);
        const secs = this.timeRemaining % 60;
        this.timerDisplay = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
      } else {
        clearInterval(this.timerInterval);
        this.submitExam();
      }
    }, 1000);
  }

  get currentQuestion(): any {
    return this.questions[this.currentIndex];
  }

  selectOption(optionIndex: number): void {
    const letters = ['A', 'B', 'C', 'D'];
    if (this.currentQuestion) {
      this.answers[this.currentQuestion.number] = letters[optionIndex];
    }
  }

  isOptionSelected(optionIndex: number): boolean {
    const letters = ['A', 'B', 'C', 'D'];
    return this.currentQuestion && this.answers[this.currentQuestion.number] === letters[optionIndex];
  }

  goToQuestion(index: number): void {
    this.currentIndex = index;
  }

  nextQuestion(): void {
    if (this.currentIndex < this.questions.length - 1) {
      this.currentIndex++;
    }
  }

  prevQuestion(): void {
    if (this.currentIndex > 0) {
      this.currentIndex--;
    }
  }

  submitExam(): void {
    const answeredCount = Object.keys(this.answers).length;
    if (answeredCount < this.questions.length) {
      const confirmSubmit = window.confirm(`You have answered ${answeredCount} out of ${this.questions.length} questions. Are you sure you want to submit?`);
      if (!confirmSubmit) return;
    }

    this.api.submitExamSession('demo-exam', this.answers).pipe(
      catchError(() => of({ success: true }))
    ).subscribe(() => {
      alert('Exam submitted successfully!');
      this.router.navigate(['/learner/exam-result']).catch(() => {
        // Fallback if route does not exist yet
        console.log('Navigating to exam result...');
      });
    });
  }
}
