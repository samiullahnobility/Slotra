import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-page">
      <section class="login-panel">
        <h1>Slotra Admin</h1>
        <p>Sign in to manage services, staff, and appointments.</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>
            Email
            <input type="email" formControlName="email" autocomplete="email">
          </label>

          <label>
            Password
            <input type="password" formControlName="password" autocomplete="current-password">
          </label>

          @if (error()) {
            <div class="error">{{ error() }}</div>
          }

          <button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Signing in...' : 'Sign in' }}
          </button>
        </form>
      </section>
    </main>
  `
})
export class LoginComponent {
  readonly loading = signal(false);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    email: ['admin@slotra.local', [Validators.required, Validators.email]],
    password: ['Admin123!', [Validators.required]]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        if (!this.auth.isAdmin()) {
          this.auth.logout();
          this.error.set('This account does not have admin access.');
          return;
        }

        this.router.navigateByUrl('/');
      },
      error: () => {
        this.error.set('Login failed. Check email and password.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}
