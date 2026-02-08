import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserFeedbackStatsDto {
  userId: string;
  email: string;
  feedbackReceived: number;
}

export interface CohesionDashboardDto {
  totalFeedbacks: number;
  users: UserFeedbackStatsDto[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7241/api';

  getCohesionData(): Observable<CohesionDashboardDto> {
    return this.http.get<CohesionDashboardDto>(`${this.apiUrl}/dashboard/cohesion`);
  }
}
