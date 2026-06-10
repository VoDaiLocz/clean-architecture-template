import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:5173';
const apiBase = process.env.TOEIC_API_BASE_URL ?? 'http://localhost:5000';

test.describe('TOEIC learner journey', () => {
  test.beforeEach(async ({ request }) => {
    const response = await request.post(`${apiBase}/api/learner/demo/reset`);
    expect(response.ok()).toBe(true);
  });

  test('learns a unit, repairs a mistake, then unlocks the next unit', async ({ page }) => {
    await page.goto(appUrl);

    await expect(page.getByRole('link', { name: /Admin/i })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Tiếp tục học để mở khóa bài sau' })).toBeVisible();
    await expect(page.getByText('Học Word Form')).toBeVisible();
    await expect(page.getByText('Hoàn thành 100% Word Form để mở khóa.')).toBeVisible();

    await page.getByRole('link', { name: 'Tiếp tục học' }).click();
    await expect(page).toHaveURL(/\/learn\/part5-word-form-lesson$/);
    await expect(page.getByRole('heading', { name: 'Word Form: chọn đúng từ loại' })).toBeVisible();

    await page.getByRole('button', { name: 'Đã hiểu lesson' }).click();
    await page.getByRole('link', { name: 'Drill Word Form' }).click();
    await expect(page).toHaveURL(/\/learn\/part5-word-form-drill$/);
    await expect(page.getByRole('heading', { name: 'Drill: Word Form' })).toBeVisible();

    await page.getByRole('button', { name: 'Hoàn thành drill 15/15' }).click();
    await page.getByRole('link', { name: 'Mini test Word Form' }).click();
    await expect(page).toHaveURL(/\/learn\/part5-word-form-mini-test$/);
    await expect(page.getByRole('heading', { name: 'Mini test: Word Form' })).toBeVisible();

    await page.getByRole('button', { name: 'Nộp 7/10' }).click();
    await expect(page.getByText('Chưa đạt mastery. Cần sửa lỗi trước khi mở khóa bài tiếp theo.')).toBeVisible();
    await page.getByRole('link', { name: 'Sửa lỗi word form' }).click();
    await expect(page).toHaveURL(/\/review$/);
    await expect(page.getByText('1 lỗi')).toBeVisible();

    await page.getByRole('button', { name: 'Đã sửa lỗi này' }).click();
    await expect(page.getByText('Đã sửa lỗi. Làm lại mini test để mở khóa bài tiếp theo.')).toBeVisible();
    await page.getByRole('link', { name: 'Mini test Word Form' }).click();
    await page.getByRole('button', { name: 'Nộp 9/10' }).click();

    await expect(page.getByText('Đã mở khóa bài tiếp theo')).toBeVisible();
    await expect(page.getByText('Đạt mastery. Verb Tense đã được mở khóa.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Học Verb Tense' })).toBeVisible();
  });
});
