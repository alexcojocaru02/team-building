import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type TeamActivityType = 'prompt' | 'poll' | 'mini-challenge' | 'trivia';
export type TeamActivityStatus = 'Open' | 'Closed';

export interface CreateTeamActivityDto {
  title: string;
  description: string;
  activityType: TeamActivityType | string;
  options: string[];
  dueAt?: string | null;
  points: number;
}

export interface SubmitTeamActivityResponseDto {
  textResponse?: string | null;
  selectedOptionIndex?: number | null;
}

export interface TeamActivityResponseDto {
  userId: string;
  userFullName?: string;
  userEmail?: string;
  textResponse?: string | null;
  selectedOptionIndex?: number | null;
  submittedAt: string;
}

export interface TeamActivityDto {
  id: string;
  teamId: string;
  createdByUserId: string;
  createdByUserFullName?: string;
  createdByUserEmail?: string;
  activityType: string;
  title: string;
  description: string;
  options: string[];
  points: number;
  dueAt?: string | null;
  status: TeamActivityStatus;
  createdAt: string;
  completedAt?: string | null;
  responsesCount: number;
  participantCount: number;
  hasCurrentUserResponded: boolean;
  currentUserTextResponse?: string | null;
  currentUserSelectedOptionIndex?: number | null;
  recentResponses: TeamActivityResponseDto[];
}

export interface TeamActivitySummaryDto {
  teamId: string;
  totalActivities: number;
  openActivities: number;
  closedActivities: number;
  totalResponses: number;
  participantCount: number;
  teamMemberCount: number;
  participationRate: number;
  recentActivitiesCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class TeamActivitiesService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getTeamActivities(teamId: string): Observable<TeamActivityDto[]> {
    return this.http.get<TeamActivityDto[]>(`${this.apiUrl}/teams/${teamId}/activities`);
  }

  getTeamActivitySummary(teamId: string): Observable<TeamActivitySummaryDto> {
    return this.http.get<TeamActivitySummaryDto>(`${this.apiUrl}/teams/${teamId}/activities/summary`);
  }

  createTeamActivity(teamId: string, dto: CreateTeamActivityDto): Observable<TeamActivityDto> {
    const toServerType = (t: string | undefined): string => {
      if (!t) return 'Prompt';
      const n = t.trim().toLowerCase();
      return n === 'poll' ? 'Poll'
        : n === 'trivia' ? 'Trivia'
        : n === 'mini-challenge' || n === 'minichallenge' || n === 'challenge' ? 'MiniChallenge'
        : 'Prompt';
    };

    const payload = { ...dto, activityType: toServerType(dto.activityType) };
    return this.http.post<TeamActivityDto>(`${this.apiUrl}/teams/${teamId}/activities`, payload);
  }

  respondToActivity(teamId: string, activityId: string, dto: SubmitTeamActivityResponseDto): Observable<TeamActivityDto> {
    return this.http.post<TeamActivityDto>(`${this.apiUrl}/teams/${teamId}/activities/${activityId}/responses`, dto);
  }

  completeActivity(teamId: string, activityId: string): Observable<TeamActivityDto> {
    return this.http.post<TeamActivityDto>(`${this.apiUrl}/teams/${teamId}/activities/${activityId}/complete`, {});
  }
}