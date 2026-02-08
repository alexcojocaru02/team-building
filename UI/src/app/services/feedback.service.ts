import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreateFeedbackDto {
  toUserId: string;
  message: string;
}

export interface FeedbackDto {
  id: string;
  fromUserId: string;
  toUserId: string;
  message: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7241/api';

  sendFeedback(dto: CreateFeedbackDto): Observable<FeedbackDto> {
    return this.http.post<FeedbackDto>(`${this.apiUrl}/feedback`, dto);
  }

  getReceivedFeedback(): Observable<FeedbackDto[]> {
    return this.http.get<FeedbackDto[]>(`${this.apiUrl}/feedback/received`);
  }
}
