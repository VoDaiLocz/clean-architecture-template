import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';

export type LearnerHome = {
  nextAction: string;
  currentUnit: string;
  blocker: string | null;
  progressPercent: number;
};

export type LearnerNextAction = {
  code: 'StartPlacement' | 'ResumePlacement' | 'GoToday' | string;
  apiRoute: string;
  reason: string;
};

export type OnboardingRequest = {
  learnerId: string;
  displayName: string;
  email: string;
  targetScore: number;
  currentEstimatedScore: number;
  dailyStudyMinutes: number;
  timeZoneId: string;
  studyGoal: string;
};

export type OnboardingResponse = {
  learnerId: string;
  targetScore: number;
  currentEstimatedScore: number;
  dailyStudyMinutes: number;
  timeZoneId: string;
  nextAction: LearnerNextAction;
};

export type StartPlacementRequest = {
  learnerId: string;
};

export type StartPlacementResponse = {
  sessionId: string;
  learnerId: string;
  status: string;
  nextAction: LearnerNextAction;
};

export type PlacementQuestion = {
  id: string;
  part: number;
  prompt: string;
  choices: string[];
};

export type PlacementSessionResponse = {
  sessionId: string;
  status: string;
  answeredCount: number;
  totalCount: number;
  questions: PlacementQuestion[];
};

export type PlacementAnswer = {
  questionId: string;
  selectedChoice: string | null;
  skipped: boolean;
};

export type PlacementSubmitRequest = {
  answers: PlacementAnswer[];
};

export type PlacementWeakness = {
  part: number;
  skill: string;
};

export type PlacementResultResponse = {
  sessionId: string;
  estimateBand: string;
  label: string;
  weaknesses: PlacementWeakness[];
  nextAction: LearnerNextAction;
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

  onboardLearner(request: OnboardingRequest): Observable<OnboardingResponse> {
    return this.http.post<OnboardingResponse>(`${this.baseUrl}/learner/onboarding`, request);
  }

  startPlacement(request: StartPlacementRequest): Observable<StartPlacementResponse> {
    return this.http.post<StartPlacementResponse>(`${this.baseUrl}/learner/placement/start`, request);
  }

  getPlacementSession(sessionId: string): Observable<PlacementSessionResponse> {
    return this.http.get<PlacementSessionResponse>(`${this.baseUrl}/learner/placement/${sessionId}`);
  }

  submitPlacement(sessionId: string, request: PlacementSubmitRequest): Observable<PlacementResultResponse> {
    return this.http.post<PlacementResultResponse>(`${this.baseUrl}/learner/placement/${sessionId}/submit`, request);
  }
}
