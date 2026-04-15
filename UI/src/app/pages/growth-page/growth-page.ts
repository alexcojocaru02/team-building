import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DashboardService, CohesionDashboardDto } from '../../services/dashboard.service';

@Component({
  selector: 'app-cohesion-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './growth-page.html',
  styleUrl: './growth-page.scss',
})
export class CohesionDashboard implements OnInit {
  private dashboardService = inject(DashboardService);

  cohesionData = signal<CohesionDashboardDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');

  ngOnInit(): void {
    this.loadCohesionData();
  }

  loadCohesionData(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.dashboardService.getCohesionData().subscribe({
      next: (data) => {
        this.cohesionData.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load cohesion data');
        this.isLoading.set(false);
        console.error('Error loading cohesion data:', error);
      }
    });
  }

  // Helper method to calculate percentage for progress bars
  getPercentage(userFeedback: number, total: number): number {
    return total > 0 ? Math.round((userFeedback / total) * 100) : 0;
  }
}
