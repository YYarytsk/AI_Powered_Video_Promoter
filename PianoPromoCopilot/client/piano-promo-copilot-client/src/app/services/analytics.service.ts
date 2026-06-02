import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';

export interface AnalyticsSnapshotDto {
  id: number;
  youTubeVideoId: string;
  snapshotDate: string;
  views?: number;
  likes?: number;
  comments?: number;
  subscribersGained?: number;
  estimatedMinutesWatched?: number;
  averageViewDurationSeconds?: number;
  averageViewPercentage?: number;
  impressions?: number;
  impressionClickThroughRate?: number;
  trafficSource?: string;
  country?: string;
  createdAt: string;
}

export interface AnalyticsRecommendationDto {
  recommendationType: string;
  message: string;
  reason: string;
  suggestedAction: string;
  priority: number;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  constructor(private api: ApiClientService) {}

  getAnalytics(videoId: string): Observable<AnalyticsSnapshotDto[]> {
    return this.api.get<AnalyticsSnapshotDto[]>(`videos/${videoId}/analytics`);
  }

  createMockAnalytics(videoId: string): Observable<AnalyticsSnapshotDto> {
    return this.api.post<AnalyticsSnapshotDto>(`videos/${videoId}/analytics/mock`, {});
  }

  getRecommendations(videoId: string): Observable<AnalyticsRecommendationDto[]> {
    return this.api.post<AnalyticsRecommendationDto[]>(`videos/${videoId}/recommendations`, {});
  }
}
