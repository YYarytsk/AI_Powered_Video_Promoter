import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { PromotionDraftService, PromotionDraftDto } from '../../services/promotion-draft.service';
import { VideoWorkflowNavComponent } from '../shared/video-workflow-nav.component';

@Component({
  selector: 'app-promotion-drafts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, VideoWorkflowNavComponent],
  template: `
    <app-video-workflow-nav [videoId]="videoId" />

    <div class="page-header">
      <div>
        <h1>📢 Promotion Drafts</h1>
        <p>Per-platform copy for you to review, edit and post by hand</p>
      </div>
      <button type="button" class="btn btn-primary" (click)="generateDrafts()" [disabled]="generating">
        {{generating ? '⏳ Generating…' : (drafts.length ? '✨ Regenerate drafts' : '✨ Generate drafts')}}
      </button>
    </div>

    <div class="notice">
      <span class="notice-icon" aria-hidden="true">⚠️</span>
      <div>
        <div class="notice-title">Human review required</div>
        <p class="notice-body">
          Nothing here is posted for you. Read each draft, edit it until it sounds like you, approve the ones
          you want, then copy them into the platform yourself and mark them as Posted.
        </p>
      </div>
    </div>

    <div *ngIf="loading" class="loading" role="status">Loading drafts…</div>

    <!-- Load failure: nothing to show, so offer a retry -->
    <div *ngIf="error" class="error-banner" role="alert">
      <span>{{error}}</span>
      <button type="button" class="btn btn-secondary btn-sm" (click)="loadDrafts()">Retry</button>
    </div>

    <!-- Action failure: the drafts stay on screen underneath -->
    <div *ngIf="actionError" class="error-banner" role="alert">
      <span>{{actionError}}</span>
      <button type="button" class="btn btn-icon btn-sm" aria-label="Dismiss message" (click)="actionError = ''">✕</button>
    </div>

    <div *ngIf="!loading && !error && drafts.length === 0" class="empty-state card">
      <div class="empty-icon" aria-hidden="true">📢</div>
      <p>No promotion drafts yet. Generating creates one draft per platform from this video's approved material.</p>
      <button type="button" class="btn btn-primary" (click)="generateDrafts()" [disabled]="generating">
        {{generating ? '⏳ Generating…' : '✨ Generate drafts'}}
      </button>
    </div>

    <div *ngIf="drafts.length > 0" class="progress-head review-line">
      <strong>{{reviewedCount}} of {{drafts.length}} drafts reviewed</strong>
      <span class="text-meta">{{postedCount}} marked as posted</span>
    </div>

    <div class="card-grid">
      <div *ngFor="let draft of drafts; trackBy: trackDraft" class="card draft-card">
        <div class="draft-head">
          <div>
            <span class="platform-icon" aria-hidden="true">{{platformIcon(draft.platform)}}</span>
            <strong>{{draft.platform}}</strong>
          </div>
          <span class="badge" [ngClass]="statusClass(draft.status)">{{statusLabel(draft.status)}}</span>
        </div>

        <div *ngIf="editingId !== draft.id">
          <div class="content-block draft-text">{{draft.draftText}}</div>
          <div class="draft-actions">
            <button type="button" class="btn btn-secondary btn-sm"
                    [class.copy-success]="copiedId === draft.id"
                    [attr.aria-label]="'Copy the ' + draft.platform + ' draft'"
                    (click)="copy(draft)">{{copiedId === draft.id ? '✓ Copied' : '📋 Copy'}}</button>
            <button type="button" class="btn btn-secondary btn-sm"
                    [attr.aria-label]="'Edit the ' + draft.platform + ' draft'"
                    (click)="startEdit(draft)">✏️ Edit</button>
            <button type="button" *ngIf="draft.status !== 'Approved'" class="btn btn-success btn-sm"
                    [disabled]="busyIds.has(draft.id)"
                    [attr.aria-label]="'Approve the ' + draft.platform + ' draft'"
                    (click)="updateStatus(draft, 'Approved')">{{busyIds.has(draft.id) ? '…' : '✓ Approve'}}</button>
            <button type="button" *ngIf="draft.status !== 'PostedManually'" class="btn btn-primary btn-sm"
                    [disabled]="busyIds.has(draft.id)"
                    [attr.aria-label]="'Mark the ' + draft.platform + ' draft as posted'"
                    (click)="updateStatus(draft, 'PostedManually')">{{busyIds.has(draft.id) ? '…' : '📤 Mark posted'}}</button>
            <button type="button" *ngIf="draft.status !== 'Rejected'" class="btn btn-danger btn-sm"
                    [disabled]="busyIds.has(draft.id)"
                    [attr.aria-label]="'Reject the ' + draft.platform + ' draft'"
                    (click)="updateStatus(draft, 'Rejected')">{{busyIds.has(draft.id) ? '…' : '✗ Reject'}}</button>
          </div>
        </div>

        <div *ngIf="editingId === draft.id" class="form-group edit-area">
          <label [attr.for]="'draft-' + draft.id">{{draft.platform}} draft text</label>
          <textarea [id]="'draft-' + draft.id" [(ngModel)]="editText" rows="6"></textarea>
          <div class="draft-actions">
            <button type="button" class="btn btn-primary btn-sm" [disabled]="busyIds.has(draft.id)"
                    (click)="saveEdit(draft)">{{busyIds.has(draft.id) ? '⏳ Saving…' : 'Save'}}</button>
            <button type="button" class="btn btn-secondary btn-sm" (click)="cancelEdit()">Cancel</button>
          </div>
        </div>

        <div class="text-meta draft-dates">
          Created {{draft.createdAt | date:'short'}}
          <span *ngIf="draft.postedAt"> · Posted {{draft.postedAt | date:'short'}}</span>
        </div>
      </div>
    </div>

    <p class="sr-only" role="status" aria-live="polite">{{liveMessage}}</p>
  `,
  styles: [`
    .review-line { margin-bottom: 1rem; }
    .draft-card {
      display: flex; flex-direction: column;
      transition: box-shadow 0.2s;
    }
    .draft-card:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.12); }
    .draft-head {
      display: flex; justify-content: space-between; align-items: center;
      gap: 0.75rem; margin-bottom: 0.75rem;
    }
    .platform-icon { font-size: 1.2rem; margin-right: 0.4rem; }
    .draft-text { min-height: 80px; }
    .draft-actions { display: flex; gap: 0.5rem; margin-top: 0.75rem; flex-wrap: wrap; }
    .edit-area { margin-bottom: 0; }
    .draft-dates { margin-top: auto; padding-top: 0.75rem; }
  `]
})
export class PromotionDraftsComponent implements OnInit {
  videoId = '';
  drafts: PromotionDraftDto[] = [];
  loading = true;
  error = '';        // load failures - nothing to show
  actionError = '';  // generate/edit/status failures - the list stays visible
  generating = false;
  editingId: number | null = null;
  editText = '';
  busyIds = new Set<number>();
  copiedId: number | null = null;
  liveMessage = '';

