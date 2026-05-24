import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { TeamDetailDto, CreateTeamDto } from '../../models/auth.models';
import { ConfirmDialogComponent } from './confirm-dialog.component';

@Component({
  selector: 'app-teams-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule],
  templateUrl: './teams-list.component.html',
  styleUrls: ['./teams-list.component.scss']
})
export class TeamsListComponent implements OnInit {
  private authService = inject(AuthService);
  private dialog = inject(MatDialog);
  private usersService = inject(UsersService);

  teams = signal<TeamDetailDto[]>([]);
  currentUserId: string | null = null;
  isAdmin = false;
  isLoading = signal(true);
  errorMessage = signal('');
  createMessage = signal('');
  deleteMessage = signal('');
  isDeletingTeamId = signal<string | null>(null);
  showCreateForm = false;
  isCreatingTeam = false;
  newTeam: CreateTeamDto = { name: '' };

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

    this.usersService.getAllTeams().subscribe({
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
    this.createMessage.set('');

    if (!this.newTeam.name.trim()) {
      this.createMessage.set('Please enter a team name.');
      return;
    }

    this.isCreatingTeam = true;
    this.usersService.createTeam(this.newTeam).subscribe({
      next: (team) => {
        this.teams.update(teams => [team, ...teams]);
        this.newTeam = { name: '' };
        this.showCreateForm = false;
        this.isCreatingTeam = false;
      },
      error: (err) => {
        console.error('Failed to create team:', err);
        this.isCreatingTeam = false;
        this.createMessage.set(this.getErrorMessage(err, 'Failed to create team.'));
      }
    });
  }

  editTeam(team: TeamDetailDto) {
    // TODO: Implement edit team dialog
    console.log('Edit team:', team);
  }

  manageMembers(team: TeamDetailDto) {
    // TODO: Implement manage members dialog
    console.log('Manage members for team:', team);
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

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.deleteTeam(team);
      }
    });
  }

  private deleteTeam(team: TeamDetailDto) {
    this.isDeletingTeamId.set(team.id);
    this.usersService.deleteTeam(team.id).subscribe({
      next: () => {
        this.teams.update(teams => teams.filter(t => t.id !== team.id));
        this.isDeletingTeamId.set(null);
      },
      error: (err) => {
        console.error('Failed to delete team:', err);
        this.deleteMessage.set(this.getErrorMessage(err, 'Failed to delete team.'));
        this.isDeletingTeamId.set(null);
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

  isOwnerOrAdmin(team: TeamDetailDto): boolean {
    return this.isAdmin || team.ownerId === this.currentUserId;
  }
}
