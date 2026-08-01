import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { AnalyticsService, AnalyticsSnapshotDto, AnalyticsRecommendationDto } from '../../services/analytics.service';
import { VideoWorkflowNavComponent } from '../shared/video-workflow-nav.component';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, VideoWorkflowNavComponent],
  template: `
    <app-video-workflow-nav [videoId]="videoId" />

    <div class="page-header">
      <div>
        <h1>📊 Analytics</h1>
        <p>Performance snapshots and AI recommendations</p>
      </div>
      <div class="toolbar">
        <button type="button" class="btn btn-secondary" (click)="addMockSnapshot()" [disabled]="addingMock">
          {{addingMock ? '⏳ Adding…' : '➕ Add mock snapshot'}}
        </button>
        <button type="button" class="btn btn-primary" (click)="loadRecommendations()" [disabled]="loadingRecs">
          {{loadingRecs ? '⏳ Thinking…' : '💡 Get recommendations'}}
        </button>
      </div>
    </div>

    <div *ngIf="loading" class="loading" role="status">Loading analytics…</div>

    <!-- Load failure: the page has nothing to show, so offer a retry -->
    <div *ngIf="error" class="error-banner" role="alert">
      <span>{{error}}</span>
      <button type="button" class="btn btn-secondary btn-sm" (click)="load()">Retry</button>
    </div>

    <!-- Action failure: the snapshots below stay on screen -->
    <div *ngIf="actionError" class="error-banner" role="alert">
      <span>{{actionError}}</span>
      <button type="button" class="btn btn-icon btn-sm" aria-label="Dismiss message" (click)="actionError = ''">✕</button>
    </div>

    <div *ngIf="loadingRecs" class="loading loading-inline" role="status">Reading your numbers…</div>

    <div *ngIf="recsLoaded && recommendations.length === 0 && !loadingRecs" class="notice">
      <span class="notice-icon" aria-hidden="true">💡</span>
      <div>
        <div class="notice-title">No recommendations right now</div>
        <p class="notice-body">
          There isn't enough of a signal in these snapshots yet. Add more data over time and check again.
        </p>
      </div>
    </div>

    <section *ngIf="recommendations.length > 0" class="recs">
      <h2>💡 AI recommendations</h2>
      <p class="text-muted recs-note">Suggestions only — decide for yourself which are worth acting on.</p>
      <div *ngFor="let rec of recommendations" class="card rec-card">
        <div class="rec-head">
          <strong>{{rec.message}}</strong>
          <span class="badge" [ngClass]="priorityClass(rec.priority)">{{priorityLabel(rec.priority)}} priority</span>
        </div>
        <p class="rec-reason">{{rec.reason}}</p>
        <div class="rec-action"><strong>Action:</strong> {{rec.suggestedAction}}</div>
      </div>
    </section>

    <div *ngIf="!loading && !error">
      <div *ngIf="snapshots.length === 0" class="empty-state card">
        <div class="empty-icon" aria-hidden="true">📊</div>
        <p>No analytics data yet. Add a mock snapshot to see how this screen reads with real numbers.</p>
        <button type="button" class="btn btn-primary" (click)="addMockSnapshot()" [disabled]="addingMock">
          {{addingMock ? '⏳ Adding…' : '➕ Add mock snapshot'}}
        </button>
      </div>

      <div *ngIf="snapshots.length > 0">
        <h2 class="snapshots-title">📈 Recent snapshots</h2>
        <div class="card-grid">
          <div *ngFor="let snap of snapshots; trackBy: trackSnapshot" class="card">
            <div class="snap-head">
              <strong>{{snap.snapshotDate | date:'mediumDate'}}</strong>
              <span class="chip">{{snap.trafficSource || 'Unknown source'}}</span>
            </div>
            <div class="stats-grid snap-stats">
              <div class="stat-card">
                <div class="stat-value">{{snap.views | number}}</div>
                <div class="stat-label">Views</div>
              </div>
              <div class="stat-card">
                <div class="stat-value">{{snap.likes | number}}</div>
                <div class="stat-label">Likes</div>
              </div>
              <div class="stat-card">
                <div class="stat-value">{{snap.comments | number}}</div>
                <div class="stat-label">Comments</div>
              </div>
            </div>
            <dl class="snap-detail">
              <div><dt>Avg duration</dt><dd>{{formatDuration(snap.averageViewDurationSeconds)}}</dd></div>
              <div><dt>CTR</dt><dd>{{percent(snap.impressionClickThroughRate, 1)}}</dd></div>
              <div><dt>Retention</dt><dd>{{percent(snap.averageViewPercentage, 0)}}</dd></div>
              <div><dt>Country</dt><dd>{{snap.country || '—'}}</dd></div>
            </dl>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .recs { margin-bottom: 1.5rem; }
    .recs h2 { font-size: 1.25rem; }
    .recs-note { font-size: 0.85rem; margin-bottom: 0.85rem; }
    .rec-card { margin-bottom: 0.75rem; border-left: 4px solid #6c63ff; }
    .rec-head {
      display: flex; justify-content: space-between; align-items: flex-start;
      gap: 0.75rem; flex-wrap: wrap;
    }
    .rec-reason { color: #4a4f5a; font-size: 0.9rem; margin-top: 0.5rem; }
    .rec-action {
      background: #f0eeff; padding: 0.75rem; border-radius: 8px;
      margin-top: 0.5rem; font-size: 0.85rem;
    }
    .snapshots-title { font-size: 1.25rem; margin-bottom: 1rem; }
    .snap-head {
      display: flex; justify-content: space-between; align-items: center;
      gap: 0.75rem; margin-bottom: 0.75rem; flex-wrap: wrap;
    }
    .snap-head .chip { margin: 0; }
    .snap-stats { margin-bottom: 0; }
    .snap-stats .stat-card { padding: 0.75rem; }
    .snap-stats .stat-value { font-size: 1.4rem; }
    .snap-detail {
      display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem;
      margin-top: 0.75rem; font-size: 0.85rem;
    }
    .snap-detail > div { background: #f7f8fa; padding: 0.5rem; border-radius: 8px; }
    .snap-detail dt { color: #5c6270; }
    .snap-detail dd { margin: 0; font-weight: 600; }
  `]
})
export class AnalyticsComponent implements OnInit {
  videoId = '';
  snapshots: AnalyticsSnapshotDto[] = [];
  recommendations: AnalyticsRecommendationDto[] = [];
  loading = true;
  error = '';        // load failures
  actionError = '';  // mock-snapshot / recommendation failures
  addingMock = false;
  loadingRecs = false;
  recsLoaded = false;

