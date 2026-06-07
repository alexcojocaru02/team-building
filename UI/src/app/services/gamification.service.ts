import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LeaderboardEntryDto {
  rank: number;
  userId: string;
  fullName?: string;
  email: string;
  activityPoints: number;
  feedbackGiven: number;
  feedbackReceived: number;
  totalPoints: number;
}

export interface LeaderboardDto {
  teamId: string;
  entries: LeaderboardEntryDto[];
}

@Injectable({
  providedIn: 'root'
})
export class GamificationService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getLeaderboard(teamId: string): Observable<LeaderboardDto> {
    return this.http.get<LeaderboardDto>(`${this.apiUrl}/gamification/leaderboard/${teamId}`);
  }
}
