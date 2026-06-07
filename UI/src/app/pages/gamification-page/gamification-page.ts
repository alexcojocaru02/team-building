import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { GamificationService, LeaderboardEntryDto } from '../../services/gamification.service';
import { TeamDetailDto } from '../../models/auth.models';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';

@Component({
  selector: 'app-gamification-page',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule, MatSnackBarModule, MatDialogModule],
  templateUrl: './gamification-page.html',
  styleUrl: './gamification-page.scss',
})
export class GamificationPage implements OnInit {
  private gamificationService = inject(GamificationService);
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private route = inject(ActivatedRoute);

  team = signal<TeamDetailDto | null>(null);
  entries = signal<LeaderboardEntryDto[]>([]);
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
        this.loadLeaderboard(teamId);
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

  loadLeaderboard(teamId: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.gamificationService.getLeaderboard(teamId).subscribe({
      next: (data) => {
        this.entries.set(data.entries);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error.status === 403
          ? 'You do not have access to this leaderboard'
          : 'Failed to load leaderboard');
        this.isLoading.set(false);
      }
    });
  }
}