  constructor(private route: ActivatedRoute, private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.videoId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.analyticsService.getAnalytics(this.videoId).subscribe({
      next: snaps => { this.snapshots = snaps; this.loading = false; },
      error: err => { this.error = this.failed('load the analytics', err); this.loading = false; }
    });
  }

  loadRecommendations(): void {
    this.loadingRecs = true;
    this.actionError = '';
    this.analyticsService.getRecommendations(this.videoId).subscribe({
      next: recs => { this.recommendations = recs; this.loadingRecs = false; this.recsLoaded = true; },
      // A failed recommendation call must not blank out the snapshots below it.
      error: err => { this.actionError = this.failed('get recommendations', err); this.loadingRecs = false; }
    });
  }

  addMockSnapshot(): void {
    this.addingMock = true;
    this.actionError = '';
    this.analyticsService.createMockAnalytics(this.videoId).subscribe({
      next: snap => { this.snapshots.unshift(snap); this.addingMock = false; },
      error: err => { this.actionError = this.failed('add a mock snapshot', err); this.addingMock = false; }
    });
  }

  private failed(what: string, err: unknown): string {
    const e = err as { status?: number; error?: { message?: string }; message?: string };
    if (e?.status === 0) return `Could not ${what}. Cannot reach the API. Is the backend running on http://localhost:5000?`;
    if (e?.status === 404) return `Could not ${what}: this video was not found.`;
    return `Could not ${what}. ${e?.error?.message || e?.message || 'Something went wrong.'}`;
  }

  trackSnapshot = (_: number, s: AnalyticsSnapshotDto) => s.id;

  formatDuration(seconds?: number): string {
    if (!seconds) return '—';
    const m = Math.floor(seconds / 60);
    const s = Math.round(seconds % 60);
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  percent(fraction: number | undefined, digits: number): string {
    if (fraction === undefined || fraction === null) return '—';
    return `${(fraction * 100).toFixed(digits)}%`;
  }

  // Priority arrives as a number; render it as words so it is not a bare rank
  // and so the badge colour is not the only signal.
  priorityLabel(priority: number): string {
    if (priority <= 1) return 'High';
    if (priority === 2) return 'Medium';
    return 'Low';
  }

  priorityClass(priority: number): string {
    if (priority <= 1) return 'badge-high';
    if (priority === 2) return 'badge-medium';
    return 'badge-low';
  }
}
