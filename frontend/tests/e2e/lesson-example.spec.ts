import { expect, test } from '@playwright/test';

const appUrl = process.env.TOEIC_APP_URL ?? 'http://localhost:4200';

test.describe('Lesson and guided example UX', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/learner/lessons/lesson-part3-inference-1', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          lessonId: 'lesson-part3-inference-1',
          activitySessionId: 'activitySession-p7-5',
          title: 'Learn inference signals in conversations',
          objective: 'Identify speaker intent before answering Part 3 inference questions.',
          part: 3,
          skill: 'Inference from speaker intent',
          concept: {
            heading: 'Listen for purpose, not isolated words',
            body:
              'Published lessons explain what the learner must notice before practice. The learner studies the intent signal, turn change, and final request before choosing an answer.',
          },
          example: {
            prompt: 'A speaker says they will check the inventory before calling back.',
            question: 'What will the speaker most likely do next?',
            answer: 'Confirm availability before responding.',
            rationale: 'The future action is implied by the inventory check, not stated as a direct answer.',
          },
          trap: 'Do not choose an option only because it repeats a word from the conversation.',
          passage: 'Customer: Could you send the replacement today? Staff: I need to check what is still in stock first.',
          media: {
            audioUrl: '/media/lesson-part3-inference-1.mp3',
            imageUrl: null,
          },
          nextAction: {
            code: 'StartDrill',
            apiRoute: '/api/learner/activities/drill-part3-inference-1',
            reason: 'Complete the guided example before the backend unlocks the first drill.',
          },
        }),
      });
    });

    await page.route('**/api/learner/lessons/activitySession-p7-5/complete', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          activitySessionId: 'activitySession-p7-5',
          nextAction: {
            code: 'StartDrill',
            apiRoute: '/learn/drill-part3-inference-1',
            reason: 'The lesson is complete. Continue to the assigned drill.',
          },
        }),
      });
    });
  });

  test('teaches lesson context before revealing guided example answer and next action', async ({ page }) => {
    await page.goto(`${appUrl}/learn/lesson-part3-inference-1`);

    await expect(page.getByTestId('LessonHeader')).toContainText('Part 3');
    await expect(page.getByTestId('LessonHeader')).toContainText('Identify speaker intent');
    await expect(page.getByTestId('LessonContentBody')).toContainText('Listen for purpose');
    await expect(page.getByTestId('LessonContentBody')).toContainText('Customer: Could you send');
    await expect(page.getByTestId('GuidedExample')).toContainText('What will the speaker most likely do next?');
    await expect(page.getByText('Confirm availability before responding.')).toHaveCount(0);
    await expect(page.getByText(/correct answer/i)).toHaveCount(0);

    await page.getByRole('button', { name: 'Reveal guided answer' }).click();
    await expect(page.getByText('Confirm availability before responding.')).toBeVisible();
    await expect(page.getByText('Do not choose an option only because it repeats a word')).toBeVisible();

    await page.getByRole('button', { name: 'Complete lesson' }).click();
    await expect(page.getByTestId('LessonNextActionFooter')).toContainText('StartDrill');
    await expect(page.getByTestId('LessonNextActionFooter')).toContainText('Continue to the assigned drill');
  });

  test('keeps long lesson reader usable on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${appUrl}/learn/lesson-part3-inference-1`);

    await expect(page.getByTestId('LessonHeader')).toBeVisible();
    await expect(page.getByTestId('GuidedExample')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reveal guided answer' })).toBeVisible();
  });
});
