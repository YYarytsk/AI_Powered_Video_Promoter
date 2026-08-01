import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { OptimizationService, SuggestionDto } from '../../services/optimization.service';

interface ShortsIdeaPayload {
  title?: string;
  hook?: string;
  suggestedTimestamp?: string;
  description?: string;
  caption?: string;
}

interface SuggestionGroup {
  type: string;
  icon: string;
  label: string;
  items: SuggestionDto[];
}

type StatusFilter = 'all' | 'pending' | 'approved' | 'rejected';

@Component({
  selector: 'app-suggestions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page-header">
      <div>
        <a [routerLink]="['/videos', videoId]" style="color:#6c63ff;text-decoration:none;font-size:0.9rem;">← Back to Video</a>
        <h1 style="margin-top:0.5rem;">✅ Review Suggestions</h1>
        <p>Approve or reject each AI suggestion before you use it</p>
      </div>
      <a [routerLink]="['/videos', videoId, 'optimize']" class="btn btn-primary">🤖 Generate More</a>
    </div>

    <div class="card" style="margin-bottom:1.5rem;background:#f0eeff;border-left:4px solid #6c63ff;">
      <strong>⚠️ Human Review Required</strong>
      <p style="color:#666;font-size:0.9rem;margin-top:0.25rem;">
        Nothing here is published automatically. Approving a suggestion only marks it as reviewed by you —
        you still copy it and apply it to YouTube manually.
      </p>
    </div>

    <div *ngIf="loading" class="loading">Loading suggestions...</div>
    <div *ngIf="error" class="error-banner">{{error}}</div>

    <!-- Approve/reject failures must never hide the list the user is reviewing -->
    <div *ngIf="actionError" class="error-banner action-error">
      <span>{{actionError}}</span>
      <button class="btn btn-icon btn-sm" title="Dismiss" (click)="actionError = ''">✕</button>
    </div>

    <div *ngIf="!loading && !error">
      <!-- Summary + filters -->
      <div class="card" style="margin-bottom:1.5rem;" *ngIf="suggestions.length > 0">
        <div class="stats-grid" style="margin-bottom:1rem;">
          <div class="stat-card">
            <div class="stat-value">{{suggestions.length}}</div>
            <div class="stat-label">Total</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">{{pendingCount}}</div>
            <div class="stat-label">Pending</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">{{approvedCount}}</div>
            <div class="stat-label">Approved</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">{{rejectedCount}}</div>
            <div class="stat-label">Rejected</div>
          </div>
        </div>
        <div style="display:flex;gap:0.5rem;flex-wrap:wrap;">
          <button *ngFor="let f of filters"
                  class="btn btn-sm"
                  [class.btn-primary]="filter === f.key"
                  [class.btn-secondary]="filter !== f.key"
                  (click)="filter = f.key">
            {{f.label}}
          </button>
        </div>
      </div>

      <!-- Empty states -->
      <div *ngIf="suggestions.length === 0" class="empty-state card">
        <div class="empty-icon">✨</div>
        <p>No suggestions yet for this video.</p>
        <a [routerLink]="['/videos', videoId, 'optimize']" class="btn btn-primary" style="margin-top:1rem;">
          🤖 Run the AI Optimizer
        </a>
      </div>

      <div *ngIf="suggestions.length > 0 && visibleGroups.length === 0" class="empty-state card">
        <div class="empty-icon">🔍</div>
        <p>No {{filter}} suggestions.</p>
      </div>

      <!-- Grouped suggestions -->
      <div *ngFor="let group of visibleGroups" class="card" style="margin-bottom:1.25rem;">
        <h3 style="margin-bottom:0.85rem;">{{group.icon}} {{group.label}} ({{group.items.length}})</h3>

        <div *ngFor="let s of group.items" class="suggestion-item">
          <div style="flex:1;min-width:0;">
            <!-- Shorts ideas are stored as JSON; render them structured -->
            <div *ngIf="asShortsIdea(s) as idea; else plainText">
              <div style="font-weight:600;">{{idea.title}}</div>
              <div style="font-size:0.85rem;color:#666;margin-top:0.2rem;" *ngIf="idea.hook">
                <strong>Hook:</strong> {{idea.hook}}
              </div>
              <div style="font-size:0.85rem;color:#666;" *ngIf="idea.suggestedTimestamp">
                <strong>Timestamp:</strong> {{idea.suggestedTimestamp}}
              </div>
              <div style="font-size:0.85rem;color:#666;" *ngIf="idea.caption">
                <strong>Caption:</strong> {{idea.caption}}
              </div>
            </div>
            <ng-template #plainText>
              <div style="white-space:pre-line;font-size:0.9rem;">{{s.suggestionText}}</div>
            </ng-template>

            <div style="margin-top:0.4rem;display:flex;gap:0.5rem;align-items:center;flex-wrap:wrap;">
              <span class="badge" [ngClass]="statusClass(s)">{{statusLabel(s)}}</span>
              <span *ngIf="s.platform" class="chip" style="margin:0;">{{s.platform}}</span>
              <span style="font-size:0.75rem;color:#aaa;">{{s.createdAt | date:'short'}}</span>
            </div>
          </div>

          <div class="suggestion-actions">
            <button class="btn btn-icon btn-sm" title="Copy" (click)="copy(s)">📋</button>
            <button class="btn btn-success btn-sm"
                    [disabled]="s.isApproved || busyIds.has(s.id)"
                    (click)="approve(s)">✓ Approve</button>
            <button class="btn btn-danger btn-sm"
                    [disabled]="s.isRejected || busyIds.has(s.id)"
                    (click)="reject(s)">✗ Reject</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .action-error {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }
    .suggestion-item {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      padding: 0.85rem 0;
      border-bottom: 1px solid #f0f2f5;
      &:last-child { border-bottom: none; }
    }
    .suggestion-actions {
      display: flex;
      gap: 0.4rem;
      flex-shrink: 0;
      flex-wrap: wrap;
      justify-content: flex-end;
    }
    @media (max-width: 640px) {
      .suggestion-item { flex-direction: column; }
      .suggestion-actions { justify-content: flex-start; }
    }
  `]
})
export class SuggestionsComponent implements OnInit {
  videoId = '';
  suggestions: SuggestionDto[] = [];
  loading = true;
  error = '';          // load failures only - these hide the list
  actionError = '';    // approve/reject failures - shown above the list
  filter: StatusFilter = 'all';
  busyIds = new Set<number>();

  // asShortsIdea() is called from the template, so memoise it: returning a fresh
  // object on every change-detection pass would thrash the *ngIf "as" binding.
  private shortsCache = new Map<number, ShortsIdeaPayload | null>();

  filters: { key: StatusFilter; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'pending', label: 'Pending' },
    { key: 'approved', label: 'Approved' },
    { key: 'rejected', label: 'Rejected' }
  ];

  private groupMeta: { type: string; icon: string; label: string }[] = [
    { type: 'Title', icon: '📝', label: 'Titles' },
    { type: 'Description', icon: '📄', label: 'Descriptions' },
    { type: 'Tags', icon: '🏷️', label: 'Tags' },
    { type: 'Hashtags', icon: '#️⃣', label: 'Hashtags' },
    { type: 'ThumbnailIdea', icon: '🖼️', label: 'Thumbnail Ideas' },
    { type: 'ShortsIdea', icon: '📱', label: 'Shorts Ideas' },
    { type: 'SocialPost', icon: '📢', label: 'Social Posts' },
    { type: 'AnalyticsRecommendation', icon: '📊', label: 'Analytics Recommendations' }
  ];

  constructor(
    private route: ActivatedRoute,
    private optimizationService: OptimizationService
  ) {}

  ngOnInit(): void {
    this.videoId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.actionError = '';
    this.shortsCache.clear();
    this.optimizationService.getSuggestions(this.videoId).subscribe({
      next: s => { this.suggestions = s; this.loading = false; },
      error: err => { this.error = this.friendlyError(err); this.loading = false; }
    });
  }

  get pendingCount(): number {
    return this.suggestions.filter(s => !s.isApproved && !s.isRejected).length;
  }

  get approvedCount(): number {
    return this.suggestions.filter(s => s.isApproved).length;
  }

  get rejectedCount(): number {
    return this.suggestions.filter(s => s.isRejected).length;
  }

  get visibleGroups(): SuggestionGroup[] {
    const matches = (s: SuggestionDto) => {
      switch (this.filter) {
        case 'pending': return !s.isApproved && !s.isRejected;
        case 'approved': return s.isApproved;
        case 'rejected': return s.isRejected;
        default: return true;
      }
    };

    const filtered = this.suggestions.filter(matches);
    const groups: SuggestionGroup[] = [];

    for (const meta of this.groupMeta) {
      const items = filtered.filter(s => s.suggestionType === meta.type);
      if (items.length > 0) {
        groups.push({ type: meta.type, icon: meta.icon, label: meta.label, items });
      }
    }

    // Anything with an unrecognised type still gets shown rather than silently dropped
    const known = new Set(this.groupMeta.map(m => m.type));
    const other = filtered.filter(s => !known.has(s.suggestionType));
    if (other.length > 0) {
      groups.push({ type: 'Other', icon: '💡', label: 'Other', items: other });
    }

    return groups;
  }

  approve(s: SuggestionDto): void {
    this.actionError = '';
    this.busyIds.add(s.id);
    this.optimizationService.approveSuggestion(this.videoId, s.id).subscribe({
      next: updated => { this.replace(updated); this.busyIds.delete(s.id); },
      error: err => { this.actionError = this.actionFailed('approve', err); this.busyIds.delete(s.id); }
    });
  }

  reject(s: SuggestionDto): void {
    this.actionError = '';
    this.busyIds.add(s.id);
    this.optimizationService.rejectSuggestion(this.videoId, s.id).subscribe({
      next: updated => { this.replace(updated); this.busyIds.delete(s.id); },
      error: err => { this.actionError = this.actionFailed('reject', err); this.busyIds.delete(s.id); }
    });
  }

  private replace(updated: SuggestionDto): void {
    const idx = this.suggestions.findIndex(s => s.id === updated.id);
    if (idx >= 0) this.suggestions[idx] = updated;
  }

  asShortsIdea(s: SuggestionDto): ShortsIdeaPayload | null {
    if (s.suggestionType !== 'ShortsIdea') return null;

    const cached = this.shortsCache.get(s.id);
    if (cached !== undefined) return cached;

    let result: ShortsIdeaPayload | null = null;
    try {
      const parsed = JSON.parse(s.suggestionText);
      if (parsed && typeof parsed === 'object') {
        // Older rows were persisted with PascalCase keys; normalise so both shapes render.
        const lower: Record<string, unknown> = {};
        for (const [k, v] of Object.entries(parsed)) {
          lower[k.charAt(0).toLowerCase() + k.slice(1)] = v;
        }
        const idea = lower as ShortsIdeaPayload;
        // If nothing renderable came back, fall through to showing the raw text.
        if (idea.title || idea.hook || idea.caption || idea.description) {
          result = idea;
        }
      }
    } catch {
      result = null;
    }

    this.shortsCache.set(s.id, result);
    return result;
  }

  statusLabel(s: SuggestionDto): string {
    if (s.isApproved) return 'Approved';
    if (s.isRejected) return 'Rejected';
    return 'Pending';
  }

  statusClass(s: SuggestionDto): string {
    if (s.isApproved) return 'badge-approved';
    if (s.isRejected) return 'badge-rejected';
    return 'badge-draft';
  }

  copy(s: SuggestionDto): void {
    const idea = this.asShortsIdea(s);
    const text = idea ? (idea.caption || idea.title || s.suggestionText) : s.suggestionText;
    navigator.clipboard.writeText(text).catch(() => {});
  }

  private friendlyError(err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.error?.message) return e.error.message;
    if (e?.status === 0) return 'Cannot reach the API. Is the backend running on http://localhost:5000?';
    if (e?.status === 404) return 'Video not found.';
    if (e?.status) return `Request failed (HTTP ${e.status}).`;
    return e?.message || 'Something went wrong.';
  }

  // Same wording rules as friendlyError, but a 404 here means the suggestion is gone,
  // not the video - and the list stays on screen so we say what failed.
  private actionFailed(action: 'approve' | 'reject', err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.status === 404) {
      return `Could not ${action}: that suggestion no longer exists. Reload the page.`;
    }
    return `Could not ${action} the suggestion. ${this.friendlyError(err)}`;
  }
}
