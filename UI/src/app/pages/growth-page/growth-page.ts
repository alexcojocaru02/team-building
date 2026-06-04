import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { DashboardService, CohesionDashboardDto } from '../../services/dashboard.service';
import { UsersService } from '../../services/users.service';
import { TeamDetailDto } from '../../models/auth.models';

@Component({
  selector: 'app-cohesion-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule],
  templateUrl: './growth-page.html',
  styleUrl: './growth-page.scss',
})
export class CohesionDashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  private usersService = inject(UsersService);
  private route = inject(ActivatedRoute);

  cohesionData = signal<CohesionDashboardDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  team = signal<TeamDetailDto | null>(null);

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const teamId = params.get('teamId') ?? '';
      if (teamId) {
        this.usersService.getTeam(teamId).subscribe({
          next: (t) => this.team.set(t),
        });
        this.loadCohesionData(teamId);
      }
    });
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

  getPercentage(userFeedback: number, total: number): number {
    return total > 0 ? Math.round((userFeedback / total) * 100) : 0;
  }
}
