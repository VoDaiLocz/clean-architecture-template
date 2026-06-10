import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:5173';
const apiBase = process.env.TOEIC_API_BASE_URL ?? 'http://localhost:5000';

test.describe('TOEIC learner routes', () => {
  test.beforeEach(async ({ request }) => {
    const response = await request.post(`${apiBase}/api/learner/demo/reset`);
    expect(response.ok()).toBe(true);
  });

  test('exposes learner-facing sections without admin controls', async ({ page }) => {
    const routes = [
      { path: '/', heading: 'Tiếp tục học để mở khóa bài sau', text: 'Học Word Form' },
      { path: '/practice', heading: 'Chọn Part muốn tăng điểm', text: 'Part 7 - Reading Comprehension' },
      { path: '/listening', heading: 'Luyện nghe Part 1-4', text: 'Part 2 - Question-Response' },
      { path: '/vocabulary', heading: 'Từ vựng hay gặp trong TOEIC', text: 'abide by' },
      { path: '/review', heading: 'Sửa lỗi để lần sau không mất điểm', text: 'Không còn lỗi chặn' },
    ];

    for (const route of routes) {
      await page.goto(`${appUrl}${route.path}`);
      await expect(page.getByRole('heading', { name: route.heading })).toBeVisible();
      await expect(page.getByText(route.text).first()).toBeVisible();
      await expect(page.getByRole('link', { name: /Admin/i })).toHaveCount(0);
    }
  });

  test('opens every TOEIC part detail page with a study entry point', async ({ page }) => {
    for (let part = 1; part <= 7; part += 1) {
      await page.goto(`${appUrl}/part/${part}`);
      await expect(page.getByRole('heading', { name: new RegExp(`Part ${part}`) }).first()).toBeVisible();
      await expect(page.getByRole('link', { name: `Vào học Part ${part}` })).toBeVisible();
      await expect(page.getByText('Cần tránh')).toBeVisible();
      await expect(page.getByRole('link', { name: /Admin/i })).toHaveCount(0);
    }
  });
});
