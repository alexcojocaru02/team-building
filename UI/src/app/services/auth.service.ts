import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginDto, RegisterDto, AuthResponse, User } from '../models/auth.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  
  private apiUrl = environment.apiUrl;
  
  private currentUserSignal = signal<User | null>(null);
  private tokenSignal = signal<string | null>(null);
  
  public currentUser = computed(() => this.currentUserSignal());
  public isAuthenticated = computed(() => !!this.tokenSignal());
  public currentUserRole = computed(() => this.currentUserSignal()?.role);
  public isAdmin = computed(() => this.currentUserSignal()?.role === 'Admin');
  public isTeamOwner = computed(() => this.currentUserSignal()?.role === 'TeamOwner');

  constructor() {
    // Load token and user from localStorage on init
    const token = localStorage.getItem('token');
    if (token) {
      // Check if token is expired before using it
      if (!this.isTokenExpired(token)) {
        this.tokenSignal.set(token);
        this.loadUserFromToken(token);
      } else {
        localStorage.removeItem('token');
        this.clearAuthState(false);
      }
    }
  }

  register(dto: RegisterDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, {
      fullName: dto.fullName,
      email: dto.email,
      password: dto.password,
    }).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  login(dto: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, {
      email: dto.email,
      password: dto.password,
    }).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  logout(): void {
    this.clearAuthState(true);
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/users/me`).pipe(
      tap(() => this.clearAuthState(true))
    );
  }

  private clearAuthState(navigate: boolean = false): void {
    this.tokenSignal.set(null);
    this.currentUserSignal.set(null);
    localStorage.removeItem('token');
    if (navigate) {
      this.router.navigate(['/login']);
    }
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  updateToken(token: string): void {
    this.tokenSignal.set(token);
    localStorage.setItem('token', token);
    this.loadUserFromToken(token);
  }

  private handleAuthSuccess(response: AuthResponse): void {
    this.tokenSignal.set(response.token);
    localStorage.setItem('token', response.token);
    this.loadUserFromToken(response.token);
    this.router.navigate(['/']);
  }

  private loadUserFromToken(token: string): void {
    try {
      const payload = this.decodeJwtPayload(token);
      const user: User = {
        id: this.getClaimString(payload, ['sub', 'nameid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']),
        fullName: this.getClaimString(payload, ['unique_name', 'name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']),
        email: this.getClaimString(payload, ['email', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']),
        role: this.getClaimString(payload, ['role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role']) // Extract normalized role from JWT
      };
      this.currentUserSignal.set(user);
    } catch (error) {
      console.error('Error decoding token:', error);
      this.clearAuthState(false);
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = this.decodeJwtPayload(token);
      const exp = this.getClaimNumber(payload, ['exp']);
      if (!exp) return false;
      
      // exp is in seconds, convert to milliseconds
      const expirationTime = exp * 1000;
      const currentTime = Date.now();
      
      // Consider token expired if current time is past expiration (with 1 minute buffer)
      return currentTime > expirationTime - 60000;
    } catch {
      return true;
    }
  }

  private decodeJwtPayload(token: string): Record<string, unknown> {
    const payloadPart = token.split('.')[1];
    if (!payloadPart) {
      throw new Error('Invalid JWT token format');
    }

    const base64 = payloadPart
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const paddedBase64 = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
    const json = atob(paddedBase64);
    return JSON.parse(json) as Record<string, unknown>;
  }

  private getClaimString(payload: Record<string, unknown>, keys: string[]): string {
    for (const key of keys) {
      const value = payload[key];
      if (typeof value === 'string' && value.length > 0) {
        return value;
      }
    }

    return '';
  }

  private getClaimNumber(payload: Record<string, unknown>, keys: string[]): number | null {
    for (const key of keys) {
      const value = payload[key];
      if (typeof value === 'number') {
        return value;
      }

      if (typeof value === 'string') {
        const parsed = Number(value);
        if (!Number.isNaN(parsed)) {
          return parsed;
        }
      }
    }

    return null;
  }
}

