import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';

export interface VideoDto {
  id: number;
  youTubeVideoId: string;
  title: string;
  description?: string;
  publishedAt?: string;
  thumbnailUrl?: string;
  durationIso8601?: string;
  privacyStatus?: string;
  viewCount?: number;
  likeCount?: number;
  commentCount?: number;
  compositionName?: string;
  mood?: string;
  style?: string;
  targetAudience?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateVideoRequest {
  youTubeVideoId: string;
  title: string;
  description?: string;
  publishedAt?: string;
  thumbnailUrl?: string;
  compositionName?: string;
  mood?: string;
  style?: string;
  targetAudience?: string;
}

export interface UpdateVideoRequest {
  title?: string;
  description?: string;
  compositionName?: string;
  mood?: string;
  style?: string;
  targetAudience?: string;
}

@Injectable({ providedIn: 'root' })
export class VideoService {
  constructor(private api: ApiClientService) {}

  getVideos(): Observable<VideoDto[]> {
    return this.api.get<VideoDto[]>('videos');
  }

  getVideo(id: string): Observable<VideoDto> {
    return this.api.get<VideoDto>(`videos/${id}`);
  }

  createVideo(request: CreateVideoRequest): Observable<VideoDto> {
    return this.api.post<VideoDto>('videos', request);
  }

  updateVideo(id: string, request: UpdateVideoRequest): Observable<VideoDto> {
    return this.api.put<VideoDto>(`videos/${id}`, request);
  }
}
