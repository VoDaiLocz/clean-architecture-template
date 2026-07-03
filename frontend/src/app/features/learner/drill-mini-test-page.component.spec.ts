import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { DrillMiniTestPageComponent } from './drill-mini-test-page.component';

describe('DrillMiniTestPageComponent', () => {
  it('renders answer flow from the practice activity API', async () => {
    await TestBed.configureTestingModule({
      imports: [DrillMiniTestPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(DrillMiniTestPageComponent);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/learner/practice/active').flush({
      activityId: 'active',
      activitySessionId: 'activitySession-unit',
      mode: 'Drill',
      title: 'Unit drill',
      part: 5,
      skill: 'Grammar accuracy',
      isLocked: false,
      lockReason: null,
      allowSkip: true,
      questions: [{ id: 'q1', prompt: 'Choose one option.', choices: ['A', 'B'], audioUrl: null, passage: null }],
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="DrillMiniTestScreen"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="question-progress"]')).not.toBeNull();
    http.verify();
  });
});
