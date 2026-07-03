import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('Angular TOEIC learner routes', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/learner/home', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          learnerId: 'smoke-learner',
          currentPart: 0,
          currentUnitId: 'placement',
          currentUnitTitle: 'Placement required',
          nextActivity: {
            activityId: 'toeic-placement-start',
            activityType: 'Placement',
            title: 'Start TOEIC placement',
            description: 'Diagnose your current level before the path is generated.',
          },
          reviewCount: 0,
          lockedNextUnit: null,
          primaryAssignment: {
            ctaLabel: 'Start placement',
            route: '/onboarding',
            reason: 'Placement is required before the system can generate your TOEIC path.',
          },
          pathProgress: null,
          dailyTarget: null,
          weakestAreas: [],
          activeSession: null,
        }),
      });
    });

    await page.route('**/api/learner/toeic-parts', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          parts: [
            {
              toeicPart: 1,
              name: 'Photographs',
              skillType: 'Listening',
              progressPercent: 10,
              currentUnitTitle: 'Photo foundations',
              isLocked: false,
              lockedReason: null,
              nextAction: { label: 'Open Part 1', route: '/practice/part-1-next' },
              availableTests: ['Mini test'],
              weaknessTags: [],
            },
          ],
        }),
      });
    });
  });

  test('renders the Ocean Classroom app shell without learner demo content', async ({ page }) => {
    await page.goto(`${appUrl}/today`);

    await expect(page.getByRole('link', { name: /TOEIC Ocean Classroom home/i })).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Learner navigation' })).toBeVisible();
    await expect(page.getByTestId('mobile-learner-navigation')).toHaveCount(1);
    await expect(page.getByRole('link', { name: '7-Part Overview' })).toBeVisible();
    await expect(page.getByTestId('user-menu')).toContainText('Profile');
    await expect(page.getByTestId('global-error-banner')).toContainText('API connection issue');
    await expect(page.getByRole('heading', { name: 'Your TOEIC next action' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Start placement' })).toBeVisible();
    await expect(page.getByText(/backend decides the next required step/i)).toBeVisible();

    await expect(page.getByText(/Word Form/i)).toHaveCount(0);
    await expect(page.getByText(/correct answer/i)).toHaveCount(0);
    await expect(page.getByText(new RegExp('sheet' + '-row', 'i'))).toHaveCount(0);
  });

  test('exposes production learner route families', async ({ page }) => {
    const routes = [
      { path: '/learn', heading: 'Study before practice' },
      { path: '/practice', heading: 'Seven TOEIC parts' },
      { path: '/review', heading: 'Repair mistakes before unlocking' },
      { path: '/tests', heading: 'Practice test cockpit' },
      { path: '/progress', heading: 'Improvement, weaknesses, and next work' },
    ];

    for (const route of routes) {
      await page.goto(`${appUrl}${route.path}`);
      await expect(page.getByRole('heading', { name: route.heading })).toBeVisible();
      await expect(page.getByText(/raw source/i)).toHaveCount(0);
    }
  });

  test('keeps primary learner navigation usable on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${appUrl}/today`);

    await expect(page.getByRole('navigation', { name: 'Mobile learner navigation' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Today' }).last()).toBeVisible();
    await expect(page.getByRole('link', { name: 'Practice' }).last()).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Your TOEIC next action' })).toBeVisible();
  });
});
