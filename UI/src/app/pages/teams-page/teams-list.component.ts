import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, filter, finalize, switchMap, tap } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { TeamDetailDto } from '../../models/auth.models';
import { ConfirmDialogComponent } from './confirm-dialog.component';
import { TeamEditDialogComponent, TeamEditDialogResult } from './team-edit-dialog.component';
import { ManageMembersDialogComponent } from './manage-members-dialog.component';

@Component({
  selector: 'app-teams-list',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatSnackBarModule],
  templateUrl: './teams-list.component.html',
  styleUrls: ['./teams-list.component.scss']
})
export class TeamsListComponent implements OnInit {
  private authService = inject(AuthService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private usersService = inject(UsersService);
  private destroyRef = inject(DestroyRef);

  teams = signal<TeamDetailDto[]>([]);
  currentUserId: string | null = null;
  isAdmin = false;
  isLoading = signal(true);
  errorMessage = signal('');
  deleteMessage = signal('');
  isDeletingTeamId = signal<string | null>(null);
  isCreatingTeam = signal(false);
  isRequestingJoinId = signal<string | null>(null);
  pendingRequestTeamIds = signal<Set<string>>(new Set());

  ngOnInit() {
    const currentUser = this.authService.currentUser();
    this.currentUserId = currentUser?.id || null;
    this.isAdmin = this.authService.isAdmin();

    this.loadTeams();
  }

  loadTeams() {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.deleteMessage.set('');

    this.usersService.getAllTeams().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (teams) => {
        this.teams.set(teams);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load teams:', err);
        this.errorMessage.set('Failed to load teams. Please refresh and try again.');
        this.isLoading.set(false);
      }
    });
  }

  createTeam() {
    const dialogRef = this.dialog.open(TeamEditDialogComponent, {
      width: '520px',
      data: {
        team: { name: '', description: '' },
        title: 'Create team',
        confirmText: 'Create team'
      }
    });

    dialogRef.afterClosed().pipe(
      takeUntilDestroyed(this.destroyRef),
      filter((result): result is TeamEditDialogResult => !!result),
      tap(() => {
        this.isCreatingTeam.set(true);
      }),
      switchMap((result) => this.usersService.createTeam(result).pipe(
        tap({
          next: (response) => {
            this.teams.update(teams => [response.team, ...teams]);
            if (response.newToken) {
              this.authService.updateToken(response.newToken);
              this.isAdmin = this.authService.isAdmin();
            }
            this.showSnackBar(`Team "${response.team.name}" created successfully.`);
          }
        }),
        catchError((err) => {
          console.error('Failed to create team:', err);
          this.showSnackBar(this.getErrorMessage(err, 'Failed to create team.'), true);
          return EMPTY;
        })
      )),
      finalize(() => {
        this.isCreatingTeam.set(false);
      })
    ).subscribe();
  }

  editTeam(team: TeamDetailDto) {
    const dialogRef = this.dialog.open(TeamEditDialogComponent, {
      width: '520px',
      data: {
        team,
        title: 'Edit team',
        confirmText: 'Save changes'
      }
    });

    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result: TeamEditDialogResult | undefined) => {
      if (!result) {
        return;
      }

      this.usersService.updateTeam(team.id, result).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updatedTeam) => {
          this.teams.update(teams => teams.map(item => item.id === updatedTeam.id ? updatedTeam : item));
          this.showSnackBar(`Team "${updatedTeam.name}" updated successfully.`);
        },
        error: (err) => {
          console.error('Failed to update team:', err);
          this.showSnackBar(this.getErrorMessage(err, 'Failed to save changes.'), true);
        }
      });
    });
  }

  manageMembers(team: TeamDetailDto) {
    const dialogRef = this.dialog.open(ManageMembersDialogComponent, {
      width: '760px',
      maxWidth: '95vw',
      data: { team }
    });

    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((changed: boolean | undefined) => {
      if (changed) {
        this.loadTeams();
      }
    });
  }

  requestDeleteTeam(team: TeamDetailDto) {
    this.deleteMessage.set('');
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Delete team',
        message: `Delete team "${team.name}"? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        confirmColor: 'warn'
      }
    });

    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.deleteTeam(team);
      }
    });
  }

  requestJoin(team: TeamDetailDto) {
    this.isRequestingJoinId.set(team.id);
    this.usersService.requestJoinTeam(team.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.pendingRequestTeamIds.update(ids => new Set([...ids, team.id]));
        this.isRequestingJoinId.set(null);
        this.showSnackBar(`Join request sent for "${team.name}".`);
      },
      error: (err) => {
        console.error('Failed to request join:', err);
        this.showSnackBar(this.getErrorMessage(err, 'Failed to send join request.'), true);
        this.isRequestingJoinId.set(null);
      }
    });
  }

  hasPendingRequest(teamId: string): boolean {
    return this.pendingRequestTeamIds().has(teamId);
  }

  private deleteTeam(team: TeamDetailDto) {
    this.isDeletingTeamId.set(team.id);
    this.usersService.deleteTeam(team.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.teams.update(teams => teams.filter(t => t.id !== team.id));
        this.isDeletingTeamId.set(null);
        this.showSnackBar(`Team "${team.name}" deleted successfully.`);
      },
      error: (err) => {
        console.error('Failed to delete team:', err);
        const message = this.getErrorMessage(err, 'Failed to delete team.');
        this.deleteMessage.set(message);
        this.isDeletingTeamId.set(null);
        this.showSnackBar(message, true);
      }
    });
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

  isOwnerOrAdmin(team: TeamDetailDto): boolean {
    return this.isAdmin || team.ownerId === this.currentUserId;
  }

  isCurrentUserMember(team: TeamDetailDto): boolean {
    return !!this.currentUserId && team.memberIds.includes(this.currentUserId) && team.ownerId !== this.currentUserId;
  }
}
