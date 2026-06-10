import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { getRecommendedPartIds, getToeicPart, toeicParts } from '../src/studyCatalog.ts';

describe('TOEIC study catalog', () => {
  it('exposes all seven TOEIC parts as learner-facing study entry points', () => {
    assert.equal(toeicParts.length, 7);
    assert.deepEqual(
      toeicParts.map((part) => part.id),
      [1, 2, 3, 4, 5, 6, 7],
    );

    for (const part of toeicParts) {
      assert.match(part.title, /^Part [1-7]/);
      assert.ok(part.questionCount > 0, `${part.title} needs a real question count`);
      assert.ok(part.availableTests >= 8, `${part.title} needs enough practice sets`);
      assert.ok(part.roadmap.length >= 3, `${part.title} needs a learning path`);
      assert.ok(part.studyActions.length >= 3, `${part.title} needs learner actions`);
      assert.ok(part.userOutcome.length > 12, `${part.title} needs a clear outcome`);
    }
  });

  it('can resolve part detail pages and recommended next parts', () => {
    assert.equal(getToeicPart(1)?.title, 'Part 1 - Photographs');
    assert.equal(getToeicPart(7)?.title, 'Part 7 - Reading Comprehension');
    assert.equal(getToeicPart(8), undefined);
    assert.deepEqual(getRecommendedPartIds(), [5, 2, 3]);
  });
});
