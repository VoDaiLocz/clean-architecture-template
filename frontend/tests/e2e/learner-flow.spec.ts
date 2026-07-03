import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test('learner can move from Today to Practice without demo fallback content', async ({ page }) => {
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
          {
            toeicPart: 7,
            name: 'Reading Comprehension',
            skillType: 'Reading',
            progressPercent: 0,
            currentUnitTitle: 'Long passage foundations',
            isLocked: true,
            lockedReason: 'Complete earlier reading units first.',
            nextAction: { label: 'Open Part 7', route: '/practice/part-7-next' },
            availableTests: ['Mini test'],
            weaknessTags: [],
          },
        ],
      }),
    });
  });

  await page.goto(`${appUrl}/today`);
  await page.getByRole('link', { name: 'Practice' }).click();

  await expect(page).toHaveURL(/\/practice$/);
  await expect(page.getByRole('heading', { name: 'Seven TOEIC parts' })).toBeVisible();
  await expect(page.getByTestId('toeicPart-1')).toContainText('Photographs');
  await expect(page.getByTestId('toeicPart-7')).toContainText('Reading Comprehension');
  await expect(page.getByText(new RegExp('abide ' + 'by', 'i'))).toHaveCount(0);
});
