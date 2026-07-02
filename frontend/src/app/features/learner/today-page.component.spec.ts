import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { TodayPageComponent } from './today-page.component';

describe('TodayPageComponent', () => {
  it('declares production Today screen sections', async () => {
    await TestBed.configureTestingModule({
      imports: [TodayPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(TodayPageComponent);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/learner/home').flush({
      learnerId: 'unit-learner',
      currentPart: 0,
      currentUnitId: 'onboarding',
      currentUnitTitle: 'Complete onboarding',
      nextActivity: {
        activityId: 'learner-onboarding',
        activityType: 'Onboarding',
        title: 'Complete TOEIC profile',
        description: 'Set profile before placement.',
      },
      reviewCount: 0,
      lockedNextUnit: null,
      primaryAssignment: {
        ctaLabel: 'Start onboarding',
        route: '/onboarding',
        reason: 'Profile is required first.',
      },
      pathProgress: null,
      dailyTarget: null,
      weakestAreas: [],
      activeSession: null,
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="TodayScreen"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="primaryAssignment"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="blockers"]')).not.toBeNull();
    http.verify();
  });
});
