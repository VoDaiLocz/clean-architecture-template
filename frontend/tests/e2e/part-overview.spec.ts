import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('TOEIC part overview', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/learner/toeic-parts', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          parts: Array.from({ length: 7 }, (_, index) => {
            const toeicPart = index + 1;
            return {
              toeicPart,
              name: [
                'Photographs',
                'Question Response',
                'Conversations',
                'Talks',
                'Incomplete Sentences',
                'Text Completion',
                'Reading Comprehension',
              ][index],
              skillType: toeicPart <= 4 ? 'Listening' : 'Reading',
              progressPercent: toeicPart === 3 ? 42 : toeicPart === 7 ? 0 : 10,
              currentUnitTitle: toeicPart === 3 ? 'Inference foundations' : 'Foundation unit',
              isLocked: toeicPart === 7,
              lockedReason: toeicPart === 7 ? 'Complete Part 6 passage workflow before Part 7 unlocks.' : null,
              nextAction: {
                label: toeicPart === 3 ? 'Continue Part 3' : 'Open part',
                route: `/practice/part-${toeicPart}-next`,
              },
              availableTests: toeicPart === 3 ? ['Mini test', 'Part test'] : ['Mini test'],
              weaknessTags: toeicPart === 3 ? ['speaker intent', 'inference'] : [],
            };
          }),
        }),
      });
    });
  });

  test('renders all seven parts with backend progress, lock reasons, tests, and weaknesses', async ({ page }) => {
    await page.goto(`${appUrl}/practice`);

    await expect(page.getByTestId('PartOverview')).toBeVisible();
    await expect(page.getByTestId('toeicPart-1')).toContainText('Photographs');
    await expect(page.getByTestId('toeicPart-3')).toContainText('42%');
    await expect(page.getByTestId('toeicPart-3')).toContainText('Inference foundations');
    await expect(page.getByTestId('toeicPart-3')).toContainText('speaker intent');
    await expect(page.getByTestId('toeicPart-3')).toContainText('Part test');
    await expect(page.getByRole('link', { name: 'Continue Part 3' })).toBeVisible();
    await expect(page.getByTestId('toeicPart-7')).toContainText('Complete Part 6 passage workflow');
    await expect(page.getByTestId('toeicPart-7').getByRole('link')).toHaveCount(0);
    await expect(page.getByTestId('PartOverview').locator('[data-testid^="toeicPart-"]')).toHaveCount(7);
    await expect(page.getByText(/Awaiting backend progress/i)).toHaveCount(0);
    await expect(page.getByText(/raw source/i)).toHaveCount(0);
  });

  test('keeps part overview actionable on mobile without horizontal scrolling', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${appUrl}/practice`);

    await expect(page.getByTestId('toeicPart-1')).toBeVisible();
    await expect(page.getByTestId('toeicPart-7')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Continue Part 3' })).toBeVisible();
  });
});
