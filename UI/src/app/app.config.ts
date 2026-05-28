import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';

export function createAppConfig(): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      (typeof Zone === 'undefined' ? provideZonelessChangeDetection() : provideZoneChangeDetection()),
      provideAnimationsAsync(),
      provideRouter(routes),
      provideHttpClient(withInterceptors([authInterceptor]))
    ]
  };
}
