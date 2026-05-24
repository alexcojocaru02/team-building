import { Injectable, inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export function roleGuard(allowedRoles: string[]): CanActivateFn {
  return (route: ActivatedRouteSnapshot) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const currentRole = authService.currentUserRole();

    if (!currentRole || !allowedRoles.includes(currentRole)) {
      // Redirect to home or login if role not permitted
      router.navigate(['/']);
      return false;
    }

    return true;
  };
}

@Injectable({
  providedIn: 'root'
})
export class RoleGuardService {
  private authService = inject(AuthService);
  private router = inject(Router);

  canActivateAdmin(): boolean {
    if (this.authService.isAdmin()) {
      return true;
    }
    this.router.navigate(['/']);
    return false;
  }

  canActivateAdminOrTeamOwner(): boolean {
    const role = this.authService.currentUserRole();
    if (role === 'Admin' || role === 'TeamOwner') {
      return true;
    }
    this.router.navigate(['/']);
    return false;
  }
}
