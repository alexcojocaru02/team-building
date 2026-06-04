import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TeamDetailDto, TeamJoinRequestDto } from '../../models/auth.models';
import { UsersService, UserSummaryDto } from '../../services/users.service';
import { AuthService } from '../../services/auth.service';

interface ManageMembersDialogData {
  team: TeamDetailDto;
}

@Component({
  selector: 'app-manage-members-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatSelectModule, MatSnackBarModule],
  templateUrl: './manage-members-dialog.component.html'
})
export class ManageMembersDialogComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private dialogRef = inject(MatDialogRef<ManageMembersDialogComponent, boolean>);
  private snackBar = inject(MatSnackBar);
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  protected data = inject<ManageMembersDialogData>(MAT_DIALOG_DATA);

  protected readonly team = this.data.team;
  protected readonly users = signal<UserSummaryDto[]>([]);
  protected readonly memberIds = signal<string[]>([...this.team.memberIds]);
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal('');
  protected readonly isUpdating = signal(false);
  protected readonly memberIdSet = computed(() => new Set(this.memberIds()));
  protected readonly memberUsers = computed(() =>
    this.users().filter(user => this.memberIdSet().has(user.id))
  );
  protected readonly availableUsers = computed(() =>
    this.users().filter(user => !this.memberIdSet().has(user.id))
  );
  protected selectedUserId = '';
  protected readonly joinRequests = signal<TeamJoinRequestDto[]>([]);
  protected readonly processingRequestId = signal<string | null>(null);
  private hasChanges = false;

  ngOnInit(): void {
    const currentUser = this.authService.currentUser();
    const canSeeRequests = currentUser?.role === 'Admin' || this.team.ownerId === currentUser?.id;
    if (canSeeRequests) {
      this.usersService.getTeamJoinRequests(this.team.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (reqs) => this.joinRequests.set(reqs),
        error: () => {}
      });
    }

    this.usersService.getUsers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (users) => {
        this.users.set(users);
        this.selectedUserId = this.availableUsers()[0]?.id ?? '';
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load users:', err);
        this.loadError.set('Failed to load users. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  displayName(user: UserSummaryDto): string {
    return user.fullName?.trim() || user.email;
  }

  addMember(): void {
    const userId = this.selectedUserId;
    if (!userId) {
      return;
    }

    this.isUpdating.set(true);

    this.usersService.addTeamMember(this.team.id, userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.memberIds.set([...this.memberIds(), userId]);
        this.hasChanges = true;
        this.selectedUserId = this.availableUsers()[0]?.id ?? '';
        this.showSnackBar('Member added successfully.');
        this.isUpdating.set(false);
      },
      error: (err) => {
        console.error('Failed to add team member:', err);
        this.showSnackBar(this.getErrorMessage(err, 'Failed to add member.'), true);
        this.isUpdating.set(false);
      }
    });
  }

  removeMember(userId: string): void {
    if (userId === this.team.ownerId) {
      this.showSnackBar('The team owner cannot be removed.');
      return;
    }

    this.isUpdating.set(true);

    this.usersService.removeTeamMember(this.team.id, userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.memberIds.set(this.memberIds().filter(memberId => memberId !== userId));
        this.hasChanges = true;
        if (this.selectedUserId === userId) {
          this.selectedUserId = this.availableUsers()[0]?.id ?? '';
        }
        this.showSnackBar('Member removed successfully.');
        this.isUpdating.set(false);
      },
      error: (err) => {
        console.error('Failed to remove team member:', err);
        this.showSnackBar(this.getErrorMessage(err, 'Failed to remove member.'), true);
        this.isUpdating.set(false);
      }
    });
  }

  approveRequest(req: TeamJoinRequestDto): void {
    this.processingRequestId.set(req.id);
    this.usersService.approveJoinRequest(req.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.joinRequests.set(this.joinRequests().filter(r => r.id !== req.id));
        this.memberIds.set([...this.memberIds(), req.userId]);
        this.hasChanges = true;
        this.processingRequestId.set(null);
        this.showSnackBar(`${req.userFullName || req.userEmail} added to team.`);
      },
      error: () => {
        this.processingRequestId.set(null);
        this.showSnackBar('Failed to approve request.', true);
      }
    });
  }

  rejectRequest(req: TeamJoinRequestDto): void {
    this.processingRequestId.set(req.id);
    this.usersService.rejectJoinRequest(req.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.joinRequests.set(this.joinRequests().filter(r => r.id !== req.id));
        this.processingRequestId.set(null);
        this.showSnackBar('Request rejected.');
      },
      error: () => {
        this.processingRequestId.set(null);
        this.showSnackBar('Failed to reject request.', true);
      }
    });
  }

  close(): void {
    this.dialogRef.close(this.hasChanges);
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const errObj = error as { error?: { message?: string }; message?: string };
      if (typeof errObj.error?.message === 'string' && errObj.error.message.trim()) {
        return errObj.error.message;
      }

      if (typeof errObj.message === 'string' && errObj.message.trim()) {
        return errObj.message;
      }
    }

    return fallback;
  }

  private showSnackBar(message: string, isError = false): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: isError ? 4500 : 2500,
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }
}