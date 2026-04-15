import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateFeedPostDto {
  content: string;
}

export interface FeedPostDto {
  id: string;
  content: string;
  createdAt: string;
  authorId: string;
}

@Injectable({
  providedIn: 'root'
})
export class FeedService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getPosts(): Observable<FeedPostDto[]> {
    return this.http.get<FeedPostDto[]>(`${this.apiUrl}/feed`);
  }

  createPost(dto: CreateFeedPostDto): Observable<FeedPostDto> {
    return this.http.post<FeedPostDto>(`${this.apiUrl}/feed`, dto);
  }
}
