import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import {
  FeedbackService, CreateFeedbackDto, FeedbackCategory, FeedbackTone,
  FEEDBACK_CATEGORIES, FEEDBACK_TONES
} from '../../services/feedback.service';
import { UserSummaryDto } from '../../services/users.service';

export interface SendFeedbackDialogData {
  users: UserSummaryDto[];
  teamName: string;
  giverPoints: number;
  receiverPoints: number;
}

@Component({
  selector: 'app-send-feedback-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatFormFieldModule, MatSelectModule,
    MatInputModule, MatDialogModule,
  ],
  template: `
    <h2 mat-dialog-title style="font-weight: 700; font-size: 1.125rem; margin: 0;">Give Feedback</h2>

    <mat-dialog-content style="padding-top: 8px;">

      <div style="background: rgba(100, 116, 139, 0.08); border-radius: 8px; padding: 10px 12px; margin-bottom: 16px;">
        <p style="font-size: 0.8125rem; color: #475569; margin: 0;">
          🏆 You earn <strong>+{{ data.giverPoints }} pts</strong> · colleague gets <strong>+{{ data.receiverPoints }} pts</strong>
        </p>
      </div>

      <form style="display: flex; flex-direction: column; gap: 14px;">

        <mat-form-field appearance="outline" style="width: 100%;">
          <mat-label>Colleague</mat-label>
          <mat-select [value]="toUserId()" (selectionChange)="toUserId.set($event.value)" required>
            @if (data.users.length === 0) {
              <mat-option disabled>No colleagues available</mat-option>
            }
            @for (u of data.users; track u.id) {
              <mat-option [value]="u.id">{{ u.fullName || u.email }}</mat-option>
            }
          </mat-select>
          <mat-hint>Members of <strong>{{ data.teamName }}</strong></mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width: 100%;">
          <mat-label>Category</mat-label>
          <mat-select [value]="category()" (selectionChange)="category.set($event.value)">
            @for (cat of categories; track cat.value) {
              <mat-option [value]="cat.value">{{ cat.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <div>
          <p style="font-size: 0.8125rem; font-weight: 500; color: #374151; margin: 0 0 8px 0;">Tone</p>
          <div style="display: flex; gap: 8px; flex-wrap: wrap;">
            @for (t of tones; track t.value) {
              <button
                type="button"
                mat-stroked-button
                (click)="tone.set(t.value)"
                class="tone-btn"
                [class.tone-btn--positive]="t.value === 'Positive'"
                [class.tone-btn--constructive]="t.value === 'Constructive'"
                [class.tone-btn--critical]="t.value === 'Critical'"
                [class.active]="tone() === t.value"
              >{{ t.label }}</button>
            }
          </div>
        </div>

        <mat-form-field appearance="outline" style="width: 100%;">
          <mat-label>Your feedback</mat-label>
          <textarea
            matInput
            [ngModel]="message()"
            (ngModelChange)="message.set($event)"
            name="message"
            placeholder="Be specific — describe what you observed and its impact."
            rows="4"
            required
          ></textarea>
        </mat-form-field>

      </form>

    </mat-dialog-content>

    <mat-dialog-actions align="end" style="gap: 8px; padding: 12px 24px 16px;">
      <button mat-button mat-dialog-close>Cancel</button>
      <button
        mat-flat-button
        color="primary"
        style="border-radius: 24px; font-weight: 600;"
        [disabled]="isSubmitting() || !toUserId() || !message().trim()"
        (click)="submit()"
      >
        {{ isSubmitting() ? 'Sending…' : 'Send Feedback' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .tone-btn {
      border-radius: 24px !important;
      font-size: 0.8125rem !important;
      &--positive.active { background: #d1fae5 !important; color: #065f46 !important; border-color: #6ee7b7 !important; }
      &--constructive.active { background: #dbeafe !important; color: #1e40af !important; border-color: #93c5fd !important; }
      &--critical.active { background: #ffedd5 !important; color: #9a3412 !important; border-color: #fdba74 !important; }
    }
  `],
})
export class SendFeedbackDialogComponent {
  private feedbackService = inject(FeedbackService);
  private dialogRef = inject(MatDialogRef<SendFeedbackDialogComponent>);

  readonly data: SendFeedbackDialogData = inject(MAT_DIALOG_DATA);
  readonly categories = FEEDBACK_CATEGORIES;
  readonly tones = FEEDBACK_TONES;

  toUserId = signal('');
  category = signal<FeedbackCategory>('Communication');
  tone = signal<FeedbackTone>('Positive');
  message = signal('');
  isSubmitting = signal(false);

  submit(): void {
    if (!this.toUserId() || !this.message().trim()) return;

    this.isSubmitting.set(true);
    const dto: CreateFeedbackDto = {
      toUserId: this.toUserId(),
      message: this.message(),
      category: this.category(),
      tone: this.tone(),
    };

    this.feedbackService.sendFeedback(dto).subscribe({
      next: (result) => this.dialogRef.close({ success: true, result }),
      error: () => {
        this.isSubmitting.set(false);
        this.dialogRef.close({ success: false });
      },
    });
  }
}
