import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { PromotionDraftService, PromotionDraftDto } from '../../services/promotion-draft.service';

@Component({
  selector: 'app-promotion-drafts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="page-header">
      <div>
        <a [routerLink]="['/videos', videoId]" style="color:#6c63ff;text-decoration:none;font-size:0.9rem;">← Back to Video</a>
        <h1 style="margin-top:0.5rem;">📢 Promotion Drafts</h1>
        <p>Human-reviewed promotion content for social media</p>
      </div>
      <button class="btn btn-primary" (click)="generateDrafts()" [disabled]="generating">
        {{generating ? '⏳ Generating...' : '✨ Generate Drafts'}}
      </button>
    </div>

    <div class="compliance-note card" style="margin-bottom:1.5rem;background:#f0eeff;border-left:4px solid #6c63ff;">
      <strong>⚠️ Human Review Required</strong>
      <p style="color:#666;font-size:0.9rem;margin-top:0.25rem;">
        Review each draft before using. Edit as needed to ensure accuracy and authenticity.
        Approve drafts you're ready to post, and mark them as Posted manually after publishing.
      </p>
    </div>

    <div *ngIf="loading" class="loading">Loading drafts...</div>
    <div *ngIf="error" class="error-banner">{{error}}</div>

    <div *ngIf="!loading && drafts.length === 0 && !error" class="empty-state card">
      <div class="empty-icon">📢</div>
      <p>No promotion drafts yet. Click "Generate Drafts" to create content for all platforms.</p>
    </div>

    <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(380px,1fr));gap:1.25rem;">
      <div *ngFor="let draft of drafts" class="card draft-card">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:0.75rem;">
          <div>
            <span style="font-size:1.2rem;">{{platformIcon(draft.platform)}}</span>
            <strong style="margin-left:0.5rem;">{{draft.platform}}</strong>
          </div>
          <span class="badge" [ngClass]="statusClass(draft.status)">{{draft.status}}</span>
        </div>

        <div *ngIf="editingId !== draft.id">
          <div style="background:#f8f9fa;padding:0.75rem;border-radius:6px;font-size:0.85rem;white-space:pre-line;min-height:80px;">
            {{draft.draftText}}
          </div>
          <div style="display:flex;gap:0.5rem;margin-top:0.75rem;flex-wrap:wrap;">
            <button class="btn btn-secondary btn-sm" (click)="copy(draft.draftText)">📋 Copy</button>
            <button class="btn btn-secondary btn-sm" (click)="startEdit(draft)">✏️ Edit</button>
            <button *ngIf="draft.status !== 'Approved'" class="btn btn-success btn-sm"
                    (click)="updateStatus(draft, 'Approved')">✓ Approve</button>
            <button *ngIf="draft.status !== 'PostedManually'" class="btn btn-primary btn-sm"
                    (click)="updateStatus(draft, 'PostedManually')">📤 Mark Posted</button>
            <button *ngIf="draft.status !== 'Rejected'" class="btn btn-danger btn-sm"
                    (click)="updateStatus(draft, 'Rejected')">✗ Reject</button>
          </div>
        </div>

        <div *ngIf="editingId === draft.id">
          <textarea [(ngModel)]="editText" style="width:100%;min-height:120px;"></textarea>
          <div style="display:flex;gap:0.5rem;margin-top:0.75rem;">
            <button class="btn btn-primary btn-sm" (click)="saveEdit(draft)">Save</button>
            <button class="btn btn-secondary btn-sm" (click)="editingId = null">Cancel</button>
          </div>
        </div>

        <div style="font-size:0.75rem;color:#aaa;margin-top:0.5rem;">
          Created {{draft.createdAt | date:'short'}}
          <span *ngIf="draft.postedAt"> · Posted {{draft.postedAt | date:'short'}}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`.draft-card { transition: box-shadow 0.2s; &:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.12); } }`]
})
export class PromotionDraftsComponent implements OnInit {
  videoId = '';
  drafts: PromotionDraftDto[] = [];
  loading = true;
  error = '';
  generating = false;
  editingId: number | null = null;
  editText = '';

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
    this.draftService.getDrafts(this.videoId).subscribe({
      next: d => { this.drafts = d; this.loading = false; },
      error: err => { this.error = err.message; this.loading = false; }
    });
  }

  generateDrafts(): void {
    this.generating = true;
    this.draftService.generateDrafts(this.videoId).subscribe({
      next: d => { this.drafts = [...d, ...this.drafts]; this.generating = false; },
      error: err => { this.error = err.message; this.generating = false; }
    });
  }

  startEdit(draft: PromotionDraftDto): void {
    this.editingId = draft.id;
    this.editText = draft.draftText;
  }

  saveEdit(draft: PromotionDraftDto): void {
    this.draftService.updateDraft(draft.id, { draftText: this.editText }).subscribe({
      next: updated => {
        const idx = this.drafts.findIndex(d => d.id === draft.id);
        if (idx >= 0) this.drafts[idx] = updated;
        this.editingId = null;
      }
    });
  }

  updateStatus(draft: PromotionDraftDto, status: string): void {
    this.draftService.updateDraft(draft.id, { status }).subscribe({
      next: updated => {
        const idx = this.drafts.findIndex(d => d.id === draft.id);
        if (idx >= 0) this.drafts[idx] = updated;
      }
    });
  }

  copy(text: string): void { navigator.clipboard.writeText(text).catch(() => {}); }
  platformIcon(p: string): string { return this.platformIcons[p] || '📄'; }
  statusClass(s: string): string {
    const map: Record<string, string> = {
      Draft: 'badge-draft', Approved: 'badge-approved',
      Rejected: 'badge-rejected', PostedManually: 'badge-posted'
    };
    return map[s] || 'badge-draft';
  }
}
