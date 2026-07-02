import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { LessonPageComponent } from './lesson-page.component';

describe('LessonPageComponent', () => {
  it('renders lesson sections from the published lesson API', async () => {
    await TestBed.configureTestingModule({
      imports: [LessonPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(LessonPageComponent);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/learner/lessons/active').flush({
      lessonId: 'active',
      activitySessionId: 'activitySession-unit',
      title: 'Unit lesson',
      objective: 'Study from published content.',
      part: 1,
      skill: 'Photo evidence',
      concept: { heading: 'Observe first', body: 'Look at the full scene before matching details.' },
      example: {
        prompt: 'A person is holding a folder.',
        question: 'What is happening?',
        answer: 'A folder is being held.',
        rationale: 'The visible action supports the answer.',
      },
      trap: null,
      passage: null,
      media: null,
      nextAction: { code: 'CompleteLesson', apiRoute: '/api/learner/lessons/activitySession-unit/complete', reason: 'Finish lesson.' },
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="LessonHeader"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="LessonContentBody"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="GuidedExample"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="LessonNextActionFooter"]')).not.toBeNull();
    http.verify();
  });
});
