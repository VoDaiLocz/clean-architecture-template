import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type LearnerActivitySummary = {
  activityId: string;
  activityType: string;
  title: string;
  description: string;
};

export type LockedUnit = {
  unitId: string;
  title: string;
  reasonCodes: string[];
  learnerMessage: string;
};

export type TodayPrimaryAssignment = {
  ctaLabel: string;
  route: string;
  reason: string;
};

export type TodayPathProgress = {
  completedUnits: number;
  totalUnits: number;
  percent: number;
};

export type TodayDailyTarget = {
  completedMinutes: number;
  targetMinutes: number;
};

export type TodayWeakestArea = {
  part: number;
  skill: string;
};

export type TodayActiveSession = {
  label: string;
  route: string;
};

export type LearnerHome = {
  learnerId: string;
  currentPart: number;
  currentUnitId: string;
  currentUnitTitle: string;
  nextActivity: LearnerActivitySummary;
  reviewCount: number;
  lockedNextUnit: LockedUnit | null;
  primaryAssignment?: TodayPrimaryAssignment | null;
  pathProgress?: TodayPathProgress | null;
  dailyTarget?: TodayDailyTarget | null;
  weakestAreas?: TodayWeakestArea[];
  activeSession?: TodayActiveSession | null;
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
    return this.http.get<LearnerHome>(`${this.baseUrl}/learner/home`);
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
