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

export type LessonConcept = {
  heading: string;
  body: string;
};

export type GuidedExample = {
  prompt: string;
  question: string;
  answer: string;
  rationale: string;
};

export type LessonMedia = {
  audioUrl: string | null;
  imageUrl: string | null;
};

export type LessonResponse = {
  lessonId: string;
  activitySessionId: string;
  title: string;
  objective: string;
  part: number;
  skill: string;
  concept: LessonConcept;
  example: GuidedExample;
  trap: string | null;
  passage: string | null;
  media: LessonMedia | null;
  nextAction: LearnerNextAction;
};

export type CompleteLessonResponse = {
  activitySessionId: string;
  nextAction: LearnerNextAction;
};

export type PracticeQuestion = {
  id: string;
  prompt: string;
  choices: string[];
  audioUrl: string | null;
  passage: string | null;
};

export type PracticeActivityResponse = {
  activityId: string;
  activitySessionId: string | null;
  mode: 'Drill' | 'MiniTest' | string;
  title: string;
  part: number;
  skill: string;
  isLocked: boolean;
  lockReason: string | null;
  allowSkip: boolean;
  questions: PracticeQuestion[];
};

export type SubmitAttemptAnswer = {
  questionId: string;
  selectedChoice: string | null;
  skipped: boolean;
};

export type SubmitAttemptRequest = {
  answers: SubmitAttemptAnswer[];
};

export type SubmitAttemptResponse = {
  activitySessionId: string;
  resultLabel: string;
  answeredCount: number;
  totalCount: number;
  scorePercent: number;
  reviewCreated: boolean;
  nextAction: LearnerNextAction;
};

export type ReviewQueueItem = {
  reviewItemId: string;
  questionContext: string;
  learnerAnswer: string;
  correctAnswer: string;
  explanation: string;
  evidence: string;
  audioUrl: string | null;
  passage: string | null;
};

export type ReviewQueueGroup = {
  blockerId: string;
  unitTitle: string;
  part: number;
  skill: string;
  blockerReason: string;
  items: ReviewQueueItem[];
};

export type ReviewQueueResponse = {
  groups: ReviewQueueGroup[];
};

export type RepairReviewResponse = {
  reviewItemId: string;
  status: string;
  blockerResolved: boolean;
  learnerMessage: string;
  nextAction: LearnerNextAction;
};

export type ToeicPartNextAction = {
  label: string;
  route: string;
};

export type ToeicPartOverviewItem = {
  toeicPart: number;
  name: string;
  skillType: 'Listening' | 'Reading' | string;
  progressPercent: number;
  currentUnitTitle: string;
  isLocked: boolean;
  lockedReason: string | null;
  nextAction: ToeicPartNextAction;
  availableTests: string[];
  weaknessTags: string[];
};

export type ToeicPartOverviewResponse = {
  parts: ToeicPartOverviewItem[];
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

  getLesson(lessonId: string): Observable<LessonResponse> {
    return this.http.get<LessonResponse>(`${this.baseUrl}/learner/lessons/${lessonId}`);
  }

  completeLesson(activitySessionId: string): Observable<CompleteLessonResponse> {
    return this.http.post<CompleteLessonResponse>(
      `${this.baseUrl}/learner/lessons/${activitySessionId}/complete`,
      {},
    );
  }

  getPracticeActivity(activityId: string): Observable<PracticeActivityResponse> {
    return this.http.get<PracticeActivityResponse>(`${this.baseUrl}/learner/practice/${activityId}`);
  }

  submitPracticeAttempt(activitySessionId: string, request: SubmitAttemptRequest): Observable<SubmitAttemptResponse> {
    return this.http.post<SubmitAttemptResponse>(
      `${this.baseUrl}/learner/practice/${activitySessionId}/attempts`,
      request,
    );
  }

  getReviewQueue(): Observable<ReviewQueueResponse> {
    return this.http.get<ReviewQueueResponse>(`${this.baseUrl}/learner/review-queue`);
  }

  repairReviewItem(reviewItemId: string): Observable<RepairReviewResponse> {
    return this.http.post<RepairReviewResponse>(`${this.baseUrl}/learner/review/${reviewItemId}/repair`, {});
  }

  getToeicPartOverview(): Observable<ToeicPartOverviewResponse> {
    return this.http.get<ToeicPartOverviewResponse>(`${this.baseUrl}/learner/toeic-parts`);
  }
}
