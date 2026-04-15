import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { FeedbackService, CreateFeedbackDto, FeedbackDto } from '../../services/feedback.service';
import { AuthService } from '../../services/auth.service';
import { UsersService, UserSummaryDto } from '../../services/users.service';

@Component({
  selector: 'app-feedback-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './feedback-page.html',
  styleUrl: './feedback-page.scss',
})
export class FeedbackPage implements OnInit {
  private feedbackService = inject(FeedbackService);
  private authService = inject(AuthService);
  private usersService = inject(UsersService);

  receivedFeedback = signal<FeedbackDto[]>([]);
  isLoadingFeedback = signal(true);
  isSubmitting = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  activeTab = signal<'send' | 'received'>('send');

  // Form state for sending feedback
  toUserId = signal('');
  feedbackMessage = signal('');

  users = signal<UserSummaryDto[]>([]);

  ngOnInit(): void {
    this.loadUsers();
    this.loadReceivedFeedback();
  }

  loadUsers(): void {
    this.usersService.getUsers().subscribe({
      next: (data) => {
        const currentUserId = this.authService.currentUser()?.id;
        this.users.set(data.filter(u => u.id !== currentUserId));
      },
      error: (error) => {
        this.errorMessage.set('Failed to load users');
        console.error('Error loading users:', error);
      }
    });
  }

  loadReceivedFeedback(): void {
    this.isLoadingFeedback.set(true);
    this.feedbackService.getReceivedFeedback().subscribe({
      next: (data) => {
        this.receivedFeedback.set(data);
        this.isLoadingFeedback.set(false);
      },
      error: (error) => {
        this.errorMessage.set('Failed to load feedback');
        this.isLoadingFeedback.set(false);
        console.error('Error loading feedback:', error);
      }
    });
  }

  submitFeedback(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.toUserId() || !this.feedbackMessage().trim()) {
      this.errorMessage.set('Please select a recipient and enter feedback');
      return;
    }

    this.isSubmitting.set(true);
    const dto: CreateFeedbackDto = {
      toUserId: this.toUserId(),
      message: this.feedbackMessage()
    };

    this.feedbackService.sendFeedback(dto).subscribe({
      next: () => {
        this.successMessage.set('Feedback sent successfully!');
        this.toUserId.set('');
        this.feedbackMessage.set('');
        this.isSubmitting.set(false);
        // Reload received feedback after a delay
        setTimeout(() => this.loadReceivedFeedback(), 1000);
      },
      error: (error) => {
        this.errorMessage.set('Failed to send feedback');
        this.isSubmitting.set(false);
        console.error('Error sending feedback:', error);
      }
    });
  }
}
