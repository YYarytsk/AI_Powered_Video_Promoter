import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { OptimizationService, OptimizeVideoRequest, OptimizeVideoResponse } from '../../services/optimization.service';
import { VideoService } from '../../services/video.service';
import { VideoWorkflowNavComponent } from '../shared/video-workflow-nav.component';

@Component({
  selector: 'app-optimize-video',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, VideoWorkflowNavComponent],
  template: `
    <app-video-workflow-nav [videoId]="videoId" />

    <div class="page-header">
      <div>
        <h1>🤖 AI Optimizer</h1>
        <p>Generate metadata and promotion copy for you to review</p>
      </div>
    </div>

    <div class="opt-grid">
      <!-- Form -->
      <div class="card">
        <h2 class="card-title">Video information</h2>
        <p class="text-muted form-intro">
          The more you tell it about the piece, the less generic the results.
        </p>

        <div class="form-group">
          <label for="opt-title">Title <span class="req" aria-hidden="true">*</span></label>
          <input id="opt-title" [(ngModel)]="request.title" placeholder="Your video title"
                 required [attr.aria-invalid]="titleMissing ? 'true' : null"
                 [attr.aria-describedby]="titleMissing ? 'opt-title-error' : null">
          <p *ngIf="titleMissing" id="opt-title-error" class="field-error">A title is required.</p>
        </div>
        <div class="form-group">
          <label for="opt-composition">Composition name</label>
          <input id="opt-composition" [(ngModel)]="request.compositionName" placeholder="e.g. Moonlit Reverie">
        </div>
        <div class="form-row">
          <div class="form-group">
            <label for="opt-mood">Mood</label>
            <input id="opt-mood" [(ngModel)]="request.mood" placeholder="e.g. peaceful, dramatic">
          </div>
          <div class="form-group">
            <label for="opt-style">Style</label>
            <input id="opt-style" [(ngModel)]="request.style" placeholder="e.g. romantic, neo-classical">
          </div>
          <div class="form-group">
            <label for="opt-tempo">Tempo</label>
            <input id="opt-tempo" [(ngModel)]="request.tempo" placeholder="e.g. Adagio, Allegro">
          </div>
          <div class="form-group">
            <label for="opt-key">Key signature</label>
            <input id="opt-key" [(ngModel)]="request.keySignature" placeholder="e.g. C major, F# minor">
          </div>
        </div>
        <div class="form-group">
          <label for="opt-audience">Target audience</label>
          <input id="opt-audience" [(ngModel)]="request.targetAudience"
                 placeholder="e.g. classical music lovers, relaxation seekers">
        </div>
        <div class="form-group">
          <label for="opt-story">Story behind the composition</label>
          <textarea id="opt-story" [(ngModel)]="request.storyBehindComposition"
                    placeholder="What inspired this piece? What emotions does it explore?"></textarea>
        </div>
        <div class="form-group">
          <label for="opt-description">Current description</label>
          <textarea id="opt-description" [(ngModel)]="request.description"
                    placeholder="Current YouTube description (optional)"></textarea>
        </div>
        <div class="form-group">
          <label for="opt-url">Video URL</label>
          <input id="opt-url" [(ngModel)]="request.videoUrl" placeholder="https://youtube.com/watch?v=...">
        </div>

        <button type="button" class="btn btn-primary btn-block" (click)="optimize()" [disabled]="loading">
          {{loading ? '⏳ Generating…' : '✨ Generate optimization'}}
        </button>
        <div *ngIf="error" class="error-banner form-error" role="alert">{{error}}</div>
      </div>

      <!-- Results -->
      <div>
        <div *ngIf="!result && !loading" class="card empty-state">
          <div class="empty-icon" aria-hidden="true">✨</div>
          <p>Fill in the form and click Generate. Everything it produces lands here for you to review — nothing is applied to YouTube.</p>
        </div>

        <div *ngIf="loading" class="loading card" role="status">Generating suggestions…</div>

        <div *ngIf="result">
          <!-- Compliance verdict. Risk is spelled out, never colour-only. -->
          <div class="compliance-banner" [ngClass]="'compliance-banner-' + riskKey" role="status">
            <div class="compliance-head">
              <span aria-hidden="true">🛡️</span>
              <span>Compliance check</span>
              <span class="badge" [ngClass]="'badge-' + riskKey">{{result.compliance.riskLevel}} risk</span>
            </div>
            <ul *ngIf="result.compliance.issues?.length">
              <li *ngFor="let issue of result.compliance.issues">{{issue}}</li>
            </ul>
            <p class="compliance-note" *ngIf="result.compliance.isSafeToUse">
              Safe to use <strong>after your own review</strong>.
            </p>
            <p class="compliance-note" *ngIf="!result.compliance.isSafeToUse">
              Do not use this as-is. Resolve the issues above and generate again.
            </p>
          </div>

          <!-- Saved-to-review CTA: suggestions are persisted only when a video id is present -->
          <div class="card next-step" *ngIf="savedToVideo">
            <div>
              <strong>💾 Saved for review</strong>
              <div class="text-muted next-step-body">
                These suggestions were saved against this video. Approve or reject each one before you use it.
              </div>
            </div>
            <a [routerLink]="['/videos', videoId, 'suggestions']" class="btn btn-primary">✅ Review suggestions</a>
          </div>

          <!-- Titles -->
          <div class="card result-card">
            <h2 class="card-title">📝 Titles</h2>
            <div *ngFor="let title of result.titles" class="suggestion-row">
              <span>{{title}}</span>
              <button type="button" class="btn btn-icon btn-sm" [class.copy-success]="copied === title"
                      [attr.aria-label]="'Copy title: ' + title"
                      (click)="copy(title)">{{copied === title ? '✓' : '📋'}}</button>
            </div>
          </div>

          <!-- Tags -->
          <div class="card result-card">
            <h2 class="card-title">🏷️ Tags &amp; hashtags</h2>
            <div class="tag-wrap">
              <span *ngFor="let tag of result.tags" class="chip">{{tag}}</span>
            </div>
            <div class="tag-wrap hashtags">
              <span *ngFor="let tag of result.hashtags" class="chip chip-alt">{{tag}}</span>
            </div>
            <div class="tag-actions">
              <button type="button" class="btn btn-secondary btn-sm" (click)="copy(joined(result.tags))">📋 Copy all tags</button>
              <button type="button" class="btn btn-secondary btn-sm" *ngIf="result.hashtags?.length"
                      (click)="copy(joined(result.hashtags, ' '))">📋 Copy all hashtags</button>
            </div>
          </div>

          <!-- Descriptions -->
          <div class="card result-card">
            <h2 class="card-title">📄 Descriptions</h2>
            <div *ngFor="let desc of result.descriptions; let i = index" class="desc-block">
              <div class="desc-head">
                <span class="text-meta">Option {{i + 1}}</span>
                <button type="button" class="btn btn-icon btn-sm" [class.copy-success]="copied === desc"
                        [attr.aria-label]="'Copy description option ' + (i + 1)"
                        (click)="copy(desc)">{{copied === desc ? '✓' : '📋'}}</button>
              </div>
              <div class="content-block">{{desc}}</div>
            </div>
          </div>

          <!-- Thumbnail Ideas -->
          <div class="card result-card">
            <h2 class="card-title">🖼️ Thumbnail ideas</h2>
            <div *ngFor="let idea of result.thumbnailIdeas" class="suggestion-row">
              <span class="idea-text"><span aria-hidden="true">💡</span> {{idea}}</span>
              <button type="button" class="btn btn-icon btn-sm" [class.copy-success]="copied === idea"
                      [attr.aria-label]="'Copy thumbnail idea'"
                      (click)="copy(idea)">{{copied === idea ? '✓' : '📋'}}</button>
            </div>
          </div>

          <!-- Shorts Ideas -->
          <div class="card result-card" *ngIf="result.shortsIdeas?.length">
            <h2 class="card-title">📱 Shorts ideas</h2>
            <div *ngFor="let s of result.shortsIdeas" class="shorts-idea">
              <div class="idea-title">{{s.title}}</div>
              <div class="idea-field"><strong>Hook:</strong> {{s.hook}}</div>
              <div class="idea-field"><strong>Timestamp:</strong> {{s.suggestedTimestamp}}</div>
              <div class="idea-field"><strong>Caption:</strong> {{s.caption}}</div>
              <button type="button" class="btn btn-secondary btn-sm shorts-copy"
                      [class.copy-success]="copied === s.caption"
                      [attr.aria-label]="'Copy caption for ' + s.title"
                      (click)="copy(s.caption)">{{copied === s.caption ? '✓ Copied' : '📋 Copy caption'}}</button>
            </div>
          </div>

          <!-- Social Posts -->
          <div class="card result-card">
            <h2 class="card-title">📢 Social posts</h2>
            <div *ngFor="let post of socialPostsArray" class="desc-block">
              <div class="desc-head">
                <strong class="post-platform"><span aria-hidden="true">{{post.icon}}</span> {{post.platform}}</strong>
                <button type="button" class="btn btn-icon btn-sm" [class.copy-success]="copied === post.text"
                        [attr.aria-label]="'Copy the ' + post.platform + ' post'"
                        (click)="copy(post.text)">{{copied === post.text ? '✓' : '📋'}}</button>
              </div>
              <div class="content-block">{{post.text}}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <p class="sr-only" role="status" aria-live="polite">{{liveMessage}}</p>
  `,
  styles: [`
    .opt-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; align-items: start; }
    .card-title { font-size: 1.15rem; margin-bottom: 0.75rem; }
    .form-intro { font-size: 0.85rem; margin-bottom: 1rem; }
    .req { color: #b02a37; }
    .field-error { color: #8b1c26; font-size: 0.8rem; margin-top: 0.3rem; }
    .form-error { margin-top: 1rem; margin-bottom: 0; }
    .result-card { margin-bottom: 1rem; }
    .suggestion-row {
      display: flex; align-items: flex-start; justify-content: space-between;
      gap: 0.5rem; padding: 0.5rem 0; border-bottom: 1px solid #f0f2f5;
    }
    .suggestion-row:last-child { border-bottom: none; }
    .idea-text { font-size: 0.9rem; }
    .tag-wrap { display: flex; flex-wrap: wrap; }
    .hashtags { margin-top: 0.5rem; }
    .chip-alt { background: #e2f0fb; color: #0b5394; }
    .tag-actions { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.75rem; }
    .desc-block { margin-bottom: 0.9rem; }
    .desc-block:last-child { margin-bottom: 0; }
    .desc-head {
      display: flex; justify-content: space-between; align-items: center;
      gap: 0.5rem; margin-bottom: 0.3rem;
    }
    .post-platform { font-size: 0.9rem; }
    .shorts-idea { border: 1px solid #e8e8e8; border-radius: 8px; padding: 1rem; margin-bottom: 0.75rem; }
    .shorts-idea:last-child { margin-bottom: 0; }
    .idea-title { font-weight: 600; }
    .idea-field { font-size: 0.85rem; color: #4a4f5a; margin-top: 0.15rem; }
    .shorts-copy { margin-top: 0.6rem; }
    @media (max-width: 900px) {
      .opt-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class OptimizeVideoComponent implements OnInit {
  videoId = '';
  request: OptimizeVideoRequest = { title: '' };
  result: OptimizeVideoResponse | null = null;
  loading = false;
  error = '';
  titleMissing = false;
  savedToVideo = false;
  copied: string | null = null;
  liveMessage = '';

  private copyTimer: ReturnType<typeof setTimeout> | null = null;

  get socialPostsArray() {
    if (!this.result) return [];
    const sp = this.result.socialPosts;
    return [
      { icon: '📸', platform: 'Instagram', text: sp.instagram },
      { icon: '🎵', platform: 'TikTok', text: sp.tikTok },
      { icon: '👥', platform: 'Facebook', text: sp.facebook },
      { icon: '🤖', platform: 'Reddit', text: sp.reddit },
      { icon: '✖️', platform: 'X (Twitter)', text: sp.x },
      { icon: '💼', platform: 'LinkedIn', text: sp.linkedIn },
      { icon: '📧', platform: 'Email newsletter', text: sp.emailNewsletter },
    ].filter(p => p.text);
  }

  /** Badge/banner modifier for the compliance verdict. */
  get riskKey(): string {
    const key = (this.result?.compliance?.riskLevel || '').toLowerCase();
    // Unknown verdicts must not render as an unstyled (i.e. reassuring) banner.
    return ['low', 'medium', 'high', 'blocked'].includes(key) ? key : 'medium';
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
        },
        // Prefilling is a convenience; a failure here just leaves the form empty
        // rather than blocking the screen.
        error: () => {}
      });
    }
  }

  optimize(): void {
    this.titleMissing = !this.request.title?.trim();
    if (this.titleMissing) {
      this.error = 'Add a title before generating — it is what everything else is built from.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.result = null;

    this.optimizationService.optimize(this.request).subscribe({
      next: res => {
        this.result = res;
        // Blocked content is deliberately not persisted by the API, so don't claim it was saved.
        this.savedToVideo = !!this.request.youTubeVideoId && res.compliance?.riskLevel !== 'Blocked';
        this.loading = false;
        this.liveMessage = `Suggestions ready. Compliance risk: ${res.compliance?.riskLevel}.`;
      },
      error: err => { this.error = this.friendlyError(err); this.loading = false; }
    });
  }

  joined(values: string[] | undefined, separator = ', '): string {
    return (values || []).join(separator);
  }

  copy(text: string): void {
    navigator.clipboard.writeText(text).then(
      () => {
        this.copied = text;
        this.liveMessage = 'Copied to clipboard.';
        if (this.copyTimer) clearTimeout(this.copyTimer);
        this.copyTimer = setTimeout(() => { this.copied = null; }, 1600);
      },
      () => { this.error = 'Could not copy to the clipboard. Select the text and copy it manually.'; }
    );
  }

  private friendlyError(err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.error?.message) return e.error.message;
    if (e?.status === 0) return 'Cannot reach the API. Is the backend running on http://localhost:5000?';
    if (e?.status) return `Request failed (HTTP ${e.status}).`;
    return e?.message || 'Something went wrong.';
  }
}
