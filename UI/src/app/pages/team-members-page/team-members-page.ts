import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';
import { UserAvatarComponent } from '../../shared/user-avatar.component';
import { UsersService, UserSummaryDto } from '../../services/users.service';
import { AuthService } from '../../services/auth.service';
import { TeamDetailDto } from '../../models/auth.models';

@Component({
  selector: 'app-team-members-page',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule, MatDialogModule, UserAvatarComponent],
  templateUrl: './team-members-page.html',
  styleUrl: './team-members-page.scss',
})
export class TeamMembersPage implements OnInit {
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  private dialog = inject(MatDialog);
  private route = inject(ActivatedRoute);

  team = signal<TeamDetailDto | null>(null);
  members = signal<UserSummaryDto[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');

  currentUserId = computed(() => this.authService.currentUser()?.id ?? null);

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const teamId = params.get('teamId') ?? '';
      if (teamId) {
        this.usersService.getTeam(teamId).subscribe({
          next: (t) => this.team.set(t),
        });
        this.loadMembers(teamId);
      }
    });
  }

  loadMembers(teamId: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.usersService.getTeammatesForTeam(teamId).subscribe({
      next: (data) => {
        this.members.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.status === 403
          ? 'You do not have access to this team'
          : 'Failed to load team members');
        this.isLoading.set(false);
      }
    });
  }

  openProfile(userId: string): void {
    this.dialog.open(ColleagueProfileDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { userId }
    });
  }
}
