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

    <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(350px,1fr));gap:1.25rem;">
      <div class="card">
        <h3 style="margin-bottom:1rem;">🔌 API Connection</h3>
        <div class="setting-row">
          <span>API URL</span>
          <code style="font-size:0.8rem;">{{apiUrl}}</code>
        </div>
        <div class="setting-row">
          <span>Backend Status</span>
          <span [class]="apiHealthy ? 'badge badge-approved' : 'badge badge-rejected'">
            {{apiHealthy ? '✓ Connected' : '✗ Offline'}}
          </span>
        </div>
      </div>

      <div class="card">
        <h3 style="margin-bottom:1rem;">🤖 AI / LLM</h3>
        <div class="setting-row">
          <span>OpenAI Integration</span>
          <span class="badge badge-draft">Configured via env</span>
        </div>
        <div style="font-size:0.85rem;color:#666;margin-top:0.75rem;background:#f8f9fa;padding:0.75rem;border-radius:6px;">
          Set <code>OpenAI__ApiKey</code> in appsettings.json.
          When empty, the app uses a deterministic mock LLM service.
        </div>
      </div>

      <div class="card">
        <h3 style="margin-bottom:1rem;">📺 YouTube Integration</h3>
        <div class="setting-row">
          <span>Mock Mode</span>
          <span class="badge badge-approved">✓ Active (default)</span>
        </div>
        <div class="setting-row">
          <span>Write Actions</span>
          <span class="badge badge-draft">Disabled (safe)</span>
        </div>
        <div style="font-size:0.85rem;color:#666;margin-top:0.75rem;background:#f8f9fa;padding:0.75rem;border-radius:6px;">
          Set <code>Features__UseMockYouTube=false</code> to connect real YouTube.
          <strong>Never enable write actions without full OAuth setup.</strong>
        </div>
      </div>

      <div class="card">
        <h3 style="margin-bottom:1rem;">🛡️ Compliance Rules</h3>
        <div style="font-size:0.85rem;color:#555;line-height:1.8;">
          <div>✅ No fake view/like/subscriber automation</div>
          <div>✅ No bot engagement</div>
          <div>✅ No sub-for-sub schemes</div>
          <div>✅ No mass DM spam</div>
          <div>✅ All suggestions require human review</div>
          <div>✅ Misleading content is flagged and blocked</div>
        </div>
      </div>

      <div class="card">
        <h3 style="margin-bottom:1rem;">🔧 Quick Config</h3>
        <div style="font-size:0.85rem;color:#555;">
          <p style="margin-bottom:0.75rem;"><strong>Enable real OpenAI:</strong></p>
          <code style="display:block;background:#1a1a2e;color:#ccc;padding:0.75rem;border-radius:6px;font-size:0.8rem;">
            OpenAI__ApiKey=sk-your-key
          </code>
          <p style="margin-top:1rem;margin-bottom:0.75rem;"><strong>Connect YouTube:</strong></p>
          <div style="color:#888;">See docs/youtube-integration-notes.md for full OAuth setup.</div>
        </div>
      </div>

      <div class="card">
        <h3 style="margin-bottom:1rem;">📚 Resources</h3>
        <div style="display:flex;flex-direction:column;gap:0.5rem;">
          <a href="http://localhost:5000/swagger" target="_blank" class="btn btn-secondary btn-sm">
            📖 API Documentation (Swagger)
          </a>
          <div style="font-size:0.8rem;color:#888;margin-top:0.25rem;">
            All endpoints documented with request/response schemas.
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .setting-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.5rem 0;
      border-bottom: 1px solid #f0f2f5;
      font-size: 0.9rem;
      &:last-child { border-bottom: none; }
    }
  `]
})
export class SettingsComponent implements OnInit {
  apiUrl = environment.apiUrl;
  apiHealthy = false;

  constructor(private api: ApiClientService) {}

  ngOnInit(): void {
    this.api.get<any>('health').subscribe({
      next: () => this.apiHealthy = true,
      error: () => this.apiHealthy = false
    });
  }
}
