import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';

@Component({
  selector: 'app-lesson',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lesson.component.html',
})
export class LessonComponent implements OnInit {
  private learnerApi = inject(LearnerApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  lessonData: any = null;
  unitId: string = 'demo';
  showAnswer: boolean = false;
  isLoading: boolean = true;
  error: string | null = null;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.unitId = params.get('unitId') || 'demo';
      this.loadLesson();
    });
  }

  loadLesson() {
    this.isLoading = true;
    this.error = null;
    this.showAnswer = false;
    this.learnerApi.getLesson(this.unitId).subscribe({
      next: (data) => {
        // Fallback demo data if data is empty or API returns not-found
        if (!data || Object.keys(data).length === 0) {
          this.lessonData = this.getFallbackData();
        } else {
          this.lessonData = data;
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error fetching lesson, using fallback data.', err);
        this.lessonData = this.getFallbackData();
        this.isLoading = false;
      }
    });
  }

  getFallbackData() {
    return {
      title: 'Incomplete Sentences: Vocabulary',
      learningObjective: 'Choose the correct word to complete the sentence focusing on context clues.',
      conceptExplanation: 'Many TOEIC questions require you to understand the exact meaning of a word in a specific business context. Pay attention to the words surrounding the blank to determine whether you need a noun, verb, adjective, or adverb, and which specific word fits best.',
      commonTrap: 'Trap: Similar sounding words or words with related meanings can be confusing. For example, "affect" (verb) vs "effect" (noun).',
      guidedExample: {
        prompt: 'The new marketing director decided to ________ the current strategy to reach a wider audience.',
        choices: ['A. revision', 'B. revise', 'C. revised', 'D. revising'],
        correctAnswer: 'B. revise',
        explanation: 'The sentence requires a base verb after "decided to". "Revision" is a noun, "revised" is past tense/participle, and "revising" is a present participle. Therefore, "revise" is the correct choice.'
      }
    };
  }

  onShowAnswer() {
    this.showAnswer = true;
  }

  onCompleteLesson() {
    this.learnerApi.completeLesson(this.unitId).subscribe({
      next: () => {
        this.router.navigate(['/learner/today']);
      },
      error: (err) => {
        console.error('Failed to complete lesson', err);
        // Navigate anyway for demo purposes
        this.router.navigate(['/learner/today']);
      }
    });
  }
}
