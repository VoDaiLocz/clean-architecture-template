import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';

export type LearnerHome = {
  nextAction: string;
  currentUnit: string;
  blocker: string | null;
  progressPercent: number;
};

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getLearnerHome(): Observable<LearnerHome> {
    return this.http.get<LearnerHome>(`${this.baseUrl}/learner/home`).pipe(
      catchError(() =>
        of({
          nextAction: 'Start placement',
          currentUnit: 'Placement required',
          blocker: null,
          progressPercent: 0,
        }),
      ),
    );
  }
}
