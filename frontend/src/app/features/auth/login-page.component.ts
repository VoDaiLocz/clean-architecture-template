import { Component } from '@angular/core';

@Component({
  selector: 'toeic-login-page',
  standalone: true,
  template: `
    <section class="auth-panel">
      <p class="eyebrow">Account</p>
      <h1>Sign in to TOEIC Ocean</h1>
      <p>Authentication screens will connect to the P9 auth contracts.</p>
    </section>
  `,
})
export class LoginPageComponent {}
