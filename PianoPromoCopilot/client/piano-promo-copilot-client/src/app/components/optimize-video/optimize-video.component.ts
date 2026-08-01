import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { OptimizationService, OptimizeVideoRequest, OptimizeVideoResponse } from '../../services/optimization.service';
import { VideoService } from '../../services/video.service';

@Component({
  selector: 'app-optimize-video',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="page-header">
      <div>
        <a [routerLink]="['/videos', videoId]" style="color:#6c63ff;text-decoration:none;font-size:0.9rem;">← Back to Video</a>
        <h1 style="margin-top:0.5rem;">🤖 AI Optimizer</h1>
        <p>Generate optimized metadata and promotion content</p>
      </div>
    </div>

    <div style="display:grid;grid-template-columns:1fr 1fr;gap:1.5rem;" class="opt-grid">
      <!-- Form -->
      <div class="card">
        <h3 style="margin-bottom:1rem;">Video Information</h3>
        <div class="form-group">
          <label>Title *</label>
          <input [(ngModel)]="request.title" placeholder="Your video title">
        </div>
        <div class="form-group">
          <label>Composition Name</label>
          <input [(ngModel)]="request.compositionName" placeholder="e.g. Moonlit Reverie">
        </div>
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:0.75rem;">
          <div class="form-group">
            <label>Mood</label>
            <input [(ngModel)]="request.mood" placeholder="e.g. peaceful, dramatic">
          </div>
          <div class="form-group">
            <label>Style</label>
            <input [(ngModel)]="request.style" placeholder="e.g. romantic, neo-classical">
          </div>
          <div class="form-group">
            <label>Tempo</label>
            <input [(ngModel)]="request.tempo" placeholder="e.g. Adagio, Allegro">
          </div>
          <div class="form-group">
            <label>Key Signature</label>
            <input [(ngModel)]="request.keySignature" placeholder="e.g. C major, F# minor">
          </div>
        </div>
        <div class="form-group">
          <label>Target Audience</label>
          <input [(ngModel)]="request.targetAudience" placeholder="e.g. classical music lovers, relaxation seekers">
        </div>
        <div class="form-group">
          <label>Story Behind Composition</label>
          <textarea [(ngModel)]="request.storyBehindComposition"
                    placeholder="What inspired this piece? What emotions does it explore?"></textarea>
        </div>
        <div class="form-group">
          <label>Current Description</label>
          <textarea [(ngModel)]="request.description" placeholder="Current YouTube description (optional)"></textarea>
        </div>
        <div class="form-group">
          <label>Video URL</label>
          <input [(ngModel)]="request.videoUrl" placeholder="https://youtube.com/watch?v=...">
        </div>

        <button class="btn btn-primary" (click)="optimize()" [disabled]="loading" style="width:100%;justify-content:center;padding:0.75rem;">
          {{loading ? '⏳ Generating...' : '✨ Generate Optimization'}}
        </button>
        <div *ngIf="error" class="error-banner" style="margin-top:1rem;">{{error}}</div>
      </div>

      <!-- Results -->
      <div>
        <div *ngIf="!result && !loading" class="card" style="text-align:center;padding:3rem;">
          <div style="font-size:3rem;">✨</div>
          <p style="color:#888;margin-top:1rem;">Fill in the form and click Generate to get AI-powered optimization suggestions.</p>
        </div>

        <div *ngIf="loading" class="loading card">Generating optimization suggestions...</div>

        <div *ngIf="result">
          <!-- Compliance Banner -->
          <div class="card" [class.error-banner]="!result.compliance.isSafeToUse"
               style="margin-bottom:1rem;"
               [style.background]="result.compliance.riskLevel === 'Low' ? '#d4edda' : result.compliance.riskLevel === 'Medium' ? '#fff3cd' : '#f8d7da'">
            <strong>Compliance: {{result.compliance.riskLevel}} Risk</strong>
            <span *ngFor="let issue of result.compliance.issues">
              <br>⚠️ {{issue}}
            </span>
            <div *ngIf="result.compliance.isSafeToUse" style="color:#155724;">✅ Safe to use after human review</div>
          </div>

          <!-- Saved-to-review CTA: suggestions are persisted only when a video id is present -->
          <div class="card" style="margin-bottom:1rem;display:flex;justify-content:space-between;align-items:center;gap:1rem;flex-wrap:wrap;"
               *ngIf="savedToVideo">
            <div>
              <strong>💾 Saved for review</strong>
              <div style="color:#666;font-size:0.85rem;">
                These suggestions were saved. Approve or reject each one before you use it.
              </div>
            </div>
            <a [routerLink]="['/videos', videoId, 'suggestions']" class="btn btn-primary">✅ Review Suggestions</a>
          </div>

          <!-- Titles -->
          <div class="card" style="margin-bottom:1rem;">
            <h3 style="margin-bottom:0.75rem;">📝 Titles</h3>
            <div *ngFor="let title of result.titles" class="suggestion-row">
              <span>{{title}}</span>
              <button class="btn btn-icon btn-sm" (click)="copy(title)">📋</button>
            </div>
          </div>

          <!-- Tags -->
          <div class="card" style="margin-bottom:1rem;">
            <h3 style="margin-bottom:0.75rem;">🏷️ Tags</h3>
            <div style="display:flex;flex-wrap:wrap;">
              <span *ngFor="let tag of result.tags" class="chip">{{tag}}</span>
            </div>
            <div style="margin-top:0.75rem;">
              <span *ngFor="let tag of result.hashtags" class="chip" style="background:#e8f4fd;color:#0066cc;">{{tag}}</span>
            </div>
          </div>

          <!-- Descriptions -->
          <div class="card" style="margin-bottom:1rem;">
            <h3 style="margin-bottom:0.75rem;">📄 Descriptions</h3>
            <div *ngFor="let desc of result.descriptions; let i=index" style="margin-bottom:0.75rem;">
              <div style="display:flex;justify-content:space-between;margin-bottom:0.25rem;">
                <small style="color:#888;">Option {{i+1}}</small>
                <button class="btn btn-icon btn-sm" (click)="copy(desc)">📋</button>
              </div>
              <div style="background:#f8f9fa;padding:0.75rem;border-radius:6px;font-size:0.85rem;white-space:pre-line;">{{desc}}</div>
            </div>
          </div>

          <!-- Thumbnail Ideas -->
          <div class="card" style="margin-bottom:1rem;">
            <h3 style="margin-bottom:0.75rem;">🖼️ Thumbnail Ideas</h3>
            <div *ngFor="let idea of result.thumbnailIdeas" class="suggestion-row">
              <span style="font-size:0.9rem;">💡 {{idea}}</span>
              <button class="btn btn-icon btn-sm" (click)="copy(idea)">📋</button>
            </div>
          </div>

          <!-- Shorts Ideas -->
          <div class="card" style="margin-bottom:1rem;" *ngIf="result.shortsIdeas?.length">
            <h3 style="margin-bottom:0.75rem;">📱 Shorts Ideas</h3>
            <div *ngFor="let s of result.shortsIdeas" style="border:1px solid #e8e8e8;border-radius:8px;padding:1rem;margin-bottom:0.75rem;">
              <div style="font-weight:600;">{{s.title}}</div>
              <div style="font-size:0.85rem;color:#666;margin-top:0.25rem;"><strong>Hook:</strong> {{s.hook}}</div>
              <div style="font-size:0.85rem;color:#666;"><strong>Timestamp:</strong> {{s.suggestedTimestamp}}</div>
              <div style="font-size:0.85rem;color:#666;"><strong>Caption:</strong> {{s.caption}}</div>
              <button class="btn btn-icon btn-sm" style="margin-top:0.5rem;" (click)="copy(s.caption)">📋 Copy Caption</button>
            </div>
          </div>

          <!-- Social Posts -->
          <div class="card">
            <h3 style="margin-bottom:0.75rem;">📢 Social Posts</h3>
            <div *ngFor="let post of socialPostsArray" style="margin-bottom:1rem;">
              <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:0.35rem;">
                <strong style="font-size:0.9rem;">{{post.platform}}</strong>
                <button class="btn btn-icon btn-sm" (click)="copy(post.text)">📋</button>
              </div>
              <div style="background:#f8f9fa;padding:0.75rem;border-radius:6px;font-size:0.85rem;white-space:pre-line;">{{post.text}}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .suggestion-row {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 0.5rem;
      padding: 0.5rem 0;
      border-bottom: 1px solid #f0f2f5;
      &:last-child { border-bottom: none; }
    }
    @media (max-width: 768px) {
      .opt-grid { grid-template-columns: 1fr !important; }
    }
  `]
})
export class OptimizeVideoComponent implements OnInit {
  videoId = '';
  request: OptimizeVideoRequest = { title: '' };
  result: OptimizeVideoResponse | null = null;
  loading = false;
  error = '';
  savedToVideo = false;

  get socialPostsArray() {
    if (!this.result) return [];
    const sp = this.result.socialPosts;
    return [
      { platform: '📸 Instagram', text: sp.instagram },
      { platform: '🎵 TikTok', text: sp.tikTok },
      { platform: '👥 Facebook', text: sp.facebook },
      { platform: '🤖 Reddit', text: sp.reddit },
      { platform: '✖️ X (Twitter)', text: sp.x },
      { platform: '💼 LinkedIn', text: sp.linkedIn },
      { platform: '📧 Email Newsletter', text: sp.emailNewsletter },
    ].filter(p => p.text);
  }

  constructor(
    private route: ActivatedRoute,
    private optimizationService: OptimizationService,
    private videoService: VideoService
  ) {}

  ngOnInit(): void {
    this.videoId = this.route.snapshot.paramMap.get('id')!;
    if (this.videoId && this.videoId !== 'new') {
      this.request.youTubeVideoId = this.videoId;
      this.videoService.getVideo(this.videoId).subscribe({
        next: v => {
          this.request.title = v.title;
          this.request.description = v.description;
          this.request.compositionName = v.compositionName;
          this.request.mood = v.mood;
          this.request.style = v.style;
          this.request.targetAudience = v.targetAudience;
        }
      });
    }
  }

  optimize(): void {
    if (!this.request.title) { this.error = 'Title is required'; return; }
    this.loading = true;
    this.error = '';
    this.result = null;

    this.optimizationService.optimize(this.request).subscribe({
      next: res => {
        this.result = res;
        this.savedToVideo = !!this.request.youTubeVideoId;
        this.loading = false;
      },
      error: err => { this.error = this.friendlyError(err); this.loading = false; }
    });
  }

  copy(text: string): void {
    navigator.clipboard.writeText(text).catch(() => {});
  }

  private friendlyError(err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.error?.message) return e.error.message;
    if (e?.status === 0) return 'Cannot reach the API. Is the backend running on http://localhost:5000?';
    if (e?.status) return `Request failed (HTTP ${e.status}).`;
    return e?.message || 'Something went wrong.';
  }
}
