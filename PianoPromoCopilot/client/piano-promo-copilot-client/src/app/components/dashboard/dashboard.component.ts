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

    <div class="compliance-notice card" style="margin-bottom:1.5rem; border-left: 4px solid #6c63ff;">
      <strong>✅ Compliance First</strong>
      <p style="margin-top:0.5rem; color:#666; font-size:0.9rem;">
        All suggestions require human review. No fake views, bots, or spam automation.
        This tool assists with organic discovery, metadata quality, and legitimate promotion only.
      </p>
    </div>

    <!-- Channel Card -->
    <div class="card" style="margin-bottom:1.5rem;">
      <h3 style="margin-bottom:1rem;">📺 Channel Status</h3>
      <div *ngIf="channel">
        <div style="display:flex;align-items:center;gap:1rem;">
          <img *ngIf="channel.thumbnailUrl" [src]="channel.thumbnailUrl" alt="Channel thumbnail"
               style="width:56px;height:56px;border-radius:50%;object-fit:cover;">
          <div>
            <div style="font-weight:600;font-size:1.1rem;">{{channel.channelTitle}}</div>
            <div style="font-size:0.85rem; color:#888;">ID: {{channel.channelId}}</div>
            <span class="badge" [class.badge-approved]="channel.isConnected" [class.badge-draft]="!channel.isConnected">
              {{channel.isConnected ? '✓ Connected' : '⚡ Mock Mode'}}
            </span>
          </div>
        </div>
      </div>
      <div *ngIf="channelError" class="error-banner">{{channelError}}</div>
      <div *ngIf="!channel && !channelError" class="loading">Loading channel...</div>
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
    <div class="card" style="margin-bottom:1.5rem;">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:1rem;">
        <h3>🎵 Recent Videos</h3>
        <a routerLink="/videos" class="btn btn-secondary btn-sm">View All</a>
      </div>

      <div *ngIf="loading" class="loading">Loading videos...</div>
      <div *ngIf="error" class="error-banner">{{error}}</div>

      <div *ngIf="!loading && !error">
        <div *ngIf="videos.length === 0" class="empty-state">
          <div class="empty-icon">🎹</div>
          <p>No videos yet. Sync from YouTube or add a video manually.</p>
          <a routerLink="/videos" class="btn btn-primary">Manage Videos</a>
        </div>

        <div *ngFor="let video of recentVideos" class="video-row">
          <img [src]="video.thumbnailUrl || 'https://placehold.co/80x45/1a1a2e/ffffff?text=🎹'"
               [alt]="video.title" class="thumb">
          <div class="video-info">
            <a [routerLink]="['/videos', video.youTubeVideoId]" class="video-title">{{video.title}}</a>
            <div class="video-meta">
              <span>👁 {{video.viewCount | number}}</span>
              <span>👍 {{video.likeCount | number}}</span>
              <span>💬 {{video.commentCount | number}}</span>
            </div>
          </div>
          <div class="video-actions">
            <a [routerLink]="['/videos', video.youTubeVideoId, 'optimize']" class="btn btn-primary btn-sm">Optimize</a>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Actions -->
    <div class="card">
      <h3 style="margin-bottom:1rem;">⚡ Quick Actions</h3>
      <div style="display:flex;gap:1rem;flex-wrap:wrap;">
        <a routerLink="/videos" class="btn btn-primary">📹 View Videos</a>
        <a routerLink="/videos/mock_video_001/optimize" class="btn btn-secondary">🤖 Try Optimizer</a>
        <a routerLink="/videos/mock_video_001/analytics" class="btn btn-secondary">📊 View Analytics</a>
        <a routerLink="/settings" class="btn btn-secondary">⚙️ Settings</a>
      </div>
    </div>
  `,
  styles: [`
    .video-row {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid #f0f2f5;
      &:last-child { border-bottom: none; }
    }
    .thumb {
      width: 80px;
      height: 45px;
      object-fit: cover;
      border-radius: 6px;
      flex-shrink: 0;
    }
    .video-info { flex: 1; min-width: 0; }
    .video-title {
      font-weight: 500;
      color: #1a1a2e;
      text-decoration: none;
      display: block;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      &:hover { color: #6c63ff; }
    }
    .video-meta {
      font-size: 0.8rem;
      color: #888;
      margin-top: 0.25rem;
      display: flex;
      gap: 0.75rem;
    }
    .video-actions { flex-shrink: 0; }
  `]
})
export class DashboardComponent implements OnInit {
  channel: YouTubeChannelDto | null = null;
  videos: VideoDto[] = [];
  loading = true;
  error = '';
  channelError = '';

  get recentVideos() { return this.videos.slice(0, 5); }
  get totalViews() { return this.videos.reduce((s, v) => s + (v.viewCount || 0), 0); }
  get totalLikes() { return this.videos.reduce((s, v) => s + (v.likeCount || 0), 0); }
  get totalComments() { return this.videos.reduce((s, v) => s + (v.commentCount || 0), 0); }

  constructor(private videoService: VideoService, private youtubeService: YouTubeService) {}

  ngOnInit(): void {
    this.youtubeService.getChannel().subscribe({
      next: ch => this.channel = ch,
      error: err => this.channelError = err.message
    });

    this.videoService.getVideos().subscribe({
      next: vids => { this.videos = vids; this.loading = false; },
      error: err => { this.error = err.message; this.loading = false; }
    });
  }
}
