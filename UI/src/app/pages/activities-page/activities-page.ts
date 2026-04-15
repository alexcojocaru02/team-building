import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FeedService, FeedPostDto, CreateFeedPostDto } from '../../services/feed.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-feed-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './activities-page.html',
  styleUrl: './activities-page.scss',
})
export class FeedPage implements OnInit {
  private feedService = inject(FeedService);
  private authService = inject(AuthService);

  posts = signal<FeedPostDto[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  newPostContent = signal('');
  isSubmitting = signal(false);

  currentUser = this.authService.currentUser;

  ngOnInit(): void {
    this.loadPosts();
  }

  loadPosts(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    
    this.feedService.getPosts().subscribe({
      next: (data) => {
        this.posts.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load posts');
        this.isLoading.set(false);
        console.error('Error loading posts:', error);
      }
    });
  }

  submitPost(): void {
    const content = this.newPostContent().trim();
    if (!content) return;

    this.isSubmitting.set(true);
    const dto: CreateFeedPostDto = { content };

    this.feedService.createPost(dto).subscribe({
      next: (newPost) => {
        this.posts.update(posts => [newPost, ...posts]);
        this.newPostContent.set('');
        this.isSubmitting.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to create post');
        this.isSubmitting.set(false);
        console.error('Error creating post:', error);
      }
    });
  }
}
