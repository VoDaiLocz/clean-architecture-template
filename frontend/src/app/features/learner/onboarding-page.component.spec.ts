import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { OnboardingPageComponent } from './onboarding-page.component';

describe('OnboardingPageComponent', () => {
  it('renders required onboarding fields without TOEIC content shortcuts', async () => {
    await TestBed.configureTestingModule({
      imports: [OnboardingPageComponent],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(OnboardingPageComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Set up your TOEIC path');
    expect(text).toContain('Target score');
    expect(text).toContain('Current estimate');
    expect(text).toContain('Daily study minutes');
    expect(text).toContain('Timezone');
    expect(text).toContain('Study goal');
    expect(text).not.toContain('correct answer');
    expect(text).not.toContain('PDF');
  });
});
