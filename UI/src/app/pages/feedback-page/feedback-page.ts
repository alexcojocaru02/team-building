import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import {
  FeedbackService, FeedbackDto, FeedbackCategory, FeedbackTone, FEEDBACK_CATEGORIES, FEEDBACK_TONES
} from '../../services/feedback.service';
import { UsersService, UserSummaryDto } from '../../services/users.service';
import { TeamDetailDto } from '../../models/auth.models';
import { SendFeedbackDialogComponent } from './send-feedback-dialog';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';
import { UserAvatarComponent } from '../../shared/user-avatar.component';

@Component({
  selector: 'app-feedback-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatSnackBarModule,
    MatDialogModule,
    UserAvatarComponent,
  ],
  templateUrl: './feedback-page.html',
  styleUrl: './feedback-page.scss',
})
export class FeedbackPage implements OnInit {
  private feedbackService = inject(FeedbackService);
  private usersService = inject(UsersService);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  teamId = signal('');
  team = signal<TeamDetailDto | null>(null);
  users = signal<UserSummaryDto[]>([]);

  receivedFeedback = signal<FeedbackDto[]>([]);
  isLoadingFeedback = signal(true);

  readonly categories = FEEDBACK_CATEGORIES;
  readonly tones = FEEDBACK_TONES;
  readonly GIVER_POINTS = 5;
  readonly RECEIVER_POINTS = 10;

  selectedTone = signal<FeedbackTone | null>(null);

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('teamId') ?? '';
      this.teamId.set(id);
      if (id) {
        this.loadTeam(id);
        this.loadUsers(id);
      }
      this.loadReceivedFeedback();
    });
  }

  private loadTeam(teamId: string): void {
    this.usersService.getTeam(teamId).subscribe({
      next: (team) => this.team.set(team),
      error: () => {}
    });
  }

  private loadUsers(teamId: string): void {
    this.usersService.getTeammatesForTeam(teamId).subscribe({
      next: (data) => this.users.set(data),
      error: () => {}
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
        this.snackBar.open('Failed to load feedback.', 'Dismiss', { duration: 3000 });
        this.isLoadingFeedback.set(false);
      }
    });
  }

  openSendDialog(): void {
    const ref = this.dialog.open(SendFeedbackDialogComponent, {
      width: '520px',
      maxWidth: '95vw',
      data: {
        users: this.users(),
        teamName: this.team()?.name ?? 'your team',
        giverPoints: this.GIVER_POINTS,
        receiverPoints: this.RECEIVER_POINTS,
      },
    });

    ref.afterClosed().subscribe((result?: { success: boolean }) => {
      if (result?.success) {
        this.snackBar.open(`Feedback sent! You earned +${this.GIVER_POINTS} pts.`, 'Dismiss', { duration: 4000 });
        this.loadReceivedFeedback();
      } else if (result && !result.success) {
        this.snackBar.open('Failed to send feedback. Please try again.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  filteredFeedback(): FeedbackDto[] {
    const tone = this.selectedTone();
    return tone ? this.receivedFeedback().filter(f => f.tone === tone) : this.receivedFeedback();
  }

  toggleToneFilter(tone: FeedbackTone): void {
    this.selectedTone.set(this.selectedTone() === tone ? null : tone);
  }

  // Stats helpers — always over full set, not filtered
  toneCount(tone: FeedbackTone): number {
    return this.receivedFeedback().filter(f => f.tone === tone).length;
  }

  categoryToneCount(category: FeedbackCategory, tone: FeedbackTone): number {
    return this.receivedFeedback().filter(f => f.category === category && f.tone === tone).length;
  }

  categoryCount(category: FeedbackCategory): number {
    return this.receivedFeedback().filter(f => f.category === category).length;
  }

  categoryPercent(category: FeedbackCategory): number {
    const total = this.receivedFeedback().length;
    if (total === 0) return 0;
    return Math.round((this.categoryCount(category) / total) * 100);
  }

  getCategoryLabel(value: FeedbackCategory): string {
    return this.categories.find(c => c.value === value)?.label ?? value;
  }

  openProfile(userId: string): void {
    this.dialog.open(ColleagueProfileDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { userId }
    });
  }

}
