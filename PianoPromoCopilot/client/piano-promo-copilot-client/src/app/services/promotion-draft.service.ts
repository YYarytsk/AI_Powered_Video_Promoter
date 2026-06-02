import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from './api-client.service';

export interface PromotionDraftDto {
  id: number;
  youTubeVideoId: string;
  platform: string;
  draftText: string;
  status: string;
  scheduledFor?: string;
  postedAt?: string;
  createdAt: string;
}

export interface UpdatePromotionDraftRequest {
  draftText?: string;
  status?: string;
  scheduledFor?: string;
}

@Injectable({ providedIn: 'root' })
export class PromotionDraftService {
  constructor(private api: ApiClientService) {}

  getDrafts(videoId: string): Observable<PromotionDraftDto[]> {
    return this.api.get<PromotionDraftDto[]>(`videos/${videoId}/promotion-drafts`);
  }

  generateDrafts(videoId: string): Observable<PromotionDraftDto[]> {
    return this.api.post<PromotionDraftDto[]>(`videos/${videoId}/promotion-drafts`, {});
  }

  updateDraft(id: number, request: UpdatePromotionDraftRequest): Observable<PromotionDraftDto> {
    return this.api.put<PromotionDraftDto>(`promotion-drafts/${id}`, request);
  }
}
