import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { FeedService, FeedPostDto, CreateFeedPostDto } from '../../services/feed.service';
import { AuthService } from '../../services/auth.service';
import { ConfirmDialogComponent } from '../teams-page/confirm-dialog.component';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';

@Component({
  selector: 'app-feed-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatButtonModule, MatIconModule, MatDividerModule, MatMenuModule, MatDialogModule, MatSnackBarModule],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
})
export class FeedPage implements OnInit {
  private feedService = inject(FeedService);
  private authService = inject(AuthService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
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
  commentDrafts: Record<string, string> = {};
  expandedComments: Record<string, boolean> = {};
  composeExpanded = signal(false);

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
        this.composeExpanded.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to create post');
        this.isSubmitting.set(false);
        console.error('Error creating post:', error);
      }
    });
  }

  toggleLike(post: FeedPostDto): void {
    const request = post.likedByCurrentUser
      ? this.feedService.unlikePost(post.id)
      : this.feedService.likePost(post.id);

    request.subscribe({
      next: (stats) => {
        this.posts.update(posts =>
          posts.map(currentPost =>
            currentPost.id === post.id
              ? {
                  ...currentPost,
                  likesCount: stats.likesCount,
                  likedByCurrentUser: stats.likedByCurrentUser,
                }
              : currentPost
          )
        );
      },
      error: (error) => {
        this.errorMessage.set('Failed to update like');
        console.error('Error updating like:', error);
      }
    });
  }

  submitComment(post: FeedPostDto): void {
    const content = this.getCommentDraft(post.id).trim();
    if (!content) return;

    this.feedService.addComment(post.id, { content }).subscribe({
      next: (comment) => {
        this.posts.update(posts =>
          posts.map(currentPost =>
            currentPost.id === post.id
              ? {
                  ...currentPost,
                  commentsCount: currentPost.commentsCount + 1,
                  recentComments: [...currentPost.recentComments, comment].slice(-3),
                }
              : currentPost
          )
        );

        this.setCommentDraft(post.id, '');
      },
      error: (error) => {
        this.errorMessage.set('Failed to add comment');
        console.error('Error creating comment:', error);
      }
    });
  }

  getCommentDraft(postId: string): string {
    return this.commentDrafts[postId] ?? '';
  }

  setCommentDraft(postId: string, value: string): void {
    this.commentDrafts[postId] = value;
  }

  isPostAuthorOrAdmin(post: FeedPostDto): boolean {
    const user = this.authService.currentUser();
    return !!user && (user.id === post.authorId || this.authService.isAdmin());
  }

  requestDeletePost(post: FeedPostDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Delete post',
        message: 'Are you sure you want to delete this post? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel',
        confirmColor: 'warn',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;

      this.feedService.deletePost(post.id).subscribe({
        next: () => {
          this.posts.update(posts => posts.filter(p => p.id !== post.id));
          this.snackBar.open('Post deleted.', 'Dismiss', {
            duration: 2500,
            horizontalPosition: 'end',
            verticalPosition: 'top',
          });
        },
        error: (error) => {
          this.errorMessage.set('Failed to delete post');
          console.error('Error deleting post:', error);
        },
      });
    });
  }

  isCommentThreadVisible(postId: string): boolean {
    return this.expandedComments[postId] !== false;
  }

  toggleCommentThread(postId: string): void {
    this.expandedComments[postId] = !this.isCommentThreadVisible(postId);
  }

  displayName(userId: string, fullName: string | null | undefined, email: string | null | undefined): string {
    if (userId === this.currentUser()?.id) return 'You';
    return fullName || email || userId;
  }

  openProfile(userId: string): void {
    this.dialog.open(ColleagueProfileDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { userId }
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
