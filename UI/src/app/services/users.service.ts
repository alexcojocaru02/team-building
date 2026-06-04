import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserDto, UpdateProfileDto, TeamDetailDto, CreateTeamDto } from '../models/auth.models';

export interface UserSummaryDto {
  id: string;
  fullName?: string;
  email: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // User endpoints
  getUsers(): Observable<UserSummaryDto[]> {
    return this.http.get<UserSummaryDto[]>(`${this.apiUrl}/users`);
  }

  getUserProfile(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.apiUrl}/users/${id}`);
  }

  getMyProfile(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.apiUrl}/users/me`);
  }

  updateProfile(id: string, dto: UpdateProfileDto): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.apiUrl}/users/${id}`, dto);
  }

  updateMyProfile(dto: UpdateProfileDto): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.apiUrl}/users/me`, dto);
  }

  // Team endpoints
  getAllTeams(): Observable<TeamDetailDto[]> {
    return this.http.get<TeamDetailDto[]>(`${this.apiUrl}/teams`);
  }

  getTeam(id: string): Observable<TeamDetailDto> {
    return this.http.get<TeamDetailDto>(`${this.apiUrl}/teams/${id}`);
  }

  createTeam(dto: CreateTeamDto): Observable<TeamDetailDto> {
    return this.http.post<TeamDetailDto>(`${this.apiUrl}/teams`, dto);
  }

  updateTeam(id: string, dto: CreateTeamDto): Observable<TeamDetailDto> {
    return this.http.put<TeamDetailDto>(`${this.apiUrl}/teams/${id}`, dto);
  }

  addTeamMember(teamId: string, userId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/teams/${teamId}/add/${userId}`, {});
  }

  removeTeamMember(teamId: string, userId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/teams/${teamId}/members/${userId}`);
  }

  deleteTeam(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/teams/${id}`);
  }

  getTeammates(): Observable<UserSummaryDto[]> {
    return this.http.get<UserSummaryDto[]>(`${this.apiUrl}/users/teammates`);
  }

  getTeammatesForTeam(teamId: string): Observable<UserSummaryDto[]> {
    return this.http.get<UserSummaryDto[]>(`${this.apiUrl}/users/teammates/${teamId}`);
  }
}
