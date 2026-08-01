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
      <button class="btn btn-primary" (click)="showAddForm = !showAddForm">
        + Add Video
      </button>
    </div>

    <!-- Add Video Form -->
    <div *ngIf="showAddForm" class="card" style="margin-bottom:1.5rem;">
      <h3 style="margin-bottom:1rem;">Add New Video</h3>
      <div class="form-group">
        <label>YouTube Video ID *</label>
        <input [(ngModel)]="newVideo.youTubeVideoId" placeholder="e.g. dQw4w9WgXcQ">
      </div>
      <div class="form-group">
        <label>Title *</label>
        <input [(ngModel)]="newVideo.title" placeholder="Video title">
      </div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:1rem;">
        <div class="form-group">
          <label>Composition Name</label>
          <input [(ngModel)]="newVideo.compositionName" placeholder="e.g. Moonlit Reverie">
        </div>
        <div class="form-group">
          <label>Mood</label>
          <input [(ngModel)]="newVideo.mood" placeholder="e.g. peaceful, dramatic">
        </div>
      </div>
      <div style="display:flex;gap:1rem;margin-top:0.5rem;">
        <button class="btn btn-primary" (click)="addVideo()" [disabled]="adding">
          {{adding ? 'Adding...' : 'Add Video'}}
        </button>
        <button class="btn btn-secondary" (click)="showAddForm = false">Cancel</button>
      </div>
      <div *ngIf="addError" class="error-banner" style="margin-top:1rem;">{{addError}}</div>
    </div>

    <div *ngIf="loading" class="loading">Loading videos...</div>
    <div *ngIf="error" class="error-banner">{{error}}</div>

    <div *ngIf="!loading && !error">
      <div *ngIf="videos.length === 0" class="empty-state card">
        <div class="empty-icon">🎹</div>
        <p>No videos found. Add a video or sync from YouTube.</p>
      </div>

      <div class="table-wrapper card" *ngIf="videos.length > 0">
        <table>
          <thead>
            <tr>
              <th>Thumbnail</th>
              <th>Title</th>
              <th>Views</th>
              <th>Likes</th>
              <th>Comments</th>
              <th>Published</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let video of videos">
              <td>
                <img [src]="video.thumbnailUrl || 'https://placehold.co/80x45/1a1a2e/ffffff?text=🎹'"
                     [alt]="video.title" style="width:80px;height:45px;object-fit:cover;border-radius:4px;">
              </td>
              <td>
                <div style="font-weight:500;max-width:300px;">{{video.title}}</div>
                <div style="font-size:0.8rem;color:#888;">{{video.compositionName || video.youTubeVideoId}}</div>
              </td>
              <td>{{video.viewCount | number}}</td>
              <td>{{video.likeCount | number}}</td>
              <td>{{video.commentCount | number}}</td>
              <td style="font-size:0.85rem;">{{video.publishedAt | date:'mediumDate'}}</td>
              <td>
                <div style="display:flex;gap:0.5rem;flex-wrap:wrap;">
                  <a [routerLink]="['/videos', video.youTubeVideoId]" class="btn btn-secondary btn-sm">View</a>
                  <a [routerLink]="['/videos', video.youTubeVideoId, 'optimize']" class="btn btn-primary btn-sm">Optimize</a>
                  <a [routerLink]="['/videos', video.youTubeVideoId, 'suggestions']" class="btn btn-secondary btn-sm">Suggestions</a>
                  <a [routerLink]="['/videos', video.youTubeVideoId, 'promotions']" class="btn btn-secondary btn-sm">Promote</a>
                  <a [routerLink]="['/videos', video.youTubeVideoId, 'analytics']" class="btn btn-secondary btn-sm">Analytics</a>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class VideoListComponent implements OnInit {
  videos: VideoDto[] = [];
  loading = true;
  error = '';
  showAddForm = false;
  adding = false;
  addError = '';
  newVideo = { youTubeVideoId: '', title: '', compositionName: '', mood: '' };

  constructor(private videoService: VideoService) {}

  ngOnInit(): void {
    this.videoService.getVideos().subscribe({
      next: vids => { this.videos = vids; this.loading = false; },
      error: err => { this.error = err.message; this.loading = false; }
    });
  }

  addVideo(): void {
    if (!this.newVideo.youTubeVideoId || !this.newVideo.title) {
      this.addError = 'YouTube Video ID and Title are required';
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
      error: err => { this.addError = err.message; this.adding = false; }
    });
  }
}
