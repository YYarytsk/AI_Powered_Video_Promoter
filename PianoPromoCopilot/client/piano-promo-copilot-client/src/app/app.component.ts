import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-shell">
      <nav class="top-nav">
        <div class="nav-brand">
          <span class="piano-icon">🎹</span>
          <span class="brand-name">PianoPromoCopilot</span>
        </div>
        <div class="nav-links">
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/videos" routerLinkActive="active">Videos</a>
          <a routerLink="/settings" routerLinkActive="active">Settings</a>
        </div>
      </nav>
      <main class="main-content">
        <router-outlet />
      </main>
    </div>
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {}
