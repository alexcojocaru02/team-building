import { Routes } from '@angular/router';
import { FeedbackPage } from './pages/feedback-page/feedback-page';
import { HomePage } from './pages/home-page/home-page';
import { ActivitiesPage } from './pages/activities-page/activities-page';
import { GrowthPage } from './pages/growth-page/growth-page';

export const routes: Routes = [
  {
    path: '',
    component: HomePage,
  },
  {
    path: 'feedback',
    component: FeedbackPage,
  },
  {
    path: 'activities',
    component: ActivitiesPage
  },
  {
    path: 'growth',
    component: GrowthPage
  },
  {
    path: '**',
    redirectTo: '',
  },
];
