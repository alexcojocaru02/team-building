import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { FeedbackService, FeedbackDto } from '../../services/feedback.service';
import { FeedService, FeedPostDto } from '../../services/feed.service';
import { GamificationService, LeaderboardEntryDto } from '../../services/gamification.service';
import { TeamActivitiesService, TeamActivityDto } from '../../services/team-activities.service';
import { TeamDetailDto, UserDto } from '../../models/auth.models';
import { ICONS } from '../../shared/icons';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';
import { UserAvatarComponent } from '../../shared/user-avatar.component';

export interface TeamSummary {
  team: TeamDetailDto;
  leaderboardEntry: LeaderboardEntryDto | null;
  pendingActivitiesCount: number;
}

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule, MatDialogModule, UserAvatarComponent],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss'
})
export class HomePage implements OnInit {
  private authService = inject(AuthService);
  private usersService = inject(UsersService);
  private feedbackService = inject(FeedbackService);
  private feedService = inject(FeedService);
  private gamificationService = inject(GamificationService);
  private teamActivitiesService = inject(TeamActivitiesService);
  private sanitizer = inject(DomSanitizer);
  private dialog = inject(MatDialog);

  currentUser = this.authService.currentUser;

  isLoading = signal(true);
  myProfile = signal<UserDto | null>(null);
  teamSummaries = signal<TeamSummary[]>([]);
  receivedFeedback = signal<FeedbackDto[]>([]);
  recentPosts = signal<FeedPostDto[]>([]);
  pendingActivities = signal<TeamActivityDto[]>([]);

  primaryTeamId = computed(() => this.teamSummaries()[0]?.team.id ?? null);
  hasTeams = computed(() => this.teamSummaries().length > 0);
  hasMultipleTeams = computed(() => this.teamSummaries().length > 1);

  totalPoints = computed(() =>
    this.teamSummaries().reduce((sum, t) => sum + (t.leaderboardEntry?.totalPoints ?? 0), 0)
  );

  recentFeedback = computed(() => this.receivedFeedback().slice(0, 3));
  latestPosts = computed(() => this.recentPosts().slice(0, 3));
  upcomingActivities = computed(() => this.pendingActivities().slice(0, 3));

  isProfileIncomplete = computed(() => {
    const p = this.myProfile();
    if (!p) return false;
    return !p.bio && !p.department && !p.location && !p.icebreaker
      && !(p.hobbies?.length) && !(p.strengths?.length);
  });

  icons = {
    feed: this.sanitizer.bypassSecurityTrustHtml(ICONS.feed),
    feedback: this.sanitizer.bypassSecurityTrustHtml(ICONS.feedback),
    dashboard: this.sanitizer.bypassSecurityTrustHtml(ICONS.dashboard)
  };

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);

    this.usersService.getMyProfile().pipe(
      catchError(() => of(null))
    ).subscribe(profile => {
      this.myProfile.set(profile);
      const teamIds = profile?.teamIds ?? [];
      const userId = this.authService.currentUser()?.id;

      forkJoin({
        feedback: this.feedbackService.getReceivedFeedback().pipe(catchError(() => of([] as FeedbackDto[]))),
        posts: this.feedService.getPosts().pipe(catchError(() => of([] as FeedPostDto[]))),
        teams: teamIds.length
          ? forkJoin(teamIds.map(teamId => forkJoin({
              team: this.usersService.getTeam(teamId).pipe(catchError(() => of(null))),
              activities: this.teamActivitiesService.getTeamActivities(teamId).pipe(catchError(() => of([] as TeamActivityDto[]))),
              leaderboard: this.gamificationService.getLeaderboard(teamId).pipe(catchError(() => of(null))),
            })))
          : of([] as { team: TeamDetailDto | null; activities: TeamActivityDto[]; leaderboard: { entries: LeaderboardEntryDto[] } | null }[]),
      }).subscribe(({ feedback, posts, teams }) => {
        this.receivedFeedback.set([...feedback].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
        this.recentPosts.set(posts);

        const summaries: TeamSummary[] = [];
        const allPending: TeamActivityDto[] = [];

        for (const entry of teams) {
          if (!entry.team) continue;

          const pending = entry.activities.filter(a => a.status === 'Open' && !a.hasCurrentUserResponded);
          allPending.push(...pending);

          summaries.push({
            team: entry.team,
            leaderboardEntry: entry.leaderboard?.entries.find(e => e.userId === userId) ?? null,
            pendingActivitiesCount: pending.length,
          });
        }

        this.teamSummaries.set(summaries);
        this.pendingActivities.set(allPending);

        this.isLoading.set(false);
      });
    });
  }

  logout(): void {
    this.authService.logout();
  }

  openProfile(userId: string): void {
    this.dialog.open(ColleagueProfileDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { userId }
    });
  }

  activityTypeLabel(activityType: string): string {
    return activityType.toLowerCase() === 'syncmeeting' ? 'Meeting' : activityType;
  }

  toneClass(tone: string): string {
    switch (tone) {
      case 'Positive': return 'tw:bg-green-100 tw:text-green-700';
      case 'Critical': return 'tw:bg-orange-100 tw:text-orange-700';
      default: return 'tw:bg-blue-100 tw:text-blue-700';
    }
  }
}
