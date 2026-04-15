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
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
})
export class FeedPage implements OnInit {
  private feedService = inject(FeedService);
  private authService = inject(AuthService);
  private avatarPalette = [
    '#0ea5e9',
    '#2563eb',
    '#4f46e5',
    '#7c3aed',
    '#9333ea',
    '#c026d3',
    '#db2777',
    '#e11d48',
    '#dc2626',
    '#ea580c',
    '#d97706',
    '#65a30d',
    '#16a34a',
    '#0d9488',
  ];

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

  getInitials(value: string | null | undefined): string {
    const trimmed = (value || '').trim();
    if (!trimmed) return 'U';

    const parts = trimmed.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }

    return trimmed.slice(0, 2).toUpperCase();
  }

  getAvatarColor(value: string | null | undefined): string {
    const key = (value || '').trim().toLowerCase();
    if (!key) return this.avatarPalette[0];

    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = (hash << 5) - hash + key.charCodeAt(i);
      hash |= 0;
    }

    const index = Math.abs(hash) % this.avatarPalette.length;
    return this.avatarPalette[index];
  }
}
