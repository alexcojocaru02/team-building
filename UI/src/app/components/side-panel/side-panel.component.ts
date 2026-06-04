import { Component, computed, inject, OnInit, AfterViewInit, PLATFORM_ID, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule, isPlatformBrowser, DOCUMENT } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { TeamDetailDto } from '../../models/auth.models';

@Component({
  standalone: true,
  selector: 'app-side-panel',
  imports: [RouterModule, MatIconModule, MatButtonModule, CommonModule],
  templateUrl: './side-panel.component.html',
  styleUrls: ['./side-panel.component.scss']
})
export class SidePanelComponent implements OnInit, AfterViewInit {
  private authService = inject(AuthService);
  private usersService = inject(UsersService);
  private platformId = inject(PLATFORM_ID);
  private document = inject(DOCUMENT) as Document;
  private isBrowser = isPlatformBrowser(this.platformId);

  collapsed = false;
  isAdmin = this.authService.isAdmin;

  userTeams = signal<TeamDetailDto[]>([]);
  expandedTeamId = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.isBrowser) return;
    try {
      this.collapsed = localStorage.getItem('sideCollapsed') === 'true';
    } catch (e) {}
    this.loadTeams();
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser) return;
    this.updateCssVar();
  }

  private loadTeams(): void {
    const currentUserId = this.authService.currentUser()?.id;
    if (!currentUserId) return;
    this.usersService.getAllTeams().subscribe({
      next: (teams) => {
        const myTeams = teams.filter(t => t.memberIds.includes(currentUserId));
        this.userTeams.set(myTeams);
        if (myTeams.length > 0) {
          this.expandedTeamId.set(myTeams[0].id);
        }
      }
    });
  }

  toggleTeam(teamId: string): void {
    this.expandedTeamId.set(this.expandedTeamId() === teamId ? null : teamId);
  }

  teamLabel(index: number): string {
    return `T${index + 1}`;
  }

  toggle(): void {
    this.collapsed = !this.collapsed;
    if (this.isBrowser) {
      try { localStorage.setItem('sideCollapsed', String(this.collapsed)); } catch (e) {}
      this.updateCssVar();
    }
  }

  private updateCssVar(): void {
    if (!this.isBrowser) return;
    const w = this.collapsed ? 'var(--sidenav-collapsed)' : 'var(--sidenav-expanded)';
    this.document.documentElement.style.setProperty('--sidenav-width', w);
  }
}
