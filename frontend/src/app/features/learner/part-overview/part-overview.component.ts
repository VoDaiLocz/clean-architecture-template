import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';

export interface ToeicPart {
  part: number;
  name: string;
  skill: string;
  progress: number;
  isLocked: boolean;
  lockReason?: string;
  nextAction?: string;
}

@Component({
  selector: 'app-part-overview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './part-overview.component.html'
})
export class PartOverviewComponent implements OnInit {
  private api = inject(LearnerApiService);
  private router = inject(Router);

  parts: ToeicPart[] = [];
  isLoading = true;

  ngOnInit() {
    this.api.getPartOverview('demo-learner').pipe(
      catchError(() => {
        // Fallback dummy data if API fails
        return of([
          { part: 1, name: 'Photographs', skill: 'Listening', progress: 80, isLocked: false, nextAction: 'Review' },
          { part: 2, name: 'Question-Response', skill: 'Listening', progress: 20, isLocked: false, nextAction: 'Learn' },
          { part: 3, name: 'Conversations', skill: 'Listening', progress: 0, isLocked: true, lockReason: 'Complete Part 2 to unlock' },
          { part: 4, name: 'Talks', skill: 'Listening', progress: 0, isLocked: true, lockReason: 'Complete Part 3 to unlock' },
          { part: 5, name: 'Incomplete Sentences', skill: 'Reading', progress: 0, isLocked: true, lockReason: 'Complete Part 4 to unlock' },
          { part: 6, name: 'Text Completion', skill: 'Reading', progress: 0, isLocked: true, lockReason: 'Complete Part 5 to unlock' },
          { part: 7, name: 'Reading Comprehension', skill: 'Reading', progress: 0, isLocked: true, lockReason: 'Complete Part 6 to unlock' }
        ]);
      })
    ).subscribe(data => {
      this.parts = data;
      this.isLoading = false;
    });
  }

  handleAction(part: ToeicPart) {
    if (part.isLocked) return;
    console.log(`Action: ${part.nextAction} on Part ${part.part}`);
    // Example routing: this.router.navigate(['/learner/lesson', `part-${part.part}`]);
  }
}
