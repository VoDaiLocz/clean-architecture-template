import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { LearnerApiService } from '../../../core/api/learner-api.service';

@Component({
  selector: 'app-placement-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './placement-result.component.html'
})
export class PlacementResultComponent {
  private apiService = inject(LearnerApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  score = this.route.snapshot.queryParams['score'] || 450;
  isGenerating = false;

  generatePath() {
    this.isGenerating = true;
    this.apiService.generatePath({ learnerId: 'demo-learner' }).subscribe({
      next: () => {
        this.isGenerating = false;
        this.router.navigate(['/learner/today']);
      },
      error: (err) => {
        console.error('Failed to generate path', err);
        this.isGenerating = false;
        this.router.navigate(['/learner/today']);
      }
    });
  }
}
