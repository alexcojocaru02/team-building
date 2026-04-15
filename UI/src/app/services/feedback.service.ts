import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateFeedbackDto {
  toUserId: string;
  message: string;
}

export interface FeedbackDto {
  id: string;
  fromUserId: string;
  fromUserFullName?: string;
  fromUserEmail?: string;
  toUserId: string;
  toUserFullName?: string;
  toUserEmail?: string;
  message: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  sendFeedback(dto: CreateFeedbackDto): Observable<FeedbackDto> {
    return this.http.post<FeedbackDto>(`${this.apiUrl}/feedback`, dto);
  }

  getReceivedFeedback(): Observable<FeedbackDto[]> {
    return this.http.get<FeedbackDto[]>(`${this.apiUrl}/feedback/received`);
  }
}
