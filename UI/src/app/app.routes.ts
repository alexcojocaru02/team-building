import { Routes } from '@angular/router';
import { FeedbackPage } from './pages/feedback-page/feedback-page';
import { HomePage } from './pages/home-page/home-page';
import { ActivitiesPage } from './pages/activities-page/activities-page';
import { GrowthPage } from './pages/growth-page/growth-page';
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
    path: 'activities',
    component: ActivitiesPage,
    canActivate: [authGuard],
  },
  {
    path: 'growth',
    component: GrowthPage,
    canActivate: [authGuard],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
