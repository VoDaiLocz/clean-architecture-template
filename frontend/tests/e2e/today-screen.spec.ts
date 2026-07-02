import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('Learner Today screen', () => {
  test('shows backend-assigned primary work, blockers, progress, weakness, and resume state', async ({ page }) => {
    await page.route('**/api/learner/home', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          learnerId: 'learner-p7-4',
          currentPart: 3,
          currentUnitId: 'part3-inference-1',
          currentUnitTitle: 'Part 3 inference foundations',
          nextActivity: {
            activityId: 'lesson-part3-inference-1',
            activityType: 'Lesson',
            title: 'Learn inference signals in conversations',
            description: 'Backend assigned this lesson because Part 3 inference is blocking the next mini test.',
          },
          reviewCount: 4,
          lockedNextUnit: {
            unitId: 'part3-mini-test-1',
            title: 'Part 3 mini test',
            reasonCodes: ['REVIEW_BLOCKER', 'LESSON_REQUIRED'],
            learnerMessage: 'Repair 4 review items and complete the lesson before this mini test unlocks.',
          },
          primaryAssignment: {
            ctaLabel: 'Continue lesson',
            route: '/learn/lesson-part3-inference-1',
            reason: 'This is the shortest path to unlock your next Part 3 mini test.',
          },
          pathProgress: {
            completedUnits: 12,
            totalUnits: 40,
            percent: 30,
          },
          dailyTarget: {
            completedMinutes: 25,
            targetMinutes: 45,
          },
          weakestAreas: [
            { part: 3, skill: 'Inference from speaker intent' },
            { part: 5, skill: 'Verb form accuracy' },
          ],
          activeSession: {
            label: 'Resume active Part 3 lesson session',
            route: '/learn/lesson-part3-inference-1/session',
          },
        }),
      });
    });

    await page.goto(`${appUrl}/today`);

    await expect(page.getByTestId('TodayScreen')).toBeVisible();
    await expect(page.getByTestId('primaryAssignment')).toContainText('Learn inference signals in conversations');
    await expect(page.getByTestId('primaryAssignment')).toContainText('Part 3 inference foundations');
    await expect(page.getByTestId('primaryAssignment')).toContainText('This is the shortest path');
    await expect(page.getByRole('link', { name: 'Continue lesson' })).toBeVisible();
    await expect(page.getByTestId('blockers')).toContainText('Repair 4 review items');
    await expect(page.getByTestId('path-progress')).toContainText('30%');
    await expect(page.getByTestId('daily-target')).toContainText('25 / 45 minutes');
    await expect(page.getByTestId('weakest-areas')).toContainText('Inference from speaker intent');
    await expect(page.getByTestId('active-session')).toContainText('Resume active Part 3 lesson session');
    await expect(page.getByText(/correct answer/i)).toHaveCount(0);
    await expect(page.getByText(/raw source/i)).toHaveCount(0);
  });

  test('keeps mobile order focused on action, blocker, progress, then weakness', async ({ page }) => {
    await page.route('**/api/learner/home', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          learnerId: 'learner-p7-4-mobile',
          currentPart: 0,
          currentUnitId: 'onboarding',
          currentUnitTitle: 'Complete onboarding',
          nextActivity: {
            activityId: 'learner-onboarding',
            activityType: 'Onboarding',
            title: 'Complete TOEIC profile',
            description: 'Set your target score and daily plan.',
          },
          reviewCount: 0,
          lockedNextUnit: null,
          primaryAssignment: {
            ctaLabel: 'Start onboarding',
            route: '/onboarding',
            reason: 'A profile is required before placement and learning path generation.',
          },
          pathProgress: null,
          dailyTarget: null,
          weakestAreas: [],
          activeSession: null,
        }),
      });
    });

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${appUrl}/today`);

    const orderedSections = await page.locator('[data-mobile-order]').evaluateAll((nodes) =>
      nodes.map((node) => node.getAttribute('data-mobile-order')),
    );
    expect(orderedSections).toEqual(['primary', 'blockers', 'progress', 'weakness']);
    await expect(page.getByRole('link', { name: 'Start onboarding' })).toBeVisible();
  });
});
