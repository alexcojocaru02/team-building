import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';

@Component({
  standalone: true,
  selector: 'app-side-panel',
  imports: [RouterModule, MatIconModule],
  templateUrl: './side-panel.component.html',
  styleUrls: ['./side-panel.component.scss']
})
export class SidePanelComponent {
  collapsed = false;

  items = [
    { icon: 'home', label: 'Home', route: '/home' },
    { icon: 'dynamic_feed', label: 'Feed', route: '/feed' },
    { icon: 'forum', label: 'Feedback', route: '/feedback' },
    { icon: 'insights', label: 'Dashboard', route: '/dashboard' }
  ];

  constructor() {
    try {
      this.collapsed = localStorage.getItem('sideCollapsed') === 'true';
      this.updateCssVar();
    } catch (e) {
      // Ignore localStorage access errors (private browsing mode, disabled storage, or quota exceeded).
    }
  }

  toggle(): void {
    this.collapsed = !this.collapsed;
    try { localStorage.setItem('sideCollapsed', String(this.collapsed)); } catch (e) {
      // Ignore write errors to localStorage (private browsing, disabled storage, or quota exceeded).
    }
    this.updateCssVar();
  }

  private updateCssVar(): void {
    const w = this.collapsed ? 'var(--sidenav-collapsed)' : 'var(--sidenav-expanded)';
    document.documentElement.style.setProperty('--sidenav-width', w);
  }
}