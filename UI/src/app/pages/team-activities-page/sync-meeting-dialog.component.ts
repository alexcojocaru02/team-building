import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { RsvpStatus, TeamActivityDto } from '../../services/team-activities.service';

export interface SyncMeetingDialogData {
  activity: TeamActivityDto;
  canManage: boolean;
}

export type SyncMeetingDialogResult =
  | { action: 'rsvp'; rsvpStatus: Exclude<RsvpStatus, 'Pending'> }
  | { action: 'close' };

@Component({
  selector: 'app-sync-meeting-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, MatChipsModule],
  templateUrl: './sync-meeting-dialog.component.html',
})
export class SyncMeetingDialogComponent {
  private dialogRef = inject(MatDialogRef<SyncMeetingDialogComponent, SyncMeetingDialogResult | undefined>);
  protected data = inject<SyncMeetingDialogData>(MAT_DIALOG_DATA);

  protected readonly activity = this.data.activity;
  protected readonly canManage = this.data.canManage;

  sameDay(start?: string | null, end?: string | null): boolean {
    if (!start || !end) return false;
    const startDate = new Date(start);
    const endDate = new Date(end);
    return startDate.getFullYear() === endDate.getFullYear()
      && startDate.getMonth() === endDate.getMonth()
      && startDate.getDate() === endDate.getDate();
  }

  close(): void {
    this.dialogRef.close();
  }

  joinMeeting(): void {
    if (this.activity.meetingLink) {
      window.open(this.activity.meetingLink, '_blank', 'noopener');
    }
  }

  rsvp(status: Exclude<RsvpStatus, 'Pending'>): void {
    this.dialogRef.close({ action: 'rsvp', rsvpStatus: status });
  }

  closeMeeting(): void {
    this.dialogRef.close({ action: 'close' });
  }
}
