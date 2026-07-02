import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app.component';

describe('AppComponent', () => {
  it('renders the Ocean Classroom shell navigation', async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('TOEIC Ocean');
    expect(text).toContain('Today');
    expect(text).toContain('Practice');
    expect(text).toContain('Progress');
    expect(text).toContain('7-Part Overview');
    expect(text).toContain('Profile');
    expect(text).toContain('Settings');
    expect(text).toContain('Logout');
    expect(text).toContain('API connection issue');
  });

  it('renders production shell landmarks and loading skeletons', async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="AppShell"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="LearnerNavigation"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="mobile-learner-navigation"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="route-loading-skeleton"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="global-error-banner"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="user-menu"]')).not.toBeNull();
  });
});
