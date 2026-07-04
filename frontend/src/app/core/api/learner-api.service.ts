import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Optional Models (using 'any' as fallback as per instructions, but defined here for structure)
export interface OnboardingRequest {
  [key: string]: any;
}

export interface PlacementStartRequest {
  [key: string]: any;
}

export interface PlacementScoreRequest {
  [key: string]: any;
}

export interface PathGenerateRequest {
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class LearnerApiService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/learner';

  onboardLearner(request: OnboardingRequest | any): Observable<any> {
    return this.http.post(`${this.baseUrl}/onboarding`, request);
  }

  startPlacement(request: PlacementStartRequest | any): Observable<any> {
    return this.http.post(`${this.baseUrl}/placement/start`, request);
  }

  scorePlacement(request: PlacementScoreRequest | any): Observable<any> {
    return this.http.post(`${this.baseUrl}/placement/score`, request);
  }

  generatePath(request: PathGenerateRequest | any): Observable<any> {
    return this.http.post(`${this.baseUrl}/path/generate`, request);
  }

  getTodayPlan(learnerId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/${learnerId}/today`);
  }

  getLesson(unitId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/lessons/${unitId}`);
  }

  completeLesson(unitId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/lessons/${unitId}/complete`, {});
  }

  getPracticeSession(sessionId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/practice/${sessionId}`);
  }

  submitPracticeSession(sessionId: string, answers: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/practice/${sessionId}/submit`, answers);
  }

  getReviewQueue(learnerId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/${learnerId}/reviews`);
  }

  submitRepair(repairId: string, answer: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/reviews/${repairId}/submit`, { answer });
  }
}
