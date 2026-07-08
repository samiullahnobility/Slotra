import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-page">
      <header class="login-topbar">
        <h1>Slotra</h1>
        <p>Manage appointment operations with services, staff, and booking activity.</p>
      </header>

      <section class="login-layout">
        <div class="login-intro">
          <span>Appointment booking software</span>
          <h2>Run your schedule with Slotra</h2>
          <p>
            Slotra helps appointment-based teams manage services, staff schedules,
            bookings, and customer communication from one shared system.
          </p>
          <img class="login-visual" src="assets/login-appointments.png" alt="Appointment scheduling workspace">
        </div>

        <section class="login-panel">
          <h2>Admin sign in</h2>
          <p>Sign in to configure services, manage staff, and review daily appointment activity.</p>

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
    private readonly router: Router,
    private readonly toastr: ToastrService
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
          this.toastr.error('This account does not have admin access.');
          return;
        }

        this.toastr.success('Signed in.');
        this.router.navigateByUrl('/');
      },
      error: () => {
        this.error.set('Login failed. Check email and password.');
        this.toastr.error('Login failed. Check email and password.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}
