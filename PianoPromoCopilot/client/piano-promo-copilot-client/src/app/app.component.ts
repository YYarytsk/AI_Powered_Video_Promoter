import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-shell">
      <a class="skip-link" href="#main-content">Skip to main content</a>
      <header class="top-nav">
        <a class="nav-brand" routerLink="/dashboard">
          <span class="piano-icon" aria-hidden="true">🎹</span>
          <span class="brand-name">PianoPromoCopilot</span>
        </a>
        <nav class="nav-links" aria-label="Primary">
          <a routerLink="/dashboard" routerLinkActive="active" ariaCurrentWhenActive="page">Dashboard</a>
          <a routerLink="/videos" routerLinkActive="active" ariaCurrentWhenActive="page">Videos</a>
          <a routerLink="/settings" routerLinkActive="active" ariaCurrentWhenActive="page">Settings</a>
        </nav>
      </header>
      <main class="main-content" id="main-content" tabindex="-1">
        <router-outlet />
      </main>
      <footer class="app-footer">
        <span aria-hidden="true">🛡️</span>
        Every suggestion is reviewed by you. PianoPromoCopilot never posts anything on your behalf.
      </footer>
    </div>
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {}
