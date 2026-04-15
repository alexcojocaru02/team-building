import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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

  getUsers(): Observable<UserSummaryDto[]> {
    return this.http.get<UserSummaryDto[]>(`${this.apiUrl}/users`);
  }
}
