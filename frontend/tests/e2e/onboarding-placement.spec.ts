import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('Onboarding and placement UX', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/learner/onboarding', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          learnerId: 'learner-p7-3',
          targetScore: 850,
          currentEstimatedScore: 610,
          dailyStudyMinutes: 45,
          timeZoneId: 'Asia/Ho_Chi_Minh',
          nextAction: {
            code: 'StartPlacement',
            apiRoute: '/api/learner/placement/start',
            reason: 'Placement is required before the system can generate a personalized TOEIC path.',
          },
        }),
      });
    });

    await page.route('**/api/learner/placement/start', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          sessionId: 'placement-p7-3',
          learnerId: 'learner-p7-3',
          status: 'InProgress',
          nextAction: {
            code: 'ResumePlacement',
            apiRoute: '/api/learner/placement/placement-p7-3',
            reason: 'Continue the TOEIC placement diagnosis before the learning path can be generated.',
          },
        }),
      });
    });

    await page.route('**/api/learner/placement/placement-p7-3', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          sessionId: 'placement-p7-3',
          status: 'InProgress',
          answeredCount: 0,
          totalCount: 2,
          questions: [
            {
              id: 'q1',
              part: 2,
              prompt: 'Select the response that best matches the spoken question.',
              choices: ['A', 'B', 'C'],
            },
            {
              id: 'q2',
              part: 5,
              prompt: 'Select the option that completes the sentence.',
              choices: ['A', 'B', 'C', 'D'],
            },
          ],
        }),
      });
    });

    await page.route('**/api/learner/placement/placement-p7-3/submit', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          sessionId: 'placement-p7-3',
          estimateBand: '605-655',
          label: 'Diagnostic estimate',
          weaknesses: [
            { part: 2, skill: 'Listening response speed' },
            { part: 5, skill: 'Sentence structure accuracy' },
          ],
          nextAction: {
            code: 'GoToday',
            apiRoute: '/api/learner/home',
            reason: 'Your first learning path can now be generated from the diagnostic result.',
          },
        }),
      });
    });
  });

  test('learner completes onboarding, starts placement, submits diagnosis, and sees next action', async ({ page }) => {
    await page.goto(`${appUrl}/onboarding`);

    await expect(page.getByRole('heading', { name: 'Set up your TOEIC path' })).toBeVisible();
    await page.getByLabel('Target score').fill('850');
    await page.getByLabel('Current estimate').fill('610');
    await page.getByLabel('Daily study minutes').fill('45');
    await page.getByLabel('Timezone').selectOption('Asia/Ho_Chi_Minh');
    await page.getByLabel('Study goal').fill('Reach 800+ with stable Listening and Reading routines.');
    await page.getByRole('button', { name: 'Save profile' }).click();

    await expect(page.getByTestId('next-action')).toContainText('StartPlacement');
    await page.getByRole('button', { name: 'Start placement' }).click();

    await expect(page).toHaveURL(/\/placement\/placement-p7-3/);
    await expect(page.getByRole('heading', { name: 'TOEIC placement diagnosis' })).toBeVisible();
    await expect(page.getByTestId('placement-progress')).toContainText('1 of 2');
    await expect(page.getByText(/correct answer/i)).toHaveCount(0);
    await expect(page.getByText(/explanation/i)).toHaveCount(0);

    await page.getByRole('radio', { name: 'A' }).check();
    await page.getByRole('button', { name: 'Next question' }).click();
    await expect(page.getByTestId('placement-progress')).toContainText('2 of 2');
    await page.getByRole('button', { name: 'Skip question' }).click();
    await page.getByRole('button', { name: 'Submit placement' }).click();
    await page.getByRole('button', { name: 'Confirm submit' }).click();

    await expect(page.getByRole('heading', { name: 'Diagnostic estimate' })).toBeVisible();
    await expect(page.getByText('605-655')).toBeVisible();
    await expect(page.getByText('Listening response speed')).toBeVisible();
    await expect(page.getByTestId('placement-result-next-action')).toContainText('GoToday');
  });

  test('placement layout keeps navigation usable on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${appUrl}/placement/placement-p7-3`);

    await expect(page.getByTestId('placement-progress')).toContainText('1 of 2');
    await expect(page.getByRole('button', { name: 'Skip question' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Next question' })).toBeVisible();
  });
});
