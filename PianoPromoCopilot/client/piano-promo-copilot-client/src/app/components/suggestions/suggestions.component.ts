import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { OptimizationService, SuggestionDto } from '../../services/optimization.service';
import { VideoWorkflowNavComponent } from '../shared/video-workflow-nav.component';

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
  pending: number;
}

type StatusFilter = 'all' | 'pending' | 'approved' | 'rejected';

const CLAMP_AT = 260;

@Component({
  selector: 'app-suggestions',
  standalone: true,
  imports: [CommonModule, RouterLink, VideoWorkflowNavComponent],
  template: `
    <app-video-workflow-nav [videoId]="videoId" />

    <div class="page-header">
      <div>
        <h1>✅ Review Suggestions</h1>
        <p>Approve or reject each AI suggestion before you use it</p>
      </div>
      <a [routerLink]="['/videos', videoId, 'optimize']" class="btn btn-secondary">🤖 Generate more</a>
    </div>

    <div class="notice">
      <span class="notice-icon" aria-hidden="true">⚠️</span>
      <div>
        <div class="notice-title">Human review required</div>
        <p class="notice-body">
          Nothing here is published automatically. Approving a suggestion only marks it as reviewed by you —
          you still copy it and apply it to YouTube manually.
        </p>
      </div>
    </div>

    <div *ngIf="loading" class="loading" role="status">Loading suggestions…</div>
    <div *ngIf="error" class="error-banner" role="alert">
      <span>{{error}}</span>
      <button type="button" class="btn btn-secondary btn-sm" (click)="load()">Retry</button>
    </div>

    <!-- Approve/reject failures must never hide the list the user is reviewing -->
    <div *ngIf="actionError" class="error-banner" role="alert">
      <span>{{actionError}}</span>
      <button type="button" class="btn btn-icon btn-sm" aria-label="Dismiss message" (click)="actionError = ''">✕</button>
    </div>

    <ng-container *ngIf="!loading && !error">
      <!-- Progress, counts and filters -->
      <div class="card summary" *ngIf="suggestions.length > 0">
        <div class="progress-head">
          <strong>{{reviewedCount}} of {{suggestions.length}} reviewed</strong>
          <span class="text-meta" *ngIf="pendingCount > 0">{{pendingCount}} still to judge</span>
          <span class="text-meta" *ngIf="pendingCount === 0">Nothing left to judge</span>
        </div>
        <div class="progress" role="progressbar" aria-label="Review progress"
             [attr.aria-valuenow]="reviewedCount" aria-valuemin="0" [attr.aria-valuemax]="suggestions.length">
          <div class="progress-bar" [style.width.%]="reviewedPercent"></div>
        </div>

        <div class="stats-grid">
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

        <div class="filter-row">
          <span class="filter-label" id="status-filter-label">Status</span>
          <div class="filter-bar" role="group" aria-labelledby="status-filter-label">
            <button type="button" *ngFor="let f of filters"
                    class="btn btn-sm"
                    [class.btn-primary]="filter === f.key"
                    [class.btn-secondary]="filter !== f.key"
                    [attr.aria-pressed]="filter === f.key"
                    (click)="setFilter(f.key)">
              {{f.label}} ({{countFor(f.key)}})
            </button>
          </div>
        </div>

        <div class="filter-row" *ngIf="typeOptions.length > 1">
          <span class="filter-label" id="type-filter-label">Type</span>
          <div class="filter-bar" role="group" aria-labelledby="type-filter-label">
            <button type="button" class="btn btn-sm"
                    [class.btn-primary]="typeFilter === 'all'"
                    [class.btn-secondary]="typeFilter !== 'all'"
                    [attr.aria-pressed]="typeFilter === 'all'"
                    (click)="typeFilter = 'all'">All types</button>
            <button type="button" *ngFor="let t of typeOptions"
                    class="btn btn-sm"
                    [class.btn-primary]="typeFilter === t.type"
                    [class.btn-secondary]="typeFilter !== t.type"
                    [attr.aria-pressed]="typeFilter === t.type"
                    (click)="typeFilter = t.type">
              <span aria-hidden="true">{{t.icon}}</span> {{t.label}} ({{t.count}})
            </button>
          </div>
        </div>

        <div class="filter-row" *ngIf="visibleGroups.length > 1">
          <button type="button" class="btn btn-secondary btn-sm" (click)="toggleAllGroups()">
            {{allCollapsed ? '▾ Expand all sections' : '▸ Collapse all sections'}}
          </button>
        </div>
      </div>

      <!-- Finished: the loop continues on the promotion drafts screen -->
      <div class="card next-step" *ngIf="allReviewed">
        <div>
          <strong>🎉 All {{suggestions.length}} suggestions reviewed</strong>
          <div class="text-muted next-step-body">
            {{approvedCount}} approved, {{rejectedCount}} rejected. Next: turn the approved material into
            per-platform promotion drafts — you still review and post those by hand.
          </div>
        </div>
        <a [routerLink]="['/videos', videoId, 'promotions']" class="btn btn-primary">📢 Go to promotion drafts</a>
      </div>

      <!-- Empty states -->
      <div *ngIf="suggestions.length === 0" class="empty-state card">
        <div class="empty-icon" aria-hidden="true">✨</div>
        <p>No suggestions yet for this video.</p>
        <a [routerLink]="['/videos', videoId, 'optimize']" class="btn btn-primary">
          🤖 Run the AI optimizer
        </a>
      </div>

      <div *ngIf="showFilteredEmpty" class="empty-state card">
        <div class="empty-icon" aria-hidden="true">🔍</div>
        <p>Nothing matches these filters.</p>
        <button type="button" class="btn btn-secondary" (click)="resetFilters()">Clear filters</button>
      </div>

      <!-- Grouped suggestions -->
      <div *ngFor="let group of visibleGroups; trackBy: trackGroup" class="card group-card">
        <button type="button" class="group-head" (click)="toggleGroup(group.type)"
                [attr.aria-expanded]="!isCollapsed(group.type)">
          <span class="group-title">
            <span aria-hidden="true">{{group.icon}}</span> {{group.label}}
          </span>
          <span class="group-meta">
            <span class="badge badge-draft" *ngIf="group.pending > 0">{{group.pending}} pending</span>
            <span class="badge badge-approved" *ngIf="group.pending === 0">reviewed</span>
            <span class="text-meta">{{group.items.length}} shown</span>
            <span class="chevron" aria-hidden="true">{{isCollapsed(group.type) ? '▸' : '▾'}}</span>
          </span>
        </button>

        <div *ngIf="!isCollapsed(group.type)" class="group-body">
          <div *ngFor="let s of group.items; trackBy: trackSuggestion" class="suggestion-item">
            <div class="suggestion-body">
              <!-- Shorts ideas are stored as JSON; render them structured -->
              <div *ngIf="asShortsIdea(s) as idea; else plainText">
                <div class="idea-title">{{idea.title}}</div>
                <div class="idea-field" *ngIf="idea.hook"><strong>Hook:</strong> {{idea.hook}}</div>
                <div class="idea-field" *ngIf="idea.suggestedTimestamp"><strong>Timestamp:</strong> {{idea.suggestedTimestamp}}</div>
                <div class="idea-field" *ngIf="idea.caption"><strong>Caption:</strong> {{idea.caption}}</div>
              </div>
              <ng-template #plainText>
                <div class="suggestion-text" [class.clamped]="isClamped(s)">{{s.suggestionText}}</div>
                <button type="button" class="link-btn" *ngIf="isLong(s)" (click)="toggleExpand(s)">
                  {{expandedIds.has(s.id) ? 'Show less' : 'Show more'}}
                </button>
              </ng-template>

              <div class="suggestion-tags">
                <span class="badge" [ngClass]="statusClass(s)">{{statusLabel(s)}}</span>
                <span *ngIf="s.platform" class="chip">{{s.platform}}</span>
                <span class="text-meta">{{s.createdAt | date:'short'}}</span>
              </div>
            </div>

            <div class="suggestion-actions">
              <button type="button" class="btn btn-icon btn-sm"
                      [class.copy-success]="copiedId === s.id"
                      [attr.aria-label]="'Copy: ' + shortLabel(s)"
                      (click)="copy(s)">{{copiedId === s.id ? '✓' : '📋'}}</button>
              <button type="button" class="btn btn-success btn-sm"
                      [disabled]="s.isApproved || busyIds.has(s.id)"
                      [attr.aria-label]="'Approve: ' + shortLabel(s)"
                      (click)="approve(s)">{{busyIds.has(s.id) ? '…' : '✓ Approve'}}</button>
              <button type="button" class="btn btn-danger btn-sm"
                      [disabled]="s.isRejected || busyIds.has(s.id)"
                      [attr.aria-label]="'Reject: ' + shortLabel(s)"
                      (click)="reject(s)">{{busyIds.has(s.id) ? '…' : '✗ Reject'}}</button>
            </div>
          </div>
        </div>
      </div>
    </ng-container>

    <p class="sr-only" role="status" aria-live="polite">{{liveMessage}}</p>
  `,
  styles: [`
    .summary { margin-bottom: 1.25rem; }
    .summary .stats-grid { margin: 1.1rem 0 0.25rem; }
    .group-card { margin-bottom: 1.25rem; padding: 0; }
    .group-head {
      display: flex; justify-content: space-between; align-items: center;
      gap: 1rem; flex-wrap: wrap;
      width: 100%; padding: 1.1rem 1.5rem; background: none; border: none;
      font: inherit; text-align: left; cursor: pointer; border-radius: 12px;
    }
    .group-head:hover { background: #fafbfc; }
    .group-title { font-size: 1.1rem; font-weight: 600; color: #1a1a2e; }
    .group-meta { display: flex; align-items: center; gap: 0.6rem; flex-wrap: wrap; }
    .chevron { color: #62687a; }
    .group-body { padding: 0 1.5rem 0.75rem; }
    .suggestion-item {
      display: flex; align-items: flex-start; justify-content: space-between;
      gap: 1rem; padding: 0.85rem 0; border-top: 1px solid #f0f2f5;
    }
    .suggestion-body { flex: 1; min-width: 0; }
    .suggestion-text { white-space: pre-line; font-size: 0.9rem; overflow-wrap: anywhere; }
    .suggestion-text.clamped {
      display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .link-btn { margin-top: 0.25rem; }
    .idea-title { font-weight: 600; }
    .idea-field { font-size: 0.85rem; color: #4a4f5a; margin-top: 0.15rem; }
    .suggestion-tags {
      margin-top: 0.45rem; display: flex; gap: 0.5rem;
      align-items: center; flex-wrap: wrap;
    }
    .suggestion-tags .chip { margin: 0; }
    .suggestion-actions {
      display: flex; gap: 0.4rem; flex-shrink: 0; flex-wrap: wrap; justify-content: flex-end;
    }
    @media (max-width: 640px) {
      .group-head, .group-body { padding-left: 1rem; padding-right: 1rem; }
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
  typeFilter = 'all';
  busyIds = new Set<number>();
  expandedIds = new Set<number>();
  collapsedGroups = new Set<string>();
  copiedId: number | null = null;
  liveMessage = '';

  // asShortsIdea() is called from the template, so memoise it: returning a fresh
  // object on every change-detection pass would thrash the *ngIf "as" binding.
  private shortsCache = new Map<number, ShortsIdeaPayload | null>();
  private copyTimer: ReturnType<typeof setTimeout> | null = null;

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
      next: s => {
        this.suggestions = s;
        this.loading = false;
        // A returning user has already judged some of these; open on what is
        // left rather than on a wall of 24 rows they have mostly handled.
        const pending = s.filter(x => !x.isApproved && !x.isRejected).length;
        this.filter = pending > 0 && pending < s.length ? 'pending' : 'all';
      },
      error: err => { this.error = this.friendlyError(err); this.loading = false; }
    });
  }

  // --- counts -------------------------------------------------------------

  get pendingCount(): number {
    return this.suggestions.filter(s => !s.isApproved && !s.isRejected).length;
  }

  get approvedCount(): number {
    return this.suggestions.filter(s => s.isApproved).length;
  }

  get rejectedCount(): number {
    return this.suggestions.filter(s => s.isRejected).length;
  }

  get reviewedCount(): number {
    return this.suggestions.length - this.pendingCount;
  }

  get reviewedPercent(): number {
    if (this.suggestions.length === 0) return 0;
    return Math.round((this.reviewedCount / this.suggestions.length) * 100);
  }

  get allReviewed(): boolean {
    return this.suggestions.length > 0 && this.pendingCount === 0;
  }

  countFor(key: StatusFilter): number {
    switch (key) {
      case 'pending': return this.pendingCount;
      case 'approved': return this.approvedCount;
      case 'rejected': return this.rejectedCount;
      default: return this.suggestions.length;
    }
  }

  // --- filtering / grouping ----------------------------------------------

  private statusMatches(s: SuggestionDto): boolean {
    switch (this.filter) {
      case 'pending': return !s.isApproved && !s.isRejected;
      case 'approved': return s.isApproved;
      case 'rejected': return s.isRejected;
      default: return true;
    }
  }

  private metaFor(type: string): { type: string; icon: string; label: string } {
    return this.groupMeta.find(m => m.type === type) ?? { type, icon: '💡', label: type || 'Other' };
  }

  /** Types present under the current status filter, with counts. */
  get typeOptions(): { type: string; icon: string; label: string; count: number }[] {
    const counts = new Map<string, number>();
    for (const s of this.suggestions) {
      if (!this.statusMatches(s)) continue;
      const t = s.suggestionType || 'Other';
      counts.set(t, (counts.get(t) ?? 0) + 1);
    }
    const order = this.groupMeta.map(m => m.type);
    return [...counts.entries()]
      .sort((a, b) => {
        const ai = order.indexOf(a[0]);
        const bi = order.indexOf(b[0]);
        return (ai < 0 ? 99 : ai) - (bi < 0 ? 99 : bi);
      })
      .map(([type, count]) => ({ ...this.metaFor(type), count }));
  }

  get visibleGroups(): SuggestionGroup[] {
    const filtered = this.suggestions.filter(
      s => this.statusMatches(s) && (this.typeFilter === 'all' || (s.suggestionType || 'Other') === this.typeFilter)
    );

    const groups: SuggestionGroup[] = [];
    const seen = new Set<string>();

    const push = (type: string) => {
      if (seen.has(type)) return;
      seen.add(type);
      const items = filtered.filter(s => (s.suggestionType || 'Other') === type);
      if (items.length === 0) return;
      groups.push({
        ...this.metaFor(type),
        items,
        // Pending is counted across the whole type, not just the filtered slice,
        // so the header still tells the truth while a status filter is active.
        pending: this.suggestions.filter(
          s => (s.suggestionType || 'Other') === type && !s.isApproved && !s.isRejected
        ).length
      });
    };

    for (const meta of this.groupMeta) push(meta.type);
    // Anything with an unrecognised type still gets shown rather than silently dropped
    for (const s of filtered) push(s.suggestionType || 'Other');

    return groups;
  }

  get showFilteredEmpty(): boolean {
    return this.suggestions.length > 0 && this.visibleGroups.length === 0 && !this.allReviewed;
  }

  setFilter(key: StatusFilter): void {
    this.filter = key;
    // A type that had rows under the old status filter may have none under the
    // new one; resetting avoids landing on an empty screen for no clear reason.
    this.typeFilter = 'all';
  }

  resetFilters(): void {
    this.filter = 'all';
    this.typeFilter = 'all';
  }

  // --- collapsing ---------------------------------------------------------

  isCollapsed(type: string): boolean { return this.collapsedGroups.has(type); }

  toggleGroup(type: string): void {
    if (this.collapsedGroups.has(type)) this.collapsedGroups.delete(type);
    else this.collapsedGroups.add(type);
  }

  get allCollapsed(): boolean {
    const groups = this.visibleGroups;
    return groups.length > 0 && groups.every(g => this.collapsedGroups.has(g.type));
  }

  toggleAllGroups(): void {
    if (this.allCollapsed) this.collapsedGroups.clear();
    else for (const g of this.visibleGroups) this.collapsedGroups.add(g.type);
  }

  // --- long text ----------------------------------------------------------

  isLong(s: SuggestionDto): boolean {
    return !this.asShortsIdea(s) && (s.suggestionText?.length ?? 0) > CLAMP_AT;
  }

  isClamped(s: SuggestionDto): boolean {
    return this.isLong(s) && !this.expandedIds.has(s.id);
  }

  toggleExpand(s: SuggestionDto): void {
    if (this.expandedIds.has(s.id)) this.expandedIds.delete(s.id);
    else this.expandedIds.add(s.id);
  }

  // --- actions ------------------------------------------------------------

  approve(s: SuggestionDto): void {
    this.actionError = '';
    this.busyIds.add(s.id);
    this.optimizationService.approveSuggestion(this.videoId, s.id).subscribe({
      next: updated => {
        this.replace(updated);
        this.busyIds.delete(s.id);
        this.liveMessage = `Approved. ${this.pendingCount} suggestions left to review.`;
      },
      error: err => { this.actionError = this.actionFailed('approve', err); this.busyIds.delete(s.id); }
    });
  }

  reject(s: SuggestionDto): void {
    this.actionError = '';
    this.busyIds.add(s.id);
    this.optimizationService.rejectSuggestion(this.videoId, s.id).subscribe({
      next: updated => {
        this.replace(updated);
        this.busyIds.delete(s.id);
        this.liveMessage = `Rejected. ${this.pendingCount} suggestions left to review.`;
      },
      error: err => { this.actionError = this.actionFailed('reject', err); this.busyIds.delete(s.id); }
    });
  }

  private replace(updated: SuggestionDto): void {
    const idx = this.suggestions.findIndex(s => s.id === updated.id);
    if (idx >= 0) this.suggestions[idx] = updated;
  }

  trackGroup = (_: number, g: SuggestionGroup) => g.type;
  trackSuggestion = (_: number, s: SuggestionDto) => s.id;

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

  /** Short, stable name for a row — used for per-button accessible labels. */
  shortLabel(s: SuggestionDto): string {
    const idea = this.asShortsIdea(s);
    const text = (idea?.title || s.suggestionText || '').replace(/\s+/g, ' ').trim();
    return text.length > 60 ? `${text.slice(0, 60)}…` : text;
  }

  copy(s: SuggestionDto): void {
    const idea = this.asShortsIdea(s);
    const text = idea ? (idea.caption || idea.title || s.suggestionText) : s.suggestionText;
    navigator.clipboard.writeText(text).then(
      () => this.flagCopied(s.id, 'Copied to clipboard.'),
      () => { this.actionError = 'Could not copy to the clipboard. Select the text and copy it manually.'; }
    );
  }

  private flagCopied(id: number, message: string): void {
    this.copiedId = id;
    this.liveMessage = message;
    if (this.copyTimer) clearTimeout(this.copyTimer);
    this.copyTimer = setTimeout(() => { this.copiedId = null; }, 1600);
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
