import { Component, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe, PercentPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AnalyticsService, AnalyticsSnapshotDto, AnalyticsRecommendationDto } from '../../services/analytics.service';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page-header">
      <div>
        <a [routerLink]="['/videos', videoId]" style="color:#6c63ff;text-decoration:none;font-size:0.9rem;">← Back to Video</a>
        <h1 style="margin-top:0.5rem;">📊 Analytics</h1>
        <p>Performance snapshots and AI recommendations</p>
      </div>
      <div style="display:flex;gap:0.75rem;">
        <button class="btn btn-secondary" (click)="addMockSnapshot()" [disabled]="addingMock">
          {{addingMock ? '⏳...' : '➕ Add Mock Snapshot'}}
        </button>
        <button class="btn btn-primary" (click)="loadRecommendations()">💡 Get Recommendations</button>
      </div>
    </div>

    <div *ngIf="loading" class="loading">Loading analytics...</div>
    <div *ngIf="error" class="error-banner">{{error}}</div>

    <div *ngIf="recommendations.length > 0" style="margin-bottom:1.5rem;">
      <h3 style="margin-bottom:1rem;">💡 AI Recommendations</h3>
      <div *ngFor="let rec of recommendations" class="card" style="margin-bottom:0.75rem;border-left:4px solid #6c63ff;">
        <div style="display:flex;justify-content:space-between;">
          <strong>{{rec.message}}</strong>
          <span style="font-size:0.8rem;color:#888;">Priority {{rec.priority}}</span>
        </div>
        <p style="color:#555;font-size:0.9rem;margin-top:0.5rem;">{{rec.reason}}</p>
        <div style="background:#f0eeff;padding:0.75rem;border-radius:6px;margin-top:0.5rem;font-size:0.85rem;">
          <strong>Action:</strong> {{rec.suggestedAction}}
        </div>
      </div>
    </div>

    <div *ngIf="!loading && !error">
      <div *ngIf="snapshots.length === 0" class="empty-state card">
        <div class="empty-icon">📊</div>
        <p>No analytics data yet. Click "Add Mock Snapshot" to generate test data.</p>
      </div>

      <div *ngIf="snapshots.length > 0">
        <h3 style="margin-bottom:1rem;">📈 Recent Snapshots</h3>
        <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(320px,1fr));gap:1rem;">
          <div *ngFor="let snap of snapshots" class="card">
            <div style="display:flex;justify-content:space-between;margin-bottom:0.75rem;">
              <strong>{{snap.snapshotDate | date:'mediumDate'}}</strong>
              <span style="font-size:0.8rem;background:#f0eeff;color:#6c63ff;padding:0.2rem 0.5rem;border-radius:4px;">
                {{snap.trafficSource || 'Unknown'}}
              </span>
            </div>
            <div class="stats-grid" style="margin-bottom:0;">
              <div class="stat-card" style="padding:0.75rem;">
                <div class="stat-value" style="font-size:1.4rem;">{{snap.views | number}}</div>
                <div class="stat-label">Views</div>
              </div>
              <div class="stat-card" style="padding:0.75rem;">
                <div class="stat-value" style="font-size:1.4rem;">{{snap.likes | number}}</div>
                <div class="stat-label">Likes</div>
              </div>
              <div class="stat-card" style="padding:0.75rem;">
                <div class="stat-value" style="font-size:1.4rem;">{{snap.comments | number}}</div>
                <div class="stat-label">Comments</div>
              </div>
            </div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:0.5rem;margin-top:0.75rem;font-size:0.85rem;">
              <div style="background:#f8f9fa;padding:0.5rem;border-radius:6px;">
                <span style="color:#888;">Avg Duration:</span><br>
                <strong>{{formatDuration(snap.averageViewDurationSeconds)}}</strong>
              </div>
              <div style="background:#f8f9fa;padding:0.5rem;border-radius:6px;">
                <span style="color:#888;">CTR:</span><br>
                <strong>{{((snap.impressionClickThroughRate || 0) * 100).toFixed(1)}}%</strong>
              </div>
              <div style="background:#f8f9fa;padding:0.5rem;border-radius:6px;">
                <span style="color:#888;">Retention:</span><br>
                <strong>{{((snap.averageViewPercentage || 0) * 100).toFixed(0)}}%</strong>
              </div>
              <div style="background:#f8f9fa;padding:0.5rem;border-radius:6px;">
                <span style="color:#888;">Country:</span><br>
                <strong>{{snap.country || '—'}}</strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AnalyticsComponent implements OnInit {
  videoId = '';
  snapshots: AnalyticsSnapshotDto[] = [];
  recommendations: AnalyticsRecommendationDto[] = [];
  loading = true;
  error = '';
  addingMock = false;

  constructor(private route: ActivatedRoute, private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.videoId = this.route.snapshot.paramMap.get('id')!;
    this.analyticsService.getAnalytics(this.videoId).subscribe({
      next: snaps => { this.snapshots = snaps; this.loading = false; },
      error: err => { this.error = err.message; this.loading = false; }
    });
  }

  loadRecommendations(): void {
    this.analyticsService.getRecommendations(this.videoId).subscribe({
      next: recs => { this.recommendations = recs; },
      error: err => { this.error = err.message; }
    });
  }

  addMockSnapshot(): void {
    this.addingMock = true;
    this.analyticsService.createMockAnalytics(this.videoId).subscribe({
      next: snap => { this.snapshots.unshift(snap); this.addingMock = false; },
      error: err => { this.error = err.message; this.addingMock = false; }
    });
  }

  formatDuration(seconds?: number): string {
    if (!seconds) return '—';
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }
}
