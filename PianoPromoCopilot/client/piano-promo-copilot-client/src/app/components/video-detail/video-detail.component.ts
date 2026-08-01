import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { VideoService, VideoDto } from '../../services/video.service';

@Component({
  selector: 'app-video-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div *ngIf="loading" class="loading">Loading video...</div>
    <div *ngIf="error" class="error-banner">{{error}}</div>

    <div *ngIf="video">
      <div class="page-header">
        <div>
          <a routerLink="/videos" style="color:#6c63ff; text-decoration:none; font-size:0.9rem;">← Back to Videos</a>
          <h1 style="margin-top:0.5rem;">{{video.title}}</h1>
        </div>
        <div style="display:flex;gap:0.75rem;flex-wrap:wrap;">
          <a [routerLink]="['/videos', video.youTubeVideoId, 'optimize']" class="btn btn-primary">🤖 Optimize</a>
          <a [routerLink]="['/videos', video.youTubeVideoId, 'suggestions']" class="btn btn-secondary">✅ Suggestions</a>
          <a [routerLink]="['/videos', video.youTubeVideoId, 'promotions']" class="btn btn-secondary">📢 Promote</a>
          <a [routerLink]="['/videos', video.youTubeVideoId, 'analytics']" class="btn btn-secondary">📊 Analytics</a>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:300px 1fr;gap:1.5rem;align-items:start;" class="detail-grid">
        <div>
          <img [src]="video.thumbnailUrl || 'https://placehold.co/300x168/1a1a2e/ffffff?text=🎹'"
               [alt]="video.title" style="width:100%;border-radius:10px;">
          <div class="stats-grid" style="margin-top:1rem;">
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
          <h3 style="margin-bottom:1rem;">Video Details</h3>
          <table style="width:100%;">
            <tr><td style="color:#888;padding:0.5rem 0;width:160px;">YouTube ID</td><td><code>{{video.youTubeVideoId}}</code></td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Composition</td><td>{{video.compositionName || '—'}}</td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Mood</td><td>{{video.mood || '—'}}</td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Style</td><td>{{video.style || '—'}}</td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Target Audience</td><td>{{video.targetAudience || '—'}}</td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Duration</td><td>{{video.durationIso8601 || '—'}}</td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Privacy</td><td>{{video.privacyStatus || '—'}}</td></tr>
            <tr><td style="color:#888;padding:0.5rem 0;">Published</td><td>{{video.publishedAt | date:'mediumDate'}}</td></tr>
          </table>

          <div *ngIf="video.description" style="margin-top:1rem;">
            <h4 style="margin-bottom:0.5rem;">Description</h4>
            <p style="color:#555;font-size:0.9rem;white-space:pre-line;">{{video.description}}</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    @media (max-width: 768px) {
      .detail-grid { grid-template-columns: 1fr !important; }
    }
  `]
})
export class VideoDetailComponent implements OnInit {
  video: VideoDto | null = null;
  loading = true;
  error = '';

  constructor(private route: ActivatedRoute, private videoService: VideoService) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.videoService.getVideo(id).subscribe({
      next: v => { this.video = v; this.loading = false; },
      error: err => { this.error = err.message; this.loading = false; }
    });
  }
}
