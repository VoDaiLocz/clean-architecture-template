import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LearnerApiService } from '../../../core/api/learner-api.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-today',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './today.component.html'
})
export class TodayComponent implements OnInit {
  private learnerApi = inject(LearnerApiService);
  private router = inject(Router);

  loading = true;
  error: string | null = null;
  data: any = null;

  ngOnInit(): void {
    this.learnerApi.getTodayPlan('demo-learner').subscribe({
      next: (res) => {
        this.data = res;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Failed to load today plan.';
        this.loading = false;
      }
    });
  }
}
