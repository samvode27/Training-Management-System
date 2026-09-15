import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TmsUser {
  userId?: string;
  email?: string;
  displayName: string;
  role: string;
  roles?: string[];
}

export interface LoginRequest {
  email?: string;
  username?: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken?: string;
  displayName?: string;
  role?: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // Store tokens in memory (not localStorage - enterprise defense)
  private accessToken = signal<string | null>(null);
  private refreshToken = signal<string | null>(null);

  currentUser = signal<TmsUser | null>(null);
  isAuthenticated = computed(() => this.currentUser() !== null);

  getAccessToken(): string | null {
    return this.accessToken();
  }

  getToken(): string | null {
    return this.accessToken();
  }

  hasRole(role: string): boolean {
    const user = this.currentUser();
    if (!user) return false;
    return Boolean(
      user.role === role ||
      user.role === 'Admin' ||
      (user.roles && user.roles.includes(role))
    );
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => this.hasRole(role));
  }

  /**
   * Strictly checks if this course is assigned to the current user
   */
  isCourseOwner(instructorId?: string | number | null): boolean {
    const user = this.currentUser();
    if (!user || !instructorId) return false;

    const instStr = instructorId.toString().toLowerCase().trim();
    const uId = (user.userId || '').toLowerCase().trim();
    const uName = (user.displayName || '').toLowerCase().trim();
    const uEmail = (user.email || '').toLowerCase().trim();

    return (
      (uId !== '' && (instStr === uId || instStr.includes(uId) || uId.includes(instStr))) ||
      (uName !== '' && (instStr === uName || uName.includes(instStr) || instStr.includes(uName))) ||
      (uEmail !== '' && (instStr === uEmail || uEmail.startsWith(instStr) || instStr.startsWith(uEmail)))
    );
  }

  /**
   * Checks if user has permission to edit/manage (Admin has universal access)
   */
  canManageCourse(instructorId?: string | number | null): boolean {
    const user = this.currentUser();
    if (!user) return false;
    if (this.hasRole('Admin')) return true;
    return this.isCourseOwner(instructorId);
  }

  async login(credentials: LoginRequest): Promise<void> {
    const payloadReq = {
      email: credentials.email || credentials.username,
      username: credentials.username || credentials.email,
      password: credentials.password,
    };

    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, payloadReq)
    );

    if (res && res.accessToken) {
      this.accessToken.set(res.accessToken);
      if (res.refreshToken) this.refreshToken.set(res.refreshToken);

      try {
        const parts = res.accessToken.split('.');
        if (parts.length === 3) {
          const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
          const payload = JSON.parse(atob(base64));
          const role =
            payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
            payload.role ||
            res.role ||
            'Student';
          const email = payload.email || payload.sub || credentials.email;
          const displayName = payload.FirstName || payload.name || res.displayName || email || 'User';

          this.currentUser.set({
            userId: payload.sub || payload.nameid || payload.uid || credentials.username || credentials.email,
            email: email,
            displayName: displayName,
            role: role,
          });
          return;
        }
      } catch (e) {
        // Continue to /auth/me or direct response fields
      }
    }

    // Fallback: Set user directly from response if present
    if (res?.displayName && res?.role) {
      const userIdentifier = credentials.username || credentials.email || 'user';
      this.currentUser.set({
        userId: userIdentifier,
        displayName: res.displayName,
        role: res.role,
      });
      return;
    }

    // Fallback: Fetch user profile from /auth/me
    try {
      const user = await firstValueFrom(
        this.http.get<TmsUser>(`${environment.apiUrl}/auth/me`)
      );
      this.currentUser.set(user);
    } catch {
      // Session handling
    }
  }

  async register(data: RegisterRequest): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${environment.apiUrl}/auth/register`, data)
    );
  }

  async refreshTokenIfNeeded(): Promise<void> {
    if (!this.refreshToken()) return;

    try {
      const response = await firstValueFrom(
        this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, {
          refreshToken: this.refreshToken(),
        })
      );
      if (response.accessToken) {
        this.accessToken.set(response.accessToken);
      }
      if (response.refreshToken) {
        this.refreshToken.set(response.refreshToken);
      }
    } catch {
      this.logout();
    }
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post<void>(`${environment.apiUrl}/auth/logout`, {})
      );
    } catch {
      // Ignore network errors on logout
    } finally {
      this.accessToken.set(null);
      this.refreshToken.set(null);
      this.currentUser.set(null);
      this.router.navigate(['/login']);
    }
  }

  async checkSession(): Promise<void> {
    try {
      const user = await firstValueFrom(
        this.http.get<TmsUser>(`${environment.apiUrl}/auth/me`)
      );
      this.currentUser.set(user);
    } catch {
      this.currentUser.set(null);
    }
  }
}
