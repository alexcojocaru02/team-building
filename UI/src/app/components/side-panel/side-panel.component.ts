import { Component, computed, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../services/auth.service';

type NavItem = {
  icon: string;
  label: string;
  route: string;
};

@Component({
  standalone: true,
  selector: 'app-side-panel',
  imports: [RouterModule, MatIconModule],
  templateUrl: './side-panel.component.html',
  styleUrls: ['./side-panel.component.scss']
})
export class SidePanelComponent {
  private authService = inject(AuthService);

  collapsed = false;

  private baseItems: NavItem[] = [
    { icon: 'home', label: 'Home', route: '/home' },
    { icon: 'dynamic_feed', label: 'Feed', route: '/feed' },
    { icon: 'forum', label: 'Feedback', route: '/feedback' },
    { icon: 'insights', label: 'Dashboard', route: '/dashboard' },
    { icon: 'groups', label: 'Teams', route: '/teams' }
  ];

  navItems = computed(() => {
    const items: NavItem[] = [...this.baseItems];

    if (this.authService.isAdmin()) {
      items.push({ icon: 'admin_panel_settings', label: 'Admin', route: '/admin' });
    }

    return items;
  });

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