import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { FeedbackService, CreateFeedbackDto, FeedbackDto } from '../../services/feedback.service';
import { UsersService, UserSummaryDto } from '../../services/users.service';

@Component({
  selector: 'app-feedback-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatButtonModule],
  templateUrl: './feedback-page.html',
  styleUrl: './feedback-page.scss',
})
export class FeedbackPage implements OnInit {
  private feedbackService = inject(FeedbackService);
  private usersService = inject(UsersService);
  private route = inject(ActivatedRoute);

  teamId = signal('');

  receivedFeedback = signal<FeedbackDto[]>([]);
  isLoadingFeedback = signal(true);
  isSubmitting = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  activeTab = signal<'send' | 'received'>('send');
  toUserId = signal('');
  feedbackMessage = signal('');
  users = signal<UserSummaryDto[]>([]);

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('teamId') ?? '';
      this.teamId.set(id);
      if (id) {
        this.loadUsers(id);
        this.loadReceivedFeedback();
      }
    });
  }

  loadUsers(teamId: string): void {
    this.usersService.getTeammatesForTeam(teamId).subscribe({
      next: (data) => this.users.set(data),
      error: () => this.errorMessage.set('Failed to load teammates')
    });
  }

  loadReceivedFeedback(): void {
    this.isLoadingFeedback.set(true);
    this.feedbackService.getReceivedFeedback().subscribe({
      next: (data) => {
        this.receivedFeedback.set(data);
        this.isLoadingFeedback.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load feedback');
        this.isLoadingFeedback.set(false);
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
        setTimeout(() => this.loadReceivedFeedback(), 1000);
      },
      error: () => {
        this.errorMessage.set('Failed to send feedback');
        this.isSubmitting.set(false);
      }
    });
  }
}
