import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="admin-shell">
      <aside class="sidebar">
        <div>
          <h1>Slotra</h1>
          <p>{{ auth.currentUser()?.displayName }}</p>
        </div>

        <nav>
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Dashboard</a>
          <a routerLink="/services" routerLinkActive="active">Services</a>
        </nav>

        <button type="button" class="ghost" (click)="auth.logout()">Logout</button>
      </aside>

      <main class="content">
        <router-outlet />
      </main>
    </div>
  `
})
export class AdminShellComponent {
  constructor(readonly auth: AuthService) {}
}
