import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LearnerApiService } from '../../../core/api/learner-api.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-progress',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './progress.component.html'
})
export class ProgressComponent implements OnInit {
  public learnerApi = inject(LearnerApiService);
  public router = inject(Router);

  loading = true;
  error: string | null = null;
  data: any = null;

  ngOnInit(): void {
    // Fetch getProgress('demo-learner') on init
    this.learnerApi.getProgress('demo-learner').subscribe({
      next: (res) => {
        this.data = {
          targetScore: 850,
          estimatedScore: 700,
          weaknesses: ['Reading Part 7', 'Listening Part 3'],
          strengths: ['Grammar', 'Vocabulary'],
          unitsCompleted: 45,
          totalUnits: 100,
          accuracy: 82,
          streak: 12,
          testHistory: [
            { date: 'Oct 1', score: 650 },
            { date: 'Oct 15', score: 680 },
            { date: 'Nov 1', score: 700 }
          ],
          ...res
        };
        this.loading = false;
      },
      error: (err) => {
        console.error('Progress API error:', err);
        // Even on error, populate dummy data for demo purposes
        this.data = {
          targetScore: 850,
          estimatedScore: 700,
          weaknesses: ['Reading Part 7', 'Listening Part 3'],
          strengths: ['Grammar', 'Vocabulary'],
          unitsCompleted: 45,
          totalUnits: 100,
          accuracy: 82,
          streak: 12,
          testHistory: [
            { date: 'Oct 1', score: 650 },
            { date: 'Oct 15', score: 680 },
            { date: 'Nov 1', score: 700 }
          ]
        };
        this.loading = false;
      }
    });
  }
}
