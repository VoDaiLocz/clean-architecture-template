import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'node:test';

const frontendRoot = new URL('..', import.meta.url).pathname;

function read(path) {
  return readFileSync(join(frontendRoot, path), 'utf8');
}

describe('Angular production frontend baseline', () => {
  it('uses Angular workspace and scripts instead of legacy Vite scripts', () => {
    assert.ok(existsSync(join(frontendRoot, 'angular.json')), 'angular.json is required');

    const packageJson = JSON.parse(read('package.json'));
    assert.equal(packageJson.scripts.dev, 'ng serve --host 0.0.0.0');
    assert.equal(packageJson.scripts.build, 'ng build');
    assert.equal(packageJson.scripts.test, 'node --test tests/*.test.mjs');
    assert.equal(packageJson.scripts['test:unit'], 'ng test --watch=false');
    assert.equal(packageJson.scripts['test:e2e:browser'], 'playwright test tests/e2e --project=chromium');
    assert.ok(packageJson.dependencies['@angular/core'], '@angular/core dependency is required');
    assert.ok(!packageJson.devDependencies.vite, 'legacy Vite dependency must be removed');
  });

  it('defines Angular feature boundaries required by the spec', () => {
    const requiredPaths = [
      'src/app/core/api',
      'src/app/core/auth',
      'src/app/core/interceptors',
      'src/app/shared/ui',
      'src/app/features/auth',
      'src/app/features/learner',
      'src/app/features/admin',
      'src/styles/tokens.css',
    ];

    for (const path of requiredPaths) {
      assert.ok(existsSync(join(frontendRoot, path)), `${path} must exist`);
    }
  });

  it('implements the Ocean Classroom design token baseline', () => {
    const tokens = read('src/styles/tokens.css');
    assert.match(tokens, /--toeic-canvas:\s*#f4f9fc/i);
    assert.match(tokens, /--toeic-primary:\s*#0787c8/i);
    assert.match(tokens, /--toeic-primary-soft:\s*#dff3fb/i);
    assert.match(tokens, /--toeic-text:\s*#10222f/i);
    assert.match(tokens, /--toeic-reading:\s*#2f8fdd/i);
    assert.match(tokens, /--toeic-listening:\s*#10b9ca/i);
  });

  it('removes legacy demo-only production source files', () => {
    assert.ok(!existsSync(join(frontendRoot, 'src/studyCatalog.ts')), 'legacy studyCatalog.ts must be removed');

    const mainSource = read('src/main.ts');
    assert.doesNotMatch(mainSource, new RegExp('legacy' + 'DemoOnly', 'i'));
    assert.doesNotMatch(mainSource, new RegExp('correct' + 'Answer', 'i'));
    assert.doesNotMatch(mainSource, new RegExp('hardcoded ' + 'question', 'i'));
  });
});
