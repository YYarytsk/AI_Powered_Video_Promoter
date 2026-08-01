import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'videos',
    loadComponent: () => import('./components/video-list/video-list.component').then(m => m.VideoListComponent)
  },
  {
    path: 'videos/:id',
    loadComponent: () => import('./components/video-detail/video-detail.component').then(m => m.VideoDetailComponent)
  },
  {
    path: 'videos/:id/optimize',
    loadComponent: () => import('./components/optimize-video/optimize-video.component').then(m => m.OptimizeVideoComponent)
  },
  {
    path: 'videos/:id/suggestions',
    loadComponent: () => import('./components/suggestions/suggestions.component').then(m => m.SuggestionsComponent)
  },
  {
    path: 'videos/:id/promotions',
    loadComponent: () => import('./components/promotion-drafts/promotion-drafts.component').then(m => m.PromotionDraftsComponent)
  },
  {
    path: 'videos/:id/analytics',
    loadComponent: () => import('./components/analytics/analytics.component').then(m => m.AnalyticsComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./components/settings/settings.component').then(m => m.SettingsComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
