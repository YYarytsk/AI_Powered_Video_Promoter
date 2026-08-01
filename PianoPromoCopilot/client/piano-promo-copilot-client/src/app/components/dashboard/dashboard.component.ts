import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VideoService, VideoDto } from '../../services/video.service';
import { YouTubeService, YouTubeChannelDto } from '../../services/youtube.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page-header">
      <div>
        <h1>🎹 PianoPromoCopilot</h1>
        <p>YouTube growth copilot for independent pianists</p>
      </div>
    </div>

    <div class="notice">
      <span class="notice-icon" aria-hidden="true">🛡️</span>
      <div>
        <div class="notice-title">Compliance first</div>
        <p class="notice-body">
          Every suggestion is reviewed by you before it goes anywhere. No fake views, no bots, no spam
          automation — this tool helps with organic discovery, metadata quality and honest promotion only.
        </p>
      </div>
    </div>

    <!-- Channel Card -->
    <div class="card channel-card">
      <h2 class="card-title">📺 Channel status</h2>
      <div *ngIf="channel" class="channel-row">
        <img *ngIf="channel.thumbnailUrl" [src]="channel.thumbnailUrl"
             [alt]="channel.channelTitle + ' channel avatar'" class="channel-avatar">
        <div>
          <div class="channel-name">{{channel.channelTitle}}</div>
          <div class="text-meta">ID: {{channel.channelId}}</div>
          <span class="badge" [ngClass]="channel.isConnected ? 'badge-approved' : 'badge-draft'">
            {{channel.isConnected ? '✓ Connected' : '⚡ Mock mode'}}
          </span>
        </div>
      </div>
      <div *ngIf="channelError" class="error-banner channel-error" role="alert">
        <span>{{channelError}}</span>
        <button type="button" class="btn btn-secondary btn-sm" (click)="loadChannel()">Retry</button>
      </div>
      <div *ngIf="!channel && !channelError" class="loading loading-inline" role="status">Loading channel…</div>
    </div>

    <!-- Stats -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">{{videos.length}}</div>
        <div class="stat-label">Videos</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{totalViews | number}}</div>
        <div class="stat-label">Total Views</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{totalLikes | number}}</div>
        <div class="stat-label">Total Likes</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{totalComments | number}}</div>
        <div class="stat-label">Comments</div>
      </div>
    </div>

    <!-- Recent Videos -->
    <div class="card recent-card">
      <div class="section-head">
        <h2 class="card-title">🎵 Recent videos</h2>
        <a routerLink="/videos" class="btn btn-secondary btn-sm">View all</a>
      </div>

      <div *ngIf="loading" class="loading loading-inline" role="status">Loading videos…</div>
      <div *ngIf="error" class="error-banner" role="alert">
        <span>{{error}}</span>
        <button type="button" class="btn btn-secondary btn-sm" (click)="loadVideos()">Retry</button>
      </div>

      <div *ngIf="!loading && !error">
        <div *ngIf="videos.length === 0" class="empty-state">
          <div class="empty-icon" aria-hidden="true">🎹</div>
          <p>No videos yet. Add one by its YouTube ID and the optimizer can start work on it.</p>
          <a routerLink="/videos" class="btn btn-primary">Add your first video</a>
        </div>

        <div *ngFor="let video of recentVideos; trackBy: trackVideo" class="video-row">
          <img [src]="video.thumbnailUrl || placeholder"
               [alt]="'Thumbnail for ' + video.title" class="thumb">
          <div class="video-info">
            <a [routerLink]="['/videos', video.youTubeVideoId]" class="video-title">{{video.title}}</a>
            <div class="video-meta">
              <span><span aria-hidden="true">👁</span> {{video.viewCount | number}} views</span>
              <span><span aria-hidden="true">👍</span> {{video.likeCount | number}} likes</span>
              <span><span aria-hidden="true">💬</span> {{video.commentCount | number}} comments</span>
            </div>
          </div>
          <div class="video-actions">
            <a [routerLink]="['/videos', video.youTubeVideoId, 'optimize']" class="btn btn-primary btn-sm"
               [attr.aria-label]="'Optimize ' + video.title">Optimize</a>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="card">
      <h2 class="card-title">⚡ Quick actions</h2>
      <div class="toolbar">
        <a routerLink="/videos" class="btn btn-primary">📹 View videos</a>
        <!-- These only appear once there is a real video to point them at;
             previously they linked to a hardcoded mock id that may not exist. -->
        <a *ngIf="firstVideoId" [routerLink]="['/videos', firstVideoId, 'optimize']" class="btn btn-secondary">
          🤖 Optimize “{{firstVideoTitle}}”
        </a>
        <a *ngIf="firstVideoId" [routerLink]="['/videos', firstVideoId, 'analytics']" class="btn btn-secondary">
          📊 View analytics
        </a>
        <a routerLink="/settings" class="btn btn-secondary">⚙️ Settings</a>
      </div>
    </div>
  `,
  styles: [`
    .card-title { font-size: 1.15rem; }
    .channel-card, .recent-card { margin-bottom: 1.5rem; }
    .channel-card .card-title { margin-bottom: 1rem; }
    .channel-row { display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .channel-avatar { width: 56px; height: 56px; border-radius: 50%; object-fit: cover; background: #e6e6ee; }
    .channel-name { font-weight: 600; font-size: 1.1rem; }
    .channel-error { margin-bottom: 0; }
    .section-head {
      display: flex; justify-content: space-between; align-items: center;
      gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap;
    }
    .section-head .card-title { margin-bottom: 0; }
    .video-row {
      display: flex; align-items: center; gap: 1rem;
      padding: 0.75rem 0; border-bottom: 1px solid #f0f2f5;
    }
    .video-row:last-child { border-bottom: none; }
    .thumb { width: 80px; height: 45px; object-fit: cover; border-radius: 6px; flex-shrink: 0; background: #e6e6ee; }
    .video-info { flex: 1; min-width: 0; }
    .video-title {
      font-weight: 500; color: #1a1a2e; text-decoration: none; display: block;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .video-title:hover { color: #4338ca; text-decoration: underline; }
    .video-meta {
      font-size: 0.8rem; color: #62687a; margin-top: 0.25rem;
      display: flex; gap: 0.75rem; flex-wrap: wrap;
    }
    .video-actions { flex-shrink: 0; }
    @media (max-width: 560px) {
      .video-row { flex-wrap: wrap; }
      .video-actions { width: 100%; }
    }
  `]
})
export class DashboardComponent implements OnInit {
  channel: YouTubeChannelDto | null = null;
  videos: VideoDto[] = [];
  loading = true;
  error = '';
  channelError = '';
  readonly placeholder = 'https://placehold.co/160x90/1a1a2e/ffffff?text=Piano';

  get recentVideos() { return this.videos.slice(0, 5); }
  get totalViews() { return this.videos.reduce((s, v) => s + (v.viewCount || 0), 0); }
  get totalLikes() { return this.videos.reduce((s, v) => s + (v.likeCount || 0), 0); }
  get totalComments() { return this.videos.reduce((s, v) => s + (v.commentCount || 0), 0); }
  get firstVideoId() { return this.videos[0]?.youTubeVideoId ?? ''; }
  get firstVideoTitle() {
    const t = this.videos[0]?.title ?? '';
    return t.length > 28 ? `${t.slice(0, 28)}…` : t;
  }

  constructor(private videoService: VideoService, private youtubeService: YouTubeService) {}

  ngOnInit(): void {
    this.loadChannel();
    this.loadVideos();
  }

  loadChannel(): void {
    this.channelError = '';
    this.youtubeService.getChannel().subscribe({
      next: ch => this.channel = ch,
      error: err => this.channelError = this.friendlyError(err, 'load your channel')
    });
  }

  loadVideos(): void {
    this.loading = true;
    this.error = '';
    this.videoService.getVideos().subscribe({
      next: vids => { this.videos = vids; this.loading = false; },
      error: err => { this.error = this.friendlyError(err, 'load your videos'); this.loading = false; }
    });
  }

  trackVideo = (_: number, v: VideoDto) => v.youTubeVideoId;

  private friendlyError(err: unknown, what: string): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.status === 0) return `Could not ${what}. Cannot reach the API — is the backend running on http://localhost:5000?`;
    return `Could not ${what}. ${e?.error?.message || e?.message || 'Something went wrong.'}`;
  }
}
