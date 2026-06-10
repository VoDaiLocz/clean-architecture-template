import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

const apiBase = process.env.TOEIC_API_BASE_URL ?? 'http://localhost:5000';

describe('end-user learner flow', () => {
  it('guides a learner from lesson to review repair to unlock', async () => {
    await post('/api/learner/demo/reset', {});

    const initialHome = await get('/api/learner/home');
    assert.equal(initialHome.nextActivity.activityType, 'ConceptLesson');
    assert.equal(initialHome.nextActivity.activityId, 'part5-word-form-lesson');
    assert.equal(initialHome.lockedNextUnit.unitId, 'part5-verb-tense');

    const lesson = await get(`/api/learner/activities/${initialHome.nextActivity.activityId}`);
    assert.equal(lesson.activityType, 'ConceptLesson');
    assert.ok(lesson.lessonPoints.length >= 3);
    await post(`/api/learner/activities/${lesson.activityId}/attempts`, {});

    const afterLessonHome = await get('/api/learner/home');
    assert.equal(afterLessonHome.nextActivity.activityType, 'FocusDrill');
    await post(`/api/learner/activities/${afterLessonHome.nextActivity.activityId}/attempts`, {
      correctCount: 15,
      totalCount: 15,
    });

    const afterDrillHome = await get('/api/learner/home');
    assert.equal(afterDrillHome.nextActivity.activityType, 'MiniTest');
    const failedMiniTest = await post(`/api/learner/activities/${afterDrillHome.nextActivity.activityId}/attempts`, {
      correctCount: 7,
      totalCount: 10,
      wrongItemIds: ['p5-word-form-007'],
      errorTag: 'word_form',
    });

    assert.equal(failedMiniTest.unitCompleted, false);
    assert.equal(failedMiniTest.reviewCount, 1);
    assert.equal(failedMiniTest.nextActivity.activityType, 'MistakeRepair');

    const review = await get('/api/learner/review');
    assert.equal(review.length, 1);
    assert.equal(review[0].errorTag, 'word_form');
    await post(`/api/learner/review/${encodeURIComponent(review[0].reviewItemId)}/attempts`, {});

    const afterReviewHome = await get('/api/learner/home');
    assert.equal(afterReviewHome.nextActivity.activityType, 'MiniTest');
    const passedMiniTest = await post(`/api/learner/activities/${afterReviewHome.nextActivity.activityId}/attempts`, {
      correctCount: 9,
      totalCount: 10,
      wrongItemIds: [],
      errorTag: 'word_form',
    });

    assert.equal(passedMiniTest.unitCompleted, true);
    assert.equal(passedMiniTest.reviewCount, 0);
    assert.equal(passedMiniTest.nextActivity.activityId, 'part5-verb-tense-lesson');

    const unlockedHome = await get('/api/learner/home');
    assert.equal(unlockedHome.lockedNextUnit, null);
    assert.equal(unlockedHome.nextActivity.title, 'Học Verb Tense');
  });
});

async function get(path) {
  const response = await fetch(`${apiBase}${path}`);
  assert.equal(response.ok, true, `${path} should return 2xx`);
  return response.json();
}

async function post(path, body) {
  const response = await fetch(`${apiBase}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  assert.equal(response.ok, true, `${path} should return 2xx`);
  if (response.status === 204) {
    return null;
  }

  return response.json();
}