  private copyTimer: ReturnType<typeof setTimeout> | null = null;

  platformIcons: Record<string, string> = {
    Instagram: '📸', TikTok: '🎵', Facebook: '👥', Reddit: '🤖',
    X: '✖️', LinkedIn: '💼', Email: '📧'
  };

  constructor(private route: ActivatedRoute, private draftService: PromotionDraftService) {}

  ngOnInit(): void {
    this.videoId = this.route.snapshot.paramMap.get('id')!;
    this.loadDrafts();
  }

  loadDrafts(): void {
    this.loading = true;
    this.error = '';
    this.draftService.getDrafts(this.videoId).subscribe({
      next: d => { this.drafts = d; this.loading = false; },
      error: err => { this.error = this.actionFailed('load the drafts', err); this.loading = false; }
    });
  }

  get reviewedCount(): number {
    return this.drafts.filter(d => d.status !== 'Draft').length;
  }

  get postedCount(): number {
    return this.drafts.filter(d => d.status === 'PostedManually').length;
  }

  generateDrafts(): void {
    this.generating = true;
    this.actionError = '';
    this.draftService.generateDrafts(this.videoId).subscribe({
      // Regenerating supersedes the previous un-reviewed batch server-side, so reload
      // rather than prepending — otherwise superseded drafts would linger in the list.
      next: () => {
        this.generating = false;
        this.liveMessage = 'New drafts generated. Review each one before posting.';
        this.loadDrafts();
      },
      error: err => { this.actionError = this.actionFailed('generate drafts', err); this.generating = false; }
    });
  }

