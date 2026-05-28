import { HttpTestingController, HttpClientTestingModule } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    localStorage.clear();
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        provideZonelessChangeDetection(),
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should POST register payload to the backend and store the returned token', () => {
    const registerDto = {
      fullName: 'Alex Cojocaru',
      email: 'alex@example.com',
      password: 'Password123!'
    };
    const token = createJwtToken({
      sub: 'user-1',
      name: registerDto.fullName,
      email: registerDto.email,
      role: 'User',
      exp: Math.floor(Date.now() / 1000) + 3600
    });

    service.register(registerDto).subscribe(response => {
      expect(response.token).toBe(token);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      fullName: registerDto.fullName,
      email: registerDto.email,
      password: registerDto.password
    });

    req.flush({ token });

    expect(localStorage.getItem('token')).toBe(token);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
    expect(service.currentUser()?.email).toBe(registerDto.email);
  });

  it('should POST login payload to the backend and hydrate the current user from the token', () => {
    const loginDto = {
      email: 'alex@example.com',
      password: 'Password123!'
    };
    const token = createJwtToken({
      sub: 'user-2',
      name: 'Alex Cojocaru',
      email: loginDto.email,
      role: 'Admin',
      exp: Math.floor(Date.now() / 1000) + 3600
    });

    service.login(loginDto).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      email: loginDto.email,
      password: loginDto.password
    });

    req.flush({ token });

    expect(localStorage.getItem('token')).toBe(token);
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.isAdmin()).toBeTrue();
    expect(service.currentUser()?.id).toBe('user-2');
    expect(service.currentUser()?.fullName).toBe('Alex Cojocaru');
    expect(service.currentUser()?.email).toBe(loginDto.email);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should clear auth state and navigate to login on logout', () => {
    service.logout();

    expect(localStorage.getItem('token')).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentUser()).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  function createJwtToken(payload: Record<string, unknown>): string {
    const header = { alg: 'none', typ: 'JWT' };
    const encode = (value: unknown) => btoa(JSON.stringify(value))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/g, '');

    return `${encode(header)}.${encode(payload)}.signature`;
  }
});