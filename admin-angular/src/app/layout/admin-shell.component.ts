import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="admin-shell" [class.sidebar-open]="sidebarOpen()">
      <button type="button" class="menu-toggle" (click)="sidebarOpen.set(!sidebarOpen())">Menu</button>

      <aside class="sidebar">
        <div class="sidebar-main">
          <div>
            <h1>Slotra</h1>
            <p>{{ auth.currentUser()?.displayName }}</p>
          </div>

          <nav>
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="sidebarOpen.set(false)">Dashboard</a>
            <a routerLink="/appointments" routerLinkActive="active" (click)="sidebarOpen.set(false)">Appointments</a>
            <a routerLink="/services" routerLinkActive="active" (click)="sidebarOpen.set(false)">Services</a>
            <a routerLink="/staff" routerLinkActive="active" (click)="sidebarOpen.set(false)">Staff</a>
          </nav>
        </div>

        <button type="button" class="ghost" (click)="auth.logout()">Logout</button>
      </aside>

      <main class="content">
        <router-outlet />
      </main>
    </div>
  `
})
export class AdminShellComponent {
  readonly sidebarOpen = signal(false);

  constructor(readonly auth: AuthService) {}
}