  startEdit(draft: PromotionDraftDto): void {
    this.editingId = draft.id;
    this.editText = draft.draftText;
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editText = '';
  }

  saveEdit(draft: PromotionDraftDto): void {
    this.actionError = '';
    this.busyIds.add(draft.id);
    this.draftService.updateDraft(draft.id, { draftText: this.editText }).subscribe({
      next: updated => {
        this.replace(updated);
        this.busyIds.delete(draft.id);
        this.editingId = null;
        this.liveMessage = `${draft.platform} draft saved.`;
      },
      // Leave the editor open so the typed text is not lost, and say what failed.
      error: err => {
        this.actionError = this.actionFailed('save your edit', err);
        this.busyIds.delete(draft.id);
      }
    });
  }

  updateStatus(draft: PromotionDraftDto, status: string): void {
    this.actionError = '';
    this.busyIds.add(draft.id);
    this.draftService.updateDraft(draft.id, { status }).subscribe({
      next: updated => {
        this.replace(updated);
        this.busyIds.delete(draft.id);
        this.liveMessage = `${draft.platform} draft marked as ${this.statusLabel(status)}.`;
      },
      // The row is only rewritten from the server response, so a failure leaves
      // the draft showing its last known (still correct) status.
      error: err => {
        this.actionError = this.actionFailed(`mark this draft as ${this.statusLabel(status)}`, err);
        this.busyIds.delete(draft.id);
      }
    });
  }

  private replace(updated: PromotionDraftDto): void {
    const idx = this.drafts.findIndex(d => d.id === updated.id);
    if (idx >= 0) this.drafts[idx] = updated;
  }

  private actionFailed(what: string, err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.status === 404) return `Could not ${what}: that draft no longer exists. Reload the page.`;
    if (e?.status === 0) return `Could not ${what}. Cannot reach the API. Is the backend running on http://localhost:5000?`;
    return `Could not ${what}. ${e?.error?.message || e?.message || 'Something went wrong.'}`;
  }

  trackDraft = (_: number, d: PromotionDraftDto) => d.id;

  copy(draft: PromotionDraftDto): void {
    navigator.clipboard.writeText(draft.draftText).then(
      () => {
        this.copiedId = draft.id;
        this.liveMessage = `${draft.platform} draft copied to the clipboard.`;
        if (this.copyTimer) clearTimeout(this.copyTimer);
        this.copyTimer = setTimeout(() => { this.copiedId = null; }, 1600);
      },
      () => { this.actionError = 'Could not copy to the clipboard. Select the text and copy it manually.'; }
    );
  }

  platformIcon(p: string): string { return this.platformIcons[p] || '📄'; }
  statusLabel(s: string): string { return s === 'PostedManually' ? 'Posted' : s; }
  statusClass(s: string): string {
    const map: Record<string, string> = {
      Draft: 'badge-draft', Approved: 'badge-approved',
      Rejected: 'badge-rejected', PostedManually: 'badge-posted'
    };
    return map[s] || 'badge-draft';
  }
}
