import { Component, computed, inject, OnInit, AfterViewInit, PLATFORM_ID } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';
import { AuthService } from '../../services/auth.service';

type NavItem = {
  icon: string;
  label: string;
  route: string;
};

@Component({
  standalone: true,
  selector: 'app-side-panel',
  imports: [RouterModule, MatIconModule, MatButtonModule],
  templateUrl: './side-panel.component.html',
  styleUrls: ['./side-panel.component.scss']
})
export class SidePanelComponent implements OnInit, AfterViewInit {
  private authService = inject(AuthService);
  private platformId = inject(PLATFORM_ID);
  private document = inject(DOCUMENT) as Document;
  private isBrowser = isPlatformBrowser(this.platformId);

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

  ngOnInit(): void {
    if (!this.isBrowser) return;
    try {
      this.collapsed = localStorage.getItem('sideCollapsed') === 'true';
    } catch (e) {
      // Ignore localStorage access errors (private browsing mode, disabled storage, or quota exceeded).
    }
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser) return;
    this.updateCssVar();
  }

  toggle(): void {
    this.collapsed = !this.collapsed;
    if (this.isBrowser) {
      try { localStorage.setItem('sideCollapsed', String(this.collapsed)); } catch (e) {
        // Ignore write errors to localStorage (private browsing, disabled storage, or quota exceeded).
      }
      this.updateCssVar();
    }
  }

  private updateCssVar(): void {
    if (!this.isBrowser) return;
    const w = this.collapsed ? 'var(--sidenav-collapsed)' : 'var(--sidenav-expanded)';
    this.document.documentElement.style.setProperty('--sidenav-width', w);
  }
}