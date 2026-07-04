import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';

@Component({
  selector: 'app-mistake-repair',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './mistake-repair.component.html'
})
export class MistakeRepairComponent implements OnInit {
  private api = inject(LearnerApiService);
  private router = inject(Router);

  queue: any[] = [];
  currentIndex = 0;
  isRepairMode = false;
  selectedOption: string | null = null;
  isLoading = true;

  ngOnInit() {
    this.api.getReviewQueue('demo-learner').subscribe({
      next: (res: any) => {
        // Assume res is an array or contains an items array
        this.queue = Array.isArray(res) ? res : (res.items || res.queue || []);
        // Fallback mock data if API returns empty for demo purposes
        if (this.queue.length === 0) {
          this.queue = this.getMockData();
        }
        this.isLoading = false;
      },
      error: () => {
        // Mock fallback if API fails
        this.queue = this.getMockData();
        this.isLoading = false;
      }
    });
  }

  private getMockData() {
    return [
      {
        repairId: 'rep-1',
        skillTag: 'Grammar: Verb Tenses',
        questionPrompt: 'The manager ______ the report by tomorrow morning.',
        userWrongAnswer: 'has finished',
        correctAnswer: 'will have finished',
        explanation: 'Because of the time marker "by tomorrow morning", we need the future perfect tense to describe an action that will be completed before a specific time in the future.',
        options: ['finishes', 'has finished', 'will have finished', 'is finishing']
      },
      {
        repairId: 'rep-2',
        partTag: 'Part 5: Incomplete Sentences',
        questionPrompt: 'Please ensure that all ______ are submitted by Friday.',
        userWrongAnswer: 'apply',
        correctAnswer: 'applications',
        explanation: 'The word "all" must be followed by a plural noun here. "Apply" is a verb, while "applications" is the correct plural noun form.',
        options: ['apply', 'applying', 'applications', 'applied']
      }
    ];
  }

  get currentItem() {
    return this.queue[this.currentIndex];
  }

  get isQueueEmpty() {
    return this.queue.length === 0;
  }

  get isCompleted() {
    return this.currentIndex >= this.queue.length && this.queue.length > 0;
  }

  startRepair() {
    this.isRepairMode = true;
    this.selectedOption = null;
  }

  selectOption(opt: string) {
    this.selectedOption = opt;
  }

  submitRepair() {
    if (!this.selectedOption) return;

    const item = this.currentItem;
    this.api.submitRepair(item.repairId, this.selectedOption).subscribe({
      next: () => this.advanceQueue(),
      error: () => this.advanceQueue() // Advance anyway for demo
    });
  }

  private advanceQueue() {
    this.currentIndex++;
    this.isRepairMode = false;
    this.selectedOption = null;
  }

  goHome() {
    this.router.navigate(['/learner/today']);
  }
}
