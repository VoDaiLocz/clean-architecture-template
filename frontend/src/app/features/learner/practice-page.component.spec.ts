import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PracticePageComponent } from './practice-page.component';

describe('PracticePageComponent', () => {
  it('renders TOEIC part overview from API data', async () => {
    await TestBed.configureTestingModule({
      imports: [PracticePageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(PracticePageComponent);
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/learner/toeic-parts').flush({
      parts: [
        {
          toeicPart: 1,
          name: 'Photographs',
          skillType: 'Listening',
          progressPercent: 20,
          currentUnitTitle: 'Photo foundations',
          isLocked: false,
          lockedReason: null,
          nextAction: { label: 'Continue Part 1', route: '/practice/part-1-next' },
          availableTests: ['Mini test'],
          weaknessTags: ['visual evidence'],
        },
      ],
    });
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="PartOverview"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="toeicPart-1"]')).not.toBeNull();
    http.verify();
  });
});
