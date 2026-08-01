import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VideoService, VideoDto } from '../../services/video.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-video-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h1>📹 Videos</h1>
        <p>Manage and optimize your piano compositions</p>
      </div>
      <button type="button" class="btn btn-primary" (click)="toggleAddForm()"
              [attr.aria-expanded]="showAddForm" aria-controls="add-video-form">
        + Add Video
      </button>
    </div>

    <!-- Add Video Form -->
    <div *ngIf="showAddForm" id="add-video-form" class="card add-form">
      <h2 class="card-title">Add a new video</h2>
      <div class="form-group">
        <label for="new-id">YouTube video ID <span class="req" aria-hidden="true">*</span></label>
        <input id="new-id" [(ngModel)]="newVideo.youTubeVideoId" placeholder="e.g. dQw4w9WgXcQ" required>
      </div>
      <div class="form-group">
        <label for="new-title">Title <span class="req" aria-hidden="true">*</span></label>
        <input id="new-title" [(ngModel)]="newVideo.title" placeholder="Video title" required>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label for="new-composition">Composition name</label>
          <input id="new-composition" [(ngModel)]="newVideo.compositionName" placeholder="e.g. Moonlit Reverie">
        </div>
        <div class="form-group">
          <label for="new-mood">Mood</label>
          <input id="new-mood" [(ngModel)]="newVideo.mood" placeholder="e.g. peaceful, dramatic">
        </div>
      </div>
      <div class="toolbar">
        <button type="button" class="btn btn-primary" (click)="addVideo()" [disabled]="adding">
          {{adding ? '⏳ Adding…' : 'Add video'}}
        </button>
        <button type="button" class="btn btn-secondary" (click)="toggleAddForm()">Cancel</button>
      </div>
      <div *ngIf="addError" class="error-banner form-error" role="alert">{{addError}}</div>
    </div>

    <div *ngIf="loading" class="loading" role="status">Loading videos…</div>

    <div *ngIf="error" class="error-banner" role="alert">
      <span>{{error}}</span>
      <button type="button" class="btn btn-secondary btn-sm" (click)="load()">Retry</button>
    </div>

    <div *ngIf="!loading && !error">
      <div *ngIf="videos.length === 0" class="empty-state card">
        <div class="empty-icon" aria-hidden="true">🎹</div>
        <p>No videos yet. Add one by its YouTube ID and the optimizer can start work on it.</p>
        <button type="button" class="btn btn-primary" (click)="toggleAddForm()">+ Add your first video</button>
      </div>

      <ng-container *ngIf="videos.length > 0">
        <div class="form-group search" *ngIf="videos.length > 5">
          <label for="video-search">Find a video</label>
          <input id="video-search" type="search" [(ngModel)]="query"
                 placeholder="Search by title, composition or ID">
        </div>

        <div *ngIf="filteredVideos.length === 0" class="empty-state card">
          <div class="empty-icon" aria-hidden="true">🔍</div>
          <p>No video matches “{{query}}”.</p>
          <button type="button" class="btn btn-secondary" (click)="query = ''">Clear search</button>
        </div>

        <div class="table-wrapper card" *ngIf="filteredVideos.length > 0">
          <table>
            <caption class="sr-only">Your videos, with view counts and per-video actions</caption>
            <thead>
              <tr>
                <th scope="col"><span class="sr-only">Thumbnail</span></th>
                <th scope="col">Title</th>
                <th scope="col" class="num">Views</th>
                <th scope="col" class="num">Likes</th>
                <th scope="col" class="num">Comments</th>
                <th scope="col">Published</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let video of filteredVideos; trackBy: trackVideo">
                <td>
                  <img [src]="video.thumbnailUrl || placeholder"
                       [alt]="'Thumbnail for ' + video.title" class="row-thumb">
                </td>
                <td>
                  <a [routerLink]="['/videos', video.youTubeVideoId]" class="row-title">{{video.title}}</a>
                  <div class="text-meta">{{video.compositionName || video.youTubeVideoId}}</div>
                </td>
                <td class="num">{{video.viewCount | number}}</td>
                <td class="num">{{video.likeCount | number}}</td>
                <td class="num">{{video.commentCount | number}}</td>
                <td class="published">{{(video.publishedAt | date:'mediumDate') || '—'}}</td>
                <td>
                  <div class="row-actions">
                    <a [routerLink]="['/videos', video.youTubeVideoId]" class="btn btn-secondary btn-sm"
                       [attr.aria-label]="'View ' + video.title">View</a>
                    <a [routerLink]="['/videos', video.youTubeVideoId, 'optimize']" class="btn btn-primary btn-sm"
                       [attr.aria-label]="'Optimize ' + video.title">Optimize</a>
                    <a [routerLink]="['/videos', video.youTubeVideoId, 'suggestions']" class="btn btn-secondary btn-sm"
                       [attr.aria-label]="'Review suggestions for ' + video.title">Suggestions</a>
                    <a [routerLink]="['/videos', video.youTubeVideoId, 'promotions']" class="btn btn-secondary btn-sm"
                       [attr.aria-label]="'Promotion drafts for ' + video.title">Promote</a>
                    <a [routerLink]="['/videos', video.youTubeVideoId, 'analytics']" class="btn btn-secondary btn-sm"
                       [attr.aria-label]="'Analytics for ' + video.title">Analytics</a>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </ng-container>
    </div>
  `,
  styles: [`
    .add-form { margin-bottom: 1.5rem; }
    .card-title { font-size: 1.15rem; margin-bottom: 1rem; }
    .req { color: #b02a37; }
    .form-error { margin-top: 1rem; margin-bottom: 0; }
    .search { max-width: 26rem; }
    .row-thumb { width: 80px; height: 45px; object-fit: cover; border-radius: 4px; display: block; background: #e6e6ee; }
    .row-title { font-weight: 500; color: #1a1a2e; text-decoration: none; display: block; max-width: 22rem; }
    .row-title:hover { color: #4338ca; text-decoration: underline; }
    .num { text-align: right; white-space: nowrap; }
    .published { font-size: 0.85rem; white-space: nowrap; }
    .row-actions { display: flex; gap: 0.4rem; flex-wrap: wrap; min-width: 12rem; }
  `]
})
export class VideoListComponent implements OnInit {
  videos: VideoDto[] = [];
  loading = true;
  error = '';
  showAddForm = false;
  adding = false;
  addError = '';
  query = '';
  newVideo = { youTubeVideoId: '', title: '', compositionName: '', mood: '' };
  readonly placeholder = 'https://placehold.co/160x90/1a1a2e/ffffff?text=Piano';

  constructor(private videoService: VideoService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.videoService.getVideos().subscribe({
      next: vids => { this.videos = vids; this.loading = false; },
      error: err => { this.error = this.friendlyError(err); this.loading = false; }
    });
  }

  get filteredVideos(): VideoDto[] {
    const q = this.query.trim().toLowerCase();
    if (!q) return this.videos;
    return this.videos.filter(v =>
      `${v.title} ${v.compositionName ?? ''} ${v.youTubeVideoId}`.toLowerCase().includes(q)
    );
  }

  toggleAddForm(): void {
    this.showAddForm = !this.showAddForm;
    if (!this.showAddForm) this.addError = '';
  }

  addVideo(): void {
    if (!this.newVideo.youTubeVideoId.trim() || !this.newVideo.title.trim()) {
      this.addError = 'A YouTube video ID and a title are both required.';
      return;
    }
    this.adding = true;
    this.addError = '';
    this.videoService.createVideo(this.newVideo).subscribe({
      next: v => {
        this.videos.unshift(v);
        this.showAddForm = false;
        this.adding = false;
        this.newVideo = { youTubeVideoId: '', title: '', compositionName: '', mood: '' };
      },
      error: err => { this.addError = this.friendlyError(err); this.adding = false; }
    });
  }

  trackVideo = (_: number, v: VideoDto) => v.youTubeVideoId;

  private friendlyError(err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.status === 0) return 'Cannot reach the API. Is the backend running on http://localhost:5000?';
    if (e?.status === 409) return 'That video is already in your list.';
    return e?.error?.message || e?.message || 'Something went wrong.';
  }
}
