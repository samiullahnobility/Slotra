import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DashboardSummary } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CurrencyPipe, RouterLink],
  template: `
    <header class="page-header">
      <div>
        <h2>Dashboard</h2>
        <p>Live operational summary from the Slotra API.</p>
      </div>
      <button type="button" (click)="load()" [disabled]="loading()">
        {{ loading() ? 'Refreshing...' : 'Refresh' }}
      </button>
    </header>

    @if (error()) {
      <div class="error">{{ error() }}</div>
    }

    @if (loading() && !summary()) {
      <div class="panel muted">Loading dashboard...</div>
    }

    <section class="metric-grid">
      <article>
        <span>Total</span>
        <strong>{{ summary()?.totalAppointments ?? 0 }}</strong>
      </article>
      <article>
        <span>Today</span>
        <strong>{{ summary()?.todayAppointments ?? 0 }}</strong>
      </article>
      <article>
        <span>Completed</span>
        <strong>{{ summary()?.completedAppointments ?? 0 }}</strong>
      </article>
      <article>
        <span>Cancelled</span>
        <strong>{{ summary()?.cancelledAppointments ?? 0 }}</strong>
      </article>
      <article>
        <span>Revenue</span>
        <strong>{{ (summary()?.estimatedRevenue ?? 0) | currency }}</strong>
      </article>
    </section>

    <section class="quick-actions">
      <a routerLink="/appointments">Manage appointments</a>
      <a routerLink="/services">Manage services</a>
      <a routerLink="/staff">Manage staff</a>
    </section>
  `
})
export class DashboardComponent implements OnInit {
  readonly summary = signal<DashboardSummary | null>(null);
  readonly error = signal('');
  readonly loading = signal(false);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error.set('');
    this.loading.set(true);
    this.api.dashboardSummary().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (summary) => this.summary.set(summary),
      error: () => this.error.set('Could not load dashboard summary.')
    });
  }
}
