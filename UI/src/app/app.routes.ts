import { Routes } from '@angular/router';
import { FeedbackPage } from './pages/feedback-page/feedback-page';
import { HomePage } from './pages/home-page/home-page';
import { FeedPage } from './pages/feed-page/feed-page';
import { CohesionDashboard } from './pages/growth-page/growth-page';
import { LoginPage } from './pages/login-page/login-page';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPage,
  },
  {
    path: '',
    component: HomePage,
    canActivate: [authGuard],
  },
  {
    path: 'feedback',
    component: FeedbackPage,
    canActivate: [authGuard],
  },
  {
    path: 'feed',
    component: FeedPage,
    canActivate: [authGuard],
  },
  {
    path: 'dashboard',
    component: CohesionDashboard,
    canActivate: [authGuard],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
