import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ReviewPageComponent } from './review-page.component';

describe('ReviewPageComponent', () => {
  it('renders review queue and repair detail from API state', async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(ReviewPageComponent);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/learner/review-queue').flush({
      groups: [
        {
          blockerId: 'blocker',
          unitTitle: 'Unit',
          part: 5,
          skill: 'Grammar',
          blockerReason: 'Repair required.',
          items: [
            {
              reviewItemId: 'review',
              questionContext: 'Question context',
              learnerAnswer: 'A',
              correctAnswer: 'B',
              explanation: 'Because of the rule.',
              evidence: 'Evidence text',
              audioUrl: null,
              passage: null,
            },
          ],
        },
      ],
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="ReviewQueue"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="MistakeRepair"]')).not.toBeNull();
    http.verify();
  });
});
