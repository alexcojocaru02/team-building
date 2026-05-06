import { Routes } from '@angular/router';
import { FeedbackPage } from './pages/feedback-page/feedback-page';
import { HomePage } from './pages/home-page/home-page';
import { FeedPage } from './pages/feed-page/feed-page';
import { CohesionDashboard } from './pages/growth-page/growth-page';
import { LoginPage } from './pages/login-page/login-page';
import { authGuard } from './guards/auth.guard';
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
      { path: 'dashboard', component: CohesionDashboard, canActivate: [authGuard] },
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
