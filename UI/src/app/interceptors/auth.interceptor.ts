import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  const isAuthRequest = req.url.includes('/auth/login') || req.url.includes('/auth/register');

  if (token) {
    const clonedRequest = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });

    return next(clonedRequest).pipe(
      catchError((error) => {
        if (error?.status === 401 && !isAuthRequest) {
          authService.logout();
        }
        return throwError(() => error);
      })
    );
  }

  return next(req).pipe(
    catchError((error) => {
      if (error?.status === 401 && !isAuthRequest) {
        authService.logout();
      }
      return throwError(() => error);
    })
  );
};
