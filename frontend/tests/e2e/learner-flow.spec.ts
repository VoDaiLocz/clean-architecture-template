import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test('learner can move from Today to Practice without demo fallback content', async ({ page }) => {
  await page.goto(`${appUrl}/today`);
  await page.getByRole('link', { name: 'Practice' }).click();

  await expect(page).toHaveURL(/\/practice$/);
  await expect(page.getByRole('heading', { name: 'Seven TOEIC parts' })).toBeVisible();
  await expect(page.getByText('Part 1')).toBeVisible();
  await expect(page.getByText('Part 7')).toBeVisible();
  await expect(page.getByText(new RegExp('abide ' + 'by', 'i'))).toHaveCount(0);
});
