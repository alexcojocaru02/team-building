import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginDto, RegisterDto, AuthResponse, User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  
  // Update this URL to match your API endpoint
  private apiUrl = 'https://localhost:7241';
  
  private currentUserSignal = signal<User | null>(null);
  private tokenSignal = signal<string | null>(null);
  
  public currentUser = computed(() => this.currentUserSignal());
  public isAuthenticated = computed(() => !!this.tokenSignal());

  constructor() {
    // Load token and user from localStorage on init
    const token = localStorage.getItem('token');
    if (token) {
      this.tokenSignal.set(token);
      this.loadUserFromToken(token);
    }
  }

  register(dto: RegisterDto): Observable<AuthResponse> {
    const params = new URLSearchParams();
    params.set('Email', dto.email);
    params.set('Password', dto.password);
    if (dto.name) {
      params.set('Name', dto.name);
    }
    return this.http.post<AuthResponse>(`${this.apiUrl}/register?${params.toString()}`, {}).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  login(dto: LoginDto): Observable<AuthResponse> {
    const params = new URLSearchParams();
    params.set('Email', dto.email);
    params.set('Password', dto.password);
    return this.http.post<AuthResponse>(`${this.apiUrl}/login?${params.toString()}`, {}).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  logout(): void {
    this.tokenSignal.set(null);
    this.currentUserSignal.set(null);
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  private handleAuthSuccess(response: AuthResponse): void {
    this.tokenSignal.set(response.token);
    localStorage.setItem('token', response.token);
    this.loadUserFromToken(response.token);
    this.router.navigate(['/']);
  }

  private loadUserFromToken(token: string): void {
    try {
      // Decode JWT token (simple base64 decode)
      const payload = JSON.parse(atob(token.split('.')[1]));
      const user: User = {
        id: payload.sub || payload.nameid,
        email: payload.email,
        name: payload.name || payload.unique_name
      };
      this.currentUserSignal.set(user);
    } catch (error) {
      console.error('Error decoding token:', error);
      this.logout();
    }
  }
}
