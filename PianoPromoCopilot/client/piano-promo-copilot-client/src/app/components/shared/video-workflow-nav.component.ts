import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface WorkflowStep {
  path: string[];
  icon: string;
  label: string;
  step?: number;
  exact: boolean;
}

/**
 * Lateral navigation for the per-video screens.
 *
 * The top nav only carries Dashboard / Videos / Settings, so before this the
 * only way between optimize -> suggestions -> promotions was to back out to the
 * video and start again. The flow is a sequence, so it is drawn as one: the
 * three numbered steps are the spine, Overview and Analytics sit either side.
 */
@Component({
  selector: 'app-video-workflow-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <nav class="workflow-nav" aria-label="Video workflow">
      <a class="back-link" routerLink="/videos">← All videos</a>
      <ol class="workflow-steps">
        <li *ngFor="let s of steps">
          <a [routerLink]="s.path"
             routerLinkActive="active"
             [routerLinkActiveOptions]="{ exact: s.exact }"
             ariaCurrentWhenActive="page">
            <span class="step-num" *ngIf="s.step" aria-hidden="true">{{s.step}}</span>
            <span aria-hidden="true">{{s.icon}}</span>
            <span class="step-label">{{s.label}}</span>
          </a>
        </li>
      </ol>
    </nav>
  `,
  styles: [`
    .workflow-nav {
      display: flex;
      flex-direction: column;
      gap: 0.6rem;
      margin-bottom: 1.25rem;
    }
    .workflow-steps {
      display: flex;
      gap: 0.35rem;
      list-style: none;
      margin: 0;
      padding: 0.25rem;
      background: white;
      border: 1px solid #e8e8e8;
      border-radius: 10px;
      overflow-x: auto;
    }
    .workflow-steps a {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      white-space: nowrap;
      padding: 0.45rem 0.75rem;
      border-radius: 7px;
      font-size: 0.88rem;
      font-weight: 500;
      color: #4a4f5a;
      text-decoration: none;
      transition: background 0.15s, color 0.15s;
    }
    .workflow-steps a:hover { background: #f0f2f5; color: #1a1a2e; }
    .workflow-steps a.active { background: #f0eeff; color: #4338ca; box-shadow: inset 0 0 0 1px #d7d3ff; }
    .step-num {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 18px;
      height: 18px;
      border-radius: 50%;
      background: #e6e6ee;
      color: #4a4f5a;
      font-size: 0.7rem;
      font-weight: 700;
    }
    .workflow-steps a.active .step-num { background: #5a52e0; color: white; }
    @media (max-width: 640px) {
      /* Clipped rather than display:none — the icons are aria-hidden, so the
         label has to stay in the accessibility tree as the link's name. */
      .step-label {
        position: absolute; width: 1px; height: 1px;
        overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap;
      }
      .workflow-steps a { padding: 0.5rem 0.7rem; font-size: 1rem; }
    }
  `]
})
export class VideoWorkflowNavComponent implements OnChanges {
  @Input({ required: true }) videoId = '';

  // Built once per videoId, never from a template-called getter. A getter here
  // hands *ngFor a fresh array of fresh objects on every change-detection pass,
  // so the differ tears down and rebuilds all five links each cycle; the
  // routerLink/routerLinkActive directives inside then re-subscribe and write
  // back, and change detection never settles — it hangs the page.
  steps: WorkflowStep[] = [];

  ngOnChanges(): void {
    const base = ['/videos', this.videoId];
    this.steps = [
      { path: base, icon: '🎬', label: 'Overview', exact: true },
      { path: [...base, 'optimize'], icon: '🤖', label: 'Optimize', step: 1, exact: false },
      { path: [...base, 'suggestions'], icon: '✅', label: 'Review', step: 2, exact: false },
      { path: [...base, 'promotions'], icon: '📢', label: 'Promote', step: 3, exact: false },
      { path: [...base, 'analytics'], icon: '📊', label: 'Analytics', exact: false }
    ];
  }
}
