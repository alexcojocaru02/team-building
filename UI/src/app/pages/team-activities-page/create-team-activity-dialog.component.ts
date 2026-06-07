import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CreateTeamActivityDto, TeamActivityType } from '../../services/team-activities.service';

interface CreateTeamActivityDialogData {
  teamName: string;
}

@Component({
  selector: 'app-create-team-activity-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule],
  templateUrl: './create-team-activity-dialog.component.html'
})
export class CreateTeamActivityDialogComponent {
  private dialogRef = inject(MatDialogRef<CreateTeamActivityDialogComponent, CreateTeamActivityDto | undefined>);
  protected data = inject<CreateTeamActivityDialogData>(MAT_DIALOG_DATA);

  protected readonly teamName = this.data.teamName;
  protected activityTitle = '';
  protected activityDescription = '';
  protected activityType: TeamActivityType = 'prompt';
  protected activityOptionsText = '';
  protected activityScheduledAt = '';
  protected activityScheduledEndAt = '';
  protected readonly activityPoints = 10;
  protected meetingLink = '';
  protected errorMessage = '';

  protected readonly totalSteps = 3;
  protected step = 1;

  protected get isLastStep(): boolean {
    return this.step === this.totalSteps;
  }

  protected get titlePlaceholder(): string {
    switch (this.activityType) {
      case 'poll': return 'Which lunch spot should we try this week?';
      case 'mini-challenge': return 'Share a photo of your workspace';
      case 'trivia': return 'Friday trivia: how well do you know the team?';
      case 'sync-meeting': return 'Weekly team sync';
      default: return 'Weekly check-in prompt';
    }
  }

  protected get descriptionPlaceholder(): string {
    switch (this.activityType) {
      case 'poll': return 'Describe what the team is voting on...';
      case 'mini-challenge': return 'Describe the challenge and what counts as completing it...';
      case 'trivia': return 'Write the trivia question (and answer, if needed)...';
      case 'sync-meeting': return 'Add an agenda or notes for this meeting...';
      default: return 'Ask a question, define a challenge, or describe the activity...';
    }
  }

  protected stepLabel(step: number): string {
    switch (step) {
      case 1: return 'Basics';
      case 2: return 'Content';
      case 3: return this.activityType === 'sync-meeting' ? 'Schedule' : 'Due date';
      default: return '';
    }
  }

  back(): void {
    if (this.step > 1) {
      this.step -= 1;
      this.errorMessage = '';
    }
  }

  next(): void {
    if (!this.validateStep(this.step)) return;
    this.errorMessage = '';
    this.step += 1;
  }

  private validateStep(step: number): boolean {
    if (step === 1) {
      if (!this.activityTitle.trim()) {
        this.errorMessage = 'Title is required.';
        return false;
      }
    }

    if (step === 2) {
      if (!this.activityDescription.trim()) {
        this.errorMessage = 'Description is required.';
        return false;
      }
      if (this.activityType === 'poll' && this.parseOptions(this.activityOptionsText).length < 2) {
        this.errorMessage = 'Poll activities need at least two options.';
        return false;
      }
      if (this.activityType === 'sync-meeting' && !this.meetingLink.trim()) {
        this.errorMessage = 'Meeting link is required for sync meetings.';
        return false;
      }
    }

    this.errorMessage = '';
    return true;
  }

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    const title = this.activityTitle.trim();
    const description = this.activityDescription.trim();

    if (!title || !description) {
      this.errorMessage = 'Title and description are required.';
      return;
    }

    const options = this.parseOptions(this.activityOptionsText);
    if (this.activityType === 'poll' && options.length < 2) {
      this.errorMessage = 'Poll activities need at least two options.';
      return;
    }

    let scheduledAtIso: string | null = null;
    let scheduledEndAtIso: string | null = null;
    let meetingLink: string | null = null;

    if (this.activityType === 'sync-meeting') {
      meetingLink = this.meetingLink.trim();

      if (!meetingLink) {
        this.errorMessage = 'Meeting link is required for sync meetings.';
        return;
      }
      if (!this.activityScheduledAt) {
        this.errorMessage = 'Scheduled date and time is required for sync meetings.';
        return;
      }

      const parsedScheduledAt = new Date(this.activityScheduledAt);
      if (isNaN(parsedScheduledAt.getTime())) {
        this.errorMessage = 'Please provide a valid scheduled date and time.';
        return;
      }
      if (parsedScheduledAt.getTime() < new Date().getTime()) {
        this.errorMessage = 'Scheduled date and time must be in the future.';
        return;
      }
      scheduledAtIso = parsedScheduledAt.toISOString();

      if (!this.activityScheduledEndAt) {
        this.errorMessage = 'Scheduled end date and time is required for sync meetings.';
        return;
      }

      const parsedScheduledEndAt = new Date(this.activityScheduledEndAt);
      if (isNaN(parsedScheduledEndAt.getTime())) {
        this.errorMessage = 'Please provide a valid scheduled end date and time.';
        return;
      }
      if (parsedScheduledEndAt.getTime() <= parsedScheduledAt.getTime()) {
        this.errorMessage = 'Scheduled end time must be after the start time.';
        return;
      }
      scheduledEndAtIso = parsedScheduledEndAt.toISOString();
    } else if (this.activityScheduledEndAt) {
      const parsedDueAt = new Date(this.activityScheduledEndAt);
      if (isNaN(parsedDueAt.getTime())) {
        this.errorMessage = 'Please provide a valid due date and time.';
        return;
      }
      if (parsedDueAt.getTime() < new Date().getTime()) {
        this.errorMessage = 'Due date must be in the future.';
        return;
      }
      scheduledEndAtIso = parsedDueAt.toISOString();
    }

    this.errorMessage = '';
    this.dialogRef.close({
      title,
      description,
      activityType: this.activityType,
      options,
      points: this.activityPoints,
      scheduledAt: scheduledAtIso,
      scheduledEndAt: scheduledEndAtIso,
      meetingLink,
    });
  }

  private parseOptions(value: string): string[] {
    return value
      .split('\n')
      .map(option => option.trim())
      .filter(option => option.length > 0);
  }

  openDateTimePicker(input: HTMLInputElement): void {
    if (!input) return;
    const anyInput = input as any;
    try {
      if (typeof anyInput.showPicker === 'function') {
        anyInput.showPicker();
        return;
      }
    } catch (e) {
      // ignore
    }
    input.focus();
  }
}