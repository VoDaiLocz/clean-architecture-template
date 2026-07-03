import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('Drill and mini test UX', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/learner/practice/drill-part3-inference-1', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          activityId: 'drill-part3-inference-1',
          activitySessionId: 'activitySession-p7-6',
          mode: 'MiniTest',
          title: 'Part 3 inference mini test',
          part: 3,
          skill: 'Inference from speaker intent',
          isLocked: false,
          lockReason: null,
          allowSkip: true,
          questions: [
            {
              id: 'q1',
              prompt: 'What will the speaker most likely do next?',
              choices: ['Call the customer', 'Check the stock room', 'Cancel the order'],
              audioUrl: '/media/part3-mini-1.mp3',
              passage: null,
            },
            {
              id: 'q2',
              prompt: 'Why does the speaker mention a delivery window?',
              choices: ['To schedule a pickup', 'To explain timing', 'To request payment'],
              audioUrl: '/media/part3-mini-2.mp3',
              passage: null,
            },
          ],
        }),
      });
    });

    await page.route('**/api/learner/practice/activitySession-p7-6/attempts', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          activitySessionId: 'activitySession-p7-6',
          resultLabel: 'Submitted to TOEIC engine',
          answeredCount: 1,
          totalCount: 2,
          scorePercent: 50,
          reviewCreated: true,
          nextAction: {
            code: 'ReviewMistakes',
            apiRoute: '/review',
            reason: 'A skipped or incorrect answer created review work before the next unlock.',
          },
        }),
      });
    });
  });

  test('submits mini test answers through backend without pre-submit answer leakage', async ({ page }) => {
    await page.goto(`${appUrl}/practice/drill-part3-inference-1`);

    await expect(page.getByTestId('DrillMiniTestScreen')).toBeVisible();
    await expect(page.getByTestId('question-progress')).toContainText('1 of 2');
    await expect(page.locator('audio')).toHaveCount(1);
    await expect(page.getByText(/correct answer/i)).toHaveCount(0);
    await expect(page.getByText(/explanation/i)).toHaveCount(0);
    await expect(page.getByText(/score/i)).toHaveCount(0);

    await page.getByRole('radio', { name: 'Check the stock room' }).check();
    await page.getByRole('button', { name: 'Next question' }).click();
    await expect(page.getByTestId('question-progress')).toContainText('2 of 2');
    await page.getByRole('button', { name: 'Skip question' }).click();
    await page.getByRole('button', { name: 'Submit mini test' }).click();
    await page.getByRole('button', { name: 'Confirm submit' }).click();

    await expect(page.getByTestId('SubmitAttemptResult')).toContainText('Submitted to TOEIC engine');
    await expect(page.getByTestId('SubmitAttemptResult')).toContainText('50%');
    await expect(page.getByTestId('SubmitAttemptResult')).toContainText('ReviewMistakes');
  });

  test('shows locked mini test state from the API', async ({ page }) => {
    await page.route('**/api/learner/practice/locked-mini-test', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          activityId: 'locked-mini-test',
          activitySessionId: null,
          mode: 'MiniTest',
          title: 'Locked Part 3 mini test',
          part: 3,
          skill: 'Inference',
          isLocked: true,
          lockReason: 'Complete the lesson and repair blockers before this mini test unlocks.',
          allowSkip: false,
          questions: [],
        }),
      });
    });

    await page.goto(`${appUrl}/practice/locked-mini-test`);

    await expect(page.getByTestId('locked-practice-state')).toContainText('Complete the lesson and repair blockers');
    await expect(page.getByRole('button', { name: 'Submit mini test' })).toHaveCount(0);
  });
});
