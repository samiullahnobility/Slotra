import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { apiBaseUrl } from './api.config';
import { AuthResponse, AuthUser, LoginRequest } from './models';

const tokenKey = 'slotra_admin_token';
const userKey = 'slotra_admin_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly currentUser = signal<AuthUser | null>(this.readUser());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${apiBaseUrl}/auth/login`, request).pipe(
      tap((response) => {
        localStorage.setItem(tokenKey, response.token);
        localStorage.setItem(userKey, JSON.stringify(response.user));
        this.currentUser.set(response.user);
      })
    );
  }

  token(): string | null {
    return localStorage.getItem(tokenKey);
  }

  isAdmin(): boolean {
    return this.currentUser()?.roles?.includes('Admin') ?? false;
  }

  logout(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(userKey);
    this.currentUser.set(null);
    this.router.navigateByUrl('/login');
  }

  private readUser(): AuthUser | null {
    const raw = localStorage.getItem(userKey);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  }
}
