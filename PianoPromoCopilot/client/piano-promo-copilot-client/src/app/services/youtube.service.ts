import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';

export interface YouTubeChannelDto {
  channelId: string;
  channelTitle: string;
  description?: string;
  thumbnailUrl?: string;
  isConnected: boolean;
}

export interface SettingsDto {
  useMockYouTube: boolean;
  enableYouTubeWriteActions: boolean;
  useMockAnalytics: boolean;
  openAiConfigured: boolean;
}

@Injectable({ providedIn: 'root' })
export class YouTubeService {
  constructor(private api: ApiClientService) {}

  getChannel(): Observable<YouTubeChannelDto> {
    return this.api.get<YouTubeChannelDto>('youtube/channel');
  }

  getVideos(): Observable<any[]> {
    return this.api.get<any[]>('youtube/videos');
  }

  sync(): Observable<any> {
    return this.api.post<any>('youtube/sync', {});
  }
}
