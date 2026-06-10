import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';
import { DashboardService, CohesionDashboardDto } from '../../services/dashboard.service';
import { UsersService } from '../../services/users.service';
import { AuthService } from '../../services/auth.service';
import { GamificationService, LeaderboardEntryDto } from '../../services/gamification.service';
import { TeamActivitiesService, TeamActivitySummaryDto } from '../../services/team-activities.service';
import { TeamDetailDto, TeamJoinRequestDto } from '../../models/auth.models';

@Component({
  selector: 'app-cohesion-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule, MatSnackBarModule, MatDialogModule],
  templateUrl: './cohesion-dashboard.html',
  styleUrl: './cohesion-dashboard.scss',
})
export class CohesionDashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  private gamificationService = inject(GamificationService);
  private teamActivitiesService = inject(TeamActivitiesService);
  private snackBar = inject(MatSnackBar);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);

  private avatarPalette = [
    '#0ea5e9', '#2563eb', '#4f46e5', '#7c3aed', '#9333ea',
    '#c026d3', '#db2777', '#e11d48', '#dc2626', '#ea580c',
    '#d97706', '#65a30d', '#16a34a', '#0d9488',
  ];

  cohesionData = signal<CohesionDashboardDto | null>(null);
  members = signal<LeaderboardEntryDto[]>([]);
  activitySummary = signal<TeamActivitySummaryDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  team = signal<TeamDetailDto | null>(null);
  joinRequests = signal<TeamJoinRequestDto[]>([]);
  processingRequestId = signal<string | null>(null);

  private currentTeamId = '';

  averagePoints = computed(() => {
    const list = this.members();
    if (list.length === 0) return 0;
    return list.reduce((sum, m) => sum + m.totalPoints, 0) / list.length;
  });

  topPerformers = computed(() =>
    [...this.members()].sort((a, b) => b.totalPoints - a.totalPoints).slice(0, 3)
  );

  needsAttention = computed(() => {
    const list = this.members();
    if (list.length <= 1) return [];
    const avg = this.averagePoints();
    return [...list]
      .filter(m => m.totalPoints < avg * 0.5 || (m.feedbackGiven === 0 && m.feedbackReceived === 0))
      .sort((a, b) => a.totalPoints - b.totalPoints)
      .slice(0, 3);
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const teamId = params.get('teamId') ?? '';
      this.currentTeamId = teamId;
      if (teamId) {
        this.usersService.getTeam(teamId).subscribe({
          next: (t) => {
            this.team.set(t);
            this.loadJoinRequestsIfOwner(t, teamId);
          },
        });
        this.loadCohesionData(teamId);
      }
    });
  }

  private loadJoinRequestsIfOwner(team: TeamDetailDto, teamId: string): void {
    const currentUserId = this.authService.currentUser()?.id;
    const isAdmin = this.authService.isAdmin();
    if (isAdmin || team.ownerId === currentUserId) {
      this.usersService.getTeamJoinRequests(teamId).subscribe({
        next: (reqs) => this.joinRequests.set(reqs),
        error: () => {}
      });
    }
  }

  loadCohesionData(teamId: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    forkJoin({
      cohesion: this.dashboardService.getCohesionData(teamId),
      leaderboard: this.gamificationService.getLeaderboard(teamId).pipe(catchError(() => of(null))),
      summary: this.teamActivitiesService.getTeamActivitySummary(teamId).pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ cohesion, leaderboard, summary }) => {
        this.cohesionData.set(cohesion);
        this.members.set([...(leaderboard?.entries ?? [])].sort((a, b) => b.totalPoints - a.totalPoints));
        this.activitySummary.set(summary);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.status === 403
          ? 'You do not have access to this dashboard'
          : 'Failed to load cohesion data');
        this.isLoading.set(false);
      }
    });
  }

  approveRequest(request: TeamJoinRequestDto): void {
    this.processingRequestId.set(request.id);
    this.usersService.approveJoinRequest(request.id).subscribe({
      next: () => {
        this.joinRequests.update(reqs => reqs.filter(r => r.id !== request.id));
        this.processingRequestId.set(null);
        this.snackBar.open(`Approved ${request.userFullName || request.userEmail}.`, 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.processingRequestId.set(null);
        this.snackBar.open('Failed to approve request.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  rejectRequest(request: TeamJoinRequestDto): void {
    this.processingRequestId.set(request.id);
    this.usersService.rejectJoinRequest(request.id).subscribe({
      next: () => {
        this.joinRequests.update(reqs => reqs.filter(r => r.id !== request.id));
        this.processingRequestId.set(null);
        this.snackBar.open(`Rejected request from ${request.userFullName || request.userEmail}.`, 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.processingRequestId.set(null);
        this.snackBar.open('Failed to reject request.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  openProfile(userId: string): void {
    this.dialog.open(ColleagueProfileDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { userId }
    });
  }

  getPercentage(value: number, total: number): number {
    return total > 0 ? Math.round((value / total) * 100) : 0;
  }

  getInitials(name?: string): string {
    if (!name) return '?';
    const parts = name.trim().split(/\s+/).filter(Boolean);
    return parts.length >= 2
      ? (parts[0][0] + parts[1][0]).toUpperCase()
      : name.slice(0, 2).toUpperCase();
  }

  getAvatarColor(seed: string): string {
    let hash = 0;
    for (let i = 0; i < seed.length; i++) {
      hash = seed.charCodeAt(i) + ((hash << 5) - hash);
    }
    return this.avatarPalette[Math.abs(hash) % this.avatarPalette.length];
  }
}
