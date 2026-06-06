import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type FeedbackCategory = 'Communication' | 'Collaboration' | 'Delivery' | 'Leadership' | 'ProblemSolving';
export type FeedbackTone = 'Positive' | 'Constructive' | 'Critical';

export const FEEDBACK_CATEGORIES: { value: FeedbackCategory; label: string }[] = [
  { value: 'Communication', label: 'Communication' },
  { value: 'Collaboration', label: 'Collaboration' },
  { value: 'Delivery', label: 'Delivery' },
  { value: 'Leadership', label: 'Leadership' },
  { value: 'ProblemSolving', label: 'Problem Solving' },
];

export const FEEDBACK_TONES: { value: FeedbackTone; label: string; color: string }[] = [
  { value: 'Positive', label: 'Positive', color: 'green' },
  { value: 'Constructive', label: 'Constructive', color: 'blue' },
  { value: 'Critical', label: 'Critical', color: 'orange' },
];

export interface CreateFeedbackDto {
  toUserId: string;
  message: string;
  category: FeedbackCategory;
  tone: FeedbackTone;
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
  category: FeedbackCategory;
  tone: FeedbackTone;
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
