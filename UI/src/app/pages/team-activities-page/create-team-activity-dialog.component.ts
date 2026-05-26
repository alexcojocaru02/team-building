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
  protected activityDueAt = '';
  protected activityPoints = 10;
  protected errorMessage = '';

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

    // validate due date/time if provided
    let dueAtIso: string | null = null;
    if (this.activityDueAt) {
      const parsed = new Date(this.activityDueAt);
      if (isNaN(parsed.getTime())) {
        this.errorMessage = 'Please provide a valid due date and time.';
        return;
      }
      // optional: do not allow past dates
      const now = new Date();
      if (parsed.getTime() < now.getTime()) {
        this.errorMessage = 'Due date must be in the future.';
        return;
      }
      dueAtIso = parsed.toISOString();
    }

    this.errorMessage = '';
    this.dialogRef.close({
      title,
      description,
      activityType: this.activityType,
      options,
      dueAt: dueAtIso,
      points: Math.max(1, Number(this.activityPoints) || 10),
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