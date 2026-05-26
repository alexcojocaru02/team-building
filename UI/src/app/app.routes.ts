import { Routes } from '@angular/router';
import { FeedbackPage } from './pages/feedback-page/feedback-page';
import { HomePage } from './pages/home-page/home-page';
import { FeedPage } from './pages/feed-page/feed-page';
import { CohesionDashboard } from './pages/growth-page/growth-page';
import { LoginPage } from './pages/login-page/login-page';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { MainLayoutComponent } from './layouts/main-layout.component';
import { AuthLayoutComponent } from './layouts/auth-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomePage, canActivate: [authGuard] },
      { path: 'feedback', component: FeedbackPage, canActivate: [authGuard] },
      { path: 'feed', component: FeedPage, canActivate: [authGuard] },
      { path: 'team-activities', loadComponent: () => import('./pages/team-activities-page/team-activities-page').then(m => m.TeamActivitiesPage), canActivate: [authGuard] },
      { path: 'dashboard', component: CohesionDashboard, canActivate: [authGuard] },
      // Profile routes
      { path: 'profile', loadComponent: () => import('./pages/profile-page/profile-view.component').then(m => m.ProfileViewComponent), canActivate: [authGuard] },
      { path: 'profile/edit', loadComponent: () => import('./pages/profile-page/profile-edit.component').then(m => m.ProfileEditComponent), canActivate: [authGuard] },
      // Team routes
      { path: 'teams', loadComponent: () => import('./pages/teams-page/teams-list.component').then(m => m.TeamsListComponent), canActivate: [authGuard] },
      // Admin routes
      { path: 'admin', loadComponent: () => import('./pages/admin-page/admin-dashboard.component').then(m => m.AdminDashboardComponent), canActivate: [authGuard, roleGuard(['Admin'])] },
    ]
  },
  {
    path: 'login',
    component: AuthLayoutComponent,
    children: [
      { path: '', component: LoginPage },
    ]
  },
  { path: '**', redirectTo: 'home' },
];
