import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';

export interface OptimizeVideoRequest {
  youTubeVideoId?: string;
  title: string;
  description?: string;
  compositionName?: string;
  mood?: string;
  style?: string;
  tempo?: string;
  keySignature?: string;
  targetAudience?: string;
  storyBehindComposition?: string;
  currentTags?: string[];
  videoUrl?: string;
}

export interface ShortsIdeaDto {
  title: string;
  hook: string;
  suggestedTimestamp: string;
  description: string;
  caption: string;
}

export interface SocialPostsDto {
  instagram: string;
  tikTok: string;
  facebook: string;
  reddit: string;
  x: string;
  linkedIn: string;
  emailNewsletter: string;
}

export interface ComplianceSummaryDto {
  riskLevel: string;
  issues: string[];
  isSafeToUse: boolean;
}

export interface OptimizeVideoResponse {
  titles: string[];
  descriptions: string[];
  tags: string[];
  hashtags: string[];
  thumbnailIdeas: string[];
  shortsIdeas: ShortsIdeaDto[];
  socialPosts: SocialPostsDto;
  compliance: ComplianceSummaryDto;
}

export interface SuggestionDto {
  id: number;
  youTubeVideoId: string;
  suggestionType: string;
  platform?: string;
  suggestionText: string;
  score?: number;
  isApproved: boolean;
  isRejected: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class OptimizationService {
  constructor(private api: ApiClientService) {}

  optimize(request: OptimizeVideoRequest): Observable<OptimizeVideoResponse> {
    return this.api.post<OptimizeVideoResponse>('videos/optimize', request);
  }

  getSuggestions(videoId: string): Observable<SuggestionDto[]> {
    return this.api.get<SuggestionDto[]>(`videos/${videoId}/suggestions`);
  }

  approveSuggestion(videoId: string, suggestionId: number): Observable<SuggestionDto> {
    return this.api.post<SuggestionDto>(`videos/${videoId}/suggestions/${suggestionId}/approve`, {});
  }

  rejectSuggestion(videoId: string, suggestionId: number): Observable<SuggestionDto> {
    return this.api.post<SuggestionDto>(`videos/${videoId}/suggestions/${suggestionId}/reject`, {});
  }
}
