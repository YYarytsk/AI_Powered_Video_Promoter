import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { VideoService, VideoDto } from '../../services/video.service';
import { VideoWorkflowNavComponent } from '../shared/video-workflow-nav.component';

@Component({
  selector: 'app-video-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, VideoWorkflowNavComponent],
  template: `
    <app-video-workflow-nav [videoId]="videoId" />

    <div *ngIf="loading" class="loading" role="status">Loading video…</div>

    <div *ngIf="error" class="error-banner" role="alert">
      <span>{{error}}</span>
      <span class="toolbar">
        <button type="button" class="btn btn-secondary btn-sm" (click)="load()">Retry</button>
        <a routerLink="/videos" class="btn btn-secondary btn-sm">All videos</a>
      </span>
    </div>

    <div *ngIf="video">
      <div class="page-header">
        <div>
          <h1>{{video.title}}</h1>
          <p *ngIf="video.compositionName">{{video.compositionName}}</p>
        </div>
        <a [routerLink]="['/videos', video.youTubeVideoId, 'optimize']" class="btn btn-primary">🤖 Optimize this video</a>
      </div>

      <div class="notice">
        <span class="notice-icon" aria-hidden="true">🛡️</span>
        <div>
          <div class="notice-title">You are always the one who publishes</div>
          <p class="notice-body">
            Optimize generates suggestions, you approve the ones you like, then you copy them into YouTube
            and your social apps yourself. This tool never changes your channel.
          </p>
        </div>
      </div>

      <div class="detail-grid">
        <div>
          <img [src]="video.thumbnailUrl || placeholder"
               [alt]="'Thumbnail for ' + video.title" class="detail-thumb">
          <div class="stats-grid detail-stats">
            <div class="stat-card">
              <div class="stat-value">{{video.viewCount | number}}</div>
              <div class="stat-label">Views</div>
            </div>
            <div class="stat-card">
              <div class="stat-value">{{video.likeCount | number}}</div>
              <div class="stat-label">Likes</div>
            </div>
            <div class="stat-card">
              <div class="stat-value">{{video.commentCount | number}}</div>
              <div class="stat-label">Comments</div>
            </div>
          </div>
        </div>

        <div class="card">
          <h2 class="card-title">Video details</h2>
          <dl class="kv-list">
            <dt>YouTube ID</dt><dd><code>{{video.youTubeVideoId}}</code></dd>
            <dt>Composition</dt><dd>{{video.compositionName || '—'}}</dd>
            <dt>Mood</dt><dd>{{video.mood || '—'}}</dd>
            <dt>Style</dt><dd>{{video.style || '—'}}</dd>
            <dt>Target audience</dt><dd>{{video.targetAudience || '—'}}</dd>
            <dt>Duration</dt><dd>{{video.durationIso8601 || '—'}}</dd>
            <dt>Privacy</dt><dd>{{video.privacyStatus || '—'}}</dd>
            <dt>Published</dt><dd>{{(video.publishedAt | date:'mediumDate') || '—'}}</dd>
          </dl>

          <div *ngIf="video.description" class="description">
            <h3 class="description-title">Description</h3>
            <p class="description-body">{{video.description}}</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .detail-grid { display: grid; grid-template-columns: 300px 1fr; gap: 1.5rem; align-items: start; }
    .detail-thumb { width: 100%; border-radius: 10px; display: block; background: #e6e6ee; }
    .detail-stats { margin-top: 1rem; margin-bottom: 0; }
    .card-title { font-size: 1.15rem; margin-bottom: 1rem; }
    .description { margin-top: 1.25rem; }
    .description-title { font-size: 1rem; margin-bottom: 0.5rem; }
    .description-body { color: #4a4f5a; font-size: 0.9rem; white-space: pre-line; overflow-wrap: anywhere; }
    @media (max-width: 768px) {
      .detail-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class VideoDetailComponent implements OnInit {
  videoId = '';
  video: VideoDto | null = null;
  loading = true;
  error = '';
  readonly placeholder = 'https://placehold.co/480x270/1a1a2e/ffffff?text=Piano';

  constructor(private route: ActivatedRoute, private videoService: VideoService) {}

  ngOnInit(): void {
    this.videoId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.videoService.getVideo(this.videoId).subscribe({
      next: v => { this.video = v; this.loading = false; },
      error: err => { this.error = this.friendlyError(err); this.loading = false; }
    });
  }

  private friendlyError(err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.status === 404) return `No video with the id "${this.videoId}" is stored yet.`;
    if (e?.status === 0) return 'Cannot reach the API. Is the backend running on http://localhost:5000?';
    return e?.error?.message || e?.message || 'Something went wrong loading this video.';
  }
}
