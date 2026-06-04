import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DashboardService, CohesionDashboardDto } from '../../services/dashboard.service';
import { UsersService } from '../../services/users.service';
import { AuthService } from '../../services/auth.service';
import { TeamDetailDto, TeamJoinRequestDto } from '../../models/auth.models';

@Component({
  selector: 'app-cohesion-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatSnackBarModule],
  templateUrl: './growth-page.html',
  styleUrl: './growth-page.scss',
})
export class CohesionDashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private route = inject(ActivatedRoute);

  cohesionData = signal<CohesionDashboardDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  team = signal<TeamDetailDto | null>(null);
  joinRequests = signal<TeamJoinRequestDto[]>([]);
  processingRequestId = signal<string | null>(null);

  private currentTeamId = '';

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

    this.dashboardService.getCohesionData(teamId).subscribe({
      next: (data) => {
        this.cohesionData.set(data);
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

  getPercentage(userFeedback: number, total: number): number {
    return total > 0 ? Math.round((userFeedback / total) * 100) : 0;
  }
}
