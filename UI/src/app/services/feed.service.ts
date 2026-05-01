import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateFeedPostDto {
  content: string;
}

export interface CreateFeedPostCommentDto {
  content: string;
}

export interface FeedPostCommentDto {
  id: string;
  content: string;
  createdAt: string;
  authorId: string;
  authorFullName?: string;
  authorEmail?: string;
}

export interface FeedPostReactionStatsDto {
  postId: string;
  likesCount: number;
  likedByCurrentUser: boolean;
}

export interface FeedPostDto {
  id: string;
  content: string;
  createdAt: string;
  authorId: string;
  authorFullName?: string;
  authorEmail?: string;
  likesCount: number;
  commentsCount: number;
  likedByCurrentUser: boolean;
  recentComments: FeedPostCommentDto[];
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

  likePost(postId: string): Observable<FeedPostReactionStatsDto> {
    return this.http.post<FeedPostReactionStatsDto>(`${this.apiUrl}/feed/${postId}/like`, {});
  }

  unlikePost(postId: string): Observable<FeedPostReactionStatsDto> {
    return this.http.delete<FeedPostReactionStatsDto>(`${this.apiUrl}/feed/${postId}/like`);
  }

  addComment(postId: string, dto: CreateFeedPostCommentDto): Observable<FeedPostCommentDto> {
    return this.http.post<FeedPostCommentDto>(`${this.apiUrl}/feed/${postId}/comments`, dto);
  }

  getComments(postId: string): Observable<FeedPostCommentDto[]> {
    return this.http.get<FeedPostCommentDto[]>(`${this.apiUrl}/feed/${postId}/comments`);
  }
}
