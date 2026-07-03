import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('Mistake repair UX', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/learner/review-queue', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          groups: [
            {
              blockerId: 'blocker-part3-inference',
              unitTitle: 'Part 3 inference foundations',
              part: 3,
              skill: 'Inference from speaker intent',
              blockerReason: 'Repair this mistake before the next mini test unlocks.',
              items: [
                {
                  reviewItemId: 'review-1',
                  questionContext: 'A staff member says they need to check inventory before calling back.',
                  learnerAnswer: 'Cancel the order',
                  correctAnswer: 'Confirm availability before responding.',
                  explanation: 'The next action is implied by the inventory check.',
                  evidence: 'need to check what is still in stock first',
                  audioUrl: '/media/review-1.mp3',
                  passage: 'Staff: I need to check what is still in stock first.',
                },
              ],
            },
          ],
        }),
      });
    });

    await page.route('**/api/learner/review/review-1/repair', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          reviewItemId: 'review-1',
          status: 'Resolved',
          blockerResolved: true,
          learnerMessage: 'Repair accepted. The blocker is resolved.',
          nextAction: {
            code: 'GoToday',
            apiRoute: '/today',
            reason: 'Return to Today for the next backend-assigned activity.',
          },
        }),
      });
    });
  });

  test('learner understands and repairs a mistake from backend evidence', async ({ page }) => {
    await page.goto(`${appUrl}/review`);

    await expect(page.getByTestId('ReviewQueue')).toContainText('Part 3 inference foundations');
    await expect(page.getByTestId('ReviewQueue')).toContainText('Inference from speaker intent');
    await expect(page.getByTestId('MistakeRepair')).toContainText('Cancel the order');
    await expect(page.getByTestId('MistakeRepair')).toContainText('Confirm availability before responding.');
    await expect(page.getByTestId('MistakeRepair')).toContainText('The next action is implied');
    await expect(page.getByTestId('MistakeRepair')).toContainText('need to check what is still in stock first');
    await expect(page.locator('audio')).toHaveCount(1);
    await expect(page.getByTestId('blocker-reason')).toContainText('before the next mini test unlocks');

    await page.getByRole('button', { name: 'Submit repair' }).click();
    await expect(page.getByTestId('repair-result')).toContainText('Repair accepted');
    await expect(page.getByTestId('repair-result')).toContainText('GoToday');
  });

  test('empty review queue sends learner back to Today', async ({ page }) => {
    await page.route('**/api/learner/review-queue', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({ groups: [] }),
      });
    });

    await page.goto(`${appUrl}/review`);

    await expect(page.getByText('No active review blocker')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Back to Today' })).toBeVisible();
  });
});
