import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiClientService } from '../../services/api-client.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-header">
      <div>
        <h1>⚙️ Settings</h1>
        <p>Feature flags and configuration status</p>
      </div>
    </div>

    <div class="card-grid">
      <div class="card">
        <h2 class="card-title">🔌 API connection</h2>
        <div class="setting-row">
          <span>API URL</span>
          <code>{{apiUrl}}</code>
        </div>
        <div class="setting-row">
          <span>Backend status</span>
          <!-- "Offline" while the check is still in flight would be a lie -->
          <span class="badge" [ngClass]="healthClass">{{healthLabel}}</span>
        </div>
        <button type="button" class="btn btn-secondary btn-sm recheck"
                (click)="checkHealth()" [disabled]="checking">
          {{checking ? '⏳ Checking…' : '↻ Re-check'}}
        </button>
      </div>

      <div class="card">
        <h2 class="card-title">🤖 AI / LLM</h2>
        <div class="setting-row">
          <span>OpenAI integration</span>
          <span class="badge badge-draft">Configured via env</span>
        </div>
        <p class="hint">
          Set <code>OpenAI__ApiKey</code> in appsettings.json.
          When empty, the app uses a deterministic mock LLM service.
        </p>
      </div>

      <div class="card">
        <h2 class="card-title">📺 YouTube integration</h2>
        <div class="setting-row">
          <span>Mock mode</span>
          <span class="badge badge-approved">✓ Active (default)</span>
        </div>
        <div class="setting-row">
          <span>Write actions</span>
          <span class="badge badge-draft">Disabled (safe)</span>
        </div>
        <p class="hint">
          Set <code>Features__UseMockYouTube=false</code> to connect real YouTube.
          <strong>Never enable write actions without full OAuth setup.</strong>
        </p>
      </div>

      <div class="card">
        <h2 class="card-title">🛡️ Compliance rules</h2>
        <ul class="rules">
          <li>No fake view/like/subscriber automation</li>
          <li>No bot engagement</li>
          <li>No sub-for-sub schemes</li>
          <li>No mass DM spam</li>
          <li>All suggestions require human review</li>
          <li>Misleading content is flagged and blocked</li>
        </ul>
      </div>

      <div class="card">
        <h2 class="card-title">🔧 Quick config</h2>
        <p class="config-label"><strong>Enable real OpenAI:</strong></p>
        <code class="code-block">OpenAI__ApiKey=sk-your-key</code>
        <p class="config-label"><strong>Connect YouTube:</strong></p>
        <p class="text-muted small">See docs/youtube-integration-notes.md for full OAuth setup.</p>
      </div>

      <div class="card">
        <h2 class="card-title">📚 Resources</h2>
        <a [href]="swaggerUrl" target="_blank" rel="noopener"
           class="btn btn-secondary btn-sm">
          📖 API documentation (Swagger)
        </a>
        <p class="hint">All endpoints documented with request/response schemas. Opens in a new tab.</p>
      </div>
    </div>
  `,
  styles: [`
    .card-title { font-size: 1.15rem; margin-bottom: 1rem; }
    .setting-row {
      display: flex; justify-content: space-between; align-items: center;
      gap: 0.75rem; padding: 0.5rem 0; border-bottom: 1px solid #f0f2f5;
      font-size: 0.9rem; flex-wrap: wrap;
    }
    .setting-row:last-of-type { border-bottom: none; }
    .setting-row code { font-size: 0.8rem; overflow-wrap: anywhere; }
    .recheck { margin-top: 0.75rem; }
    .hint {
      font-size: 0.85rem; color: #4a4f5a; margin-top: 0.75rem;
      background: #f7f8fa; border: 1px solid #e8e8ee; padding: 0.75rem; border-radius: 8px;
    }
    .rules { font-size: 0.9rem; color: #4a4f5a; margin: 0; padding-left: 1.2rem; line-height: 1.9; }
    .config-label { font-size: 0.9rem; margin-bottom: 0.5rem; }
    .config-label:not(:first-of-type) { margin-top: 1rem; }
    .code-block {
      display: block; background: #1a1a2e; color: #e6e6f0;
      padding: 0.75rem; border-radius: 8px; font-size: 0.8rem; overflow-wrap: anywhere;
    }
    .small { font-size: 0.85rem; }
  `]
})
export class SettingsComponent implements OnInit {
  apiUrl = environment.apiUrl;
  checking = true;
  apiHealthy = false;

  // Swagger sits next to the API root, not under /api.
  readonly swaggerUrl = `${environment.apiUrl.replace(/\/api\/?$/, '')}/swagger`;

  constructor(private api: ApiClientService) {}

  ngOnInit(): void {
    this.checkHealth();
  }

  checkHealth(): void {
    this.checking = true;
    this.api.get<unknown>('health').subscribe({
      next: () => { this.apiHealthy = true; this.checking = false; },
      error: () => { this.apiHealthy = false; this.checking = false; }
    });
  }

  get healthLabel(): string {
    if (this.checking) return 'Checking…';
    return this.apiHealthy ? '✓ Connected' : '✗ Offline';
  }

  get healthClass(): string {
    if (this.checking) return 'badge-draft';
    return this.apiHealthy ? 'badge-approved' : 'badge-rejected';
  }
}
