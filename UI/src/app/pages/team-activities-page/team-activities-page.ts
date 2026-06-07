import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { TeamActivitiesService, CreateTeamActivityDto, SubmitTeamActivityResponseDto, TeamActivityDto, TeamActivitySummaryDto } from '../../services/team-activities.service';
import { TeamDetailDto, UserDto } from '../../models/auth.models';
import { CreateTeamActivityDialogComponent } from './create-team-activity-dialog.component';
import { SyncMeetingDialogComponent, SyncMeetingDialogResult } from './sync-meeting-dialog.component';
import { ColleagueProfileDialogComponent } from '../../shared/colleague-profile-dialog.component';

type ActivityFilter = 'open' | 'closed' | 'all';
type ActivityCategoryFilter = 'all' | 'meeting' | 'async';

@Component({
  selector: 'app-team-activities-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDialogModule,
    MatFormFieldModule,
    MatRadioModule,
    MatSelectModule,
    MatSnackBarModule,
    MatInputModule,
    MatIconModule,
  ],
  templateUrl: './team-activities-page.html',
  styleUrls: ['./team-activities-page.scss'],
})
export class TeamActivitiesPage implements OnInit {
  private destroyRef = inject(DestroyRef);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private usersService = inject(UsersService);
  private teamActivitiesService = inject(TeamActivitiesService);

  currentUser = this.authService.currentUser;

  teams = signal<TeamDetailDto[]>([]);
  myProfile = signal<UserDto | null>(null);
  activities = signal<TeamActivityDto[]>([]);
  summary = signal<TeamActivitySummaryDto | null>(null);
  // Controls whether the summary cards are collapsed
  summaryCollapsed = signal(true);
  isLoadingTeams = signal(true);
  isLoadingActivities = signal(false);
  isSubmitting = signal(false);
  selectedTeamId = signal('');
  activityFilter = signal<ActivityFilter>('open');
  activityCategoryFilter = signal<ActivityCategoryFilter>('all');
  private teamDataRequestId = 0;

  responseDrafts = signal<Record<string, string>>({});
  selectedPollOptions = signal<Record<string, number | null>>({});
  // Per-activity collapse state: true = collapsed
  activityCollapsed = signal<Record<string, boolean>>({});

  selectedTeam = computed(() => {
    const teamId = this.selectedTeamId();
    return this.teams().find(team => team.id === teamId) ?? null;
  });

  canManageSelectedTeam = computed(() => {
    const team = this.selectedTeam();
    const currentUser = this.currentUser();

    if (!team || !currentUser) {
      return false;
    }

    return this.authService.isAdmin() || team.ownerId === currentUser.id;
  });

  availableTeams = computed(() => {
    const profile = this.myProfile();
    const teams = this.teams();

    if (this.authService.isAdmin()) {
      return teams;
    }

    const teamIds = profile?.teamIds ?? [];
    return teams.filter(team => teamIds.includes(team.id));
  });

  visibleActivities = computed(() => {
    const filter = this.canManageSelectedTeam() ? this.activityFilter() : 'open';
    const categoryFilter = this.activityCategoryFilter();
    const items = this.activities();

    const statusFiltered = filter === 'all'
      ? items
      : filter === 'closed'
        ? items.filter(activity => activity.status === 'Closed')
        : items.filter(activity => activity.status === 'Open');

    const filtered = categoryFilter === 'all'
      ? statusFiltered
      : categoryFilter === 'meeting'
        ? statusFiltered.filter(activity => activity.activityType.toLowerCase() === 'syncmeeting')
        : statusFiltered.filter(activity => activity.activityType.toLowerCase() !== 'syncmeeting');

    return [...filtered].sort((left, right) => {
      const leftResponded = left.hasCurrentUserResponded ? 1 : 0;
      const rightResponded = right.hasCurrentUserResponded ? 1 : 0;

      if (leftResponded !== rightResponded) {
        return leftResponded - rightResponded;
      }

      return new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
    });
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const teamId = params.get('teamId') ?? '';
      if (teamId) {
        this.selectedTeamId.set(teamId);
        this.usersService.getTeam(teamId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (team) => this.teams.set([team]),
        });
        this.loadTeamData(teamId);
      }
    });
  }

  openCreateActivityDialog(): void {
    const teamId = this.selectedTeamId();
    if (!teamId) {
      this.showSnackBar('Please select a team first.', true);
      return;
    }

    const dialogRef = this.dialog.open(CreateTeamActivityDialogComponent, {
      width: '720px',
      maxWidth: '95vw',
      data: {
        teamName: this.getTeamName(teamId)
      }
    });

    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result?: CreateTeamActivityDto) => {
      if (!result) {
        return;
      }

      this.createActivity(result);
    });
  }

  sameDay(start?: string | null, end?: string | null): boolean {
    if (!start || !end) return false;
    const startDate = new Date(start);
    const endDate = new Date(end);
    return startDate.getFullYear() === endDate.getFullYear()
      && startDate.getMonth() === endDate.getMonth()
      && startDate.getDate() === endDate.getDate();
  }

  openSyncMeetingDialog(activity: TeamActivityDto): void {
    const dialogRef = this.dialog.open(SyncMeetingDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      data: {
        activity,
        canManage: this.canManageSelectedTeam()
      }
    });

    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result?: SyncMeetingDialogResult) => {
      if (!result) return;

      if (result.action === 'rsvp') {
        this.submitRsvp(activity, result.rsvpStatus);
      } else if (result.action === 'close') {
        this.completeActivity(activity);
      }
    });
  }

  private submitRsvp(activity: TeamActivityDto, rsvpStatus: 'Accepted' | 'Declined'): void {
    const teamId = this.selectedTeamId();
    if (!teamId) return;

    this.teamActivitiesService.respondToActivity(teamId, activity.id, { rsvpStatus }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.activities.update(items => items.map(item => item.id === updated.id ? updated : item));
        this.showSnackBar(`RSVP recorded: ${rsvpStatus}.`);
      },
      error: (error) => {
        this.showSnackBar(this.getErrorMessage(error, 'Failed to submit RSVP.'), true);
        console.error('Error submitting RSVP:', error);
      }
    });
  }

  loadInitialData(): void {
    this.isLoadingTeams.set(true);

    this.usersService.getMyProfile().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile) => {
        this.myProfile.set(profile);
        this.loadTeams(profile);
      },
      error: (error) => {
        this.isLoadingTeams.set(false);
        this.showSnackBar(this.getErrorMessage(error, 'Failed to load profile.'), true);
        console.error('Error loading profile:', error);
      }
    });
  }

  loadTeams(profile: UserDto): void {
    this.usersService.getAllTeams().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (teams) => {
        this.teams.set(teams);
        this.isLoadingTeams.set(false);

        const teamIds = this.authService.isAdmin() ? teams.map(team => team.id) : (profile.teamIds ?? []);
        const available = teams.filter(team => teamIds.includes(team.id));
        const nextTeamId = available[0]?.id ?? '';
        this.selectedTeamId.set(nextTeamId);

        if (nextTeamId) {
          this.loadTeamData(nextTeamId);
        }
      },
      error: (error) => {
        this.isLoadingTeams.set(false);
        this.showSnackBar(this.getErrorMessage(error, 'Failed to load teams.'), true);
        console.error('Error loading teams:', error);
      }
    });
  }

  onTeamChange(teamId: string): void {
    this.selectedTeamId.set(teamId);
    this.loadTeamData(teamId);
  }

  loadTeamData(teamId: string): void {
    const requestId = ++this.teamDataRequestId;

    if (!teamId) {
      this.activities.set([]);
      this.summary.set(null);
      this.isLoadingActivities.set(false);
      return;
    }

    this.isLoadingActivities.set(true);

    this.teamActivitiesService.getTeamActivities(teamId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (activities) => {
        if (requestId !== this.teamDataRequestId) {
          return;
        }

        this.activities.set(activities);
        // Ensure we have a collapse flag for each activity and collapse items already answered by the current user.
        this.activityCollapsed.update(map => {
          const copy = { ...map };
          for (const a of activities) {
            if (!(a.id in copy)) copy[a.id] = true;
            if (a.hasCurrentUserResponded) copy[a.id] = true;
          }
          return copy;
        });
        this.isLoadingActivities.set(false);
      },
      error: (error) => {
        if (requestId !== this.teamDataRequestId) {
          return;
        }

        this.isLoadingActivities.set(false);
        this.activities.set([]);
        this.showSnackBar(this.getErrorMessage(error, 'Failed to load activities.'), true);
        console.error('Error loading activities:', error);
      }
    });

    this.teamActivitiesService.getTeamActivitySummary(teamId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (summary) => {
        if (requestId !== this.teamDataRequestId) {
          return;
        }

        this.summary.set(summary);
      },
      error: (error) => {
        if (requestId !== this.teamDataRequestId) {
          return;
        }

        this.summary.set(null);
        this.showSnackBar(this.getErrorMessage(error, 'Failed to load activity summary.'), true);
        console.error('Error loading activity summary:', error);
      }
    });
  }

  private createActivity(dto: CreateTeamActivityDto): void {
    const teamId = this.selectedTeamId();
    if (!teamId) return;

    this.isSubmitting.set(true);

    this.teamActivitiesService.createTeamActivity(teamId, dto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (activity) => {
        this.activities.update(items => [activity, ...items]);
        this.summary.update(summary => summary ? {
          ...summary,
          totalActivities: summary.totalActivities + 1,
          openActivities: summary.openActivities + 1,
          recentActivitiesCount: summary.recentActivitiesCount + 1,
        } : summary);
        this.showSnackBar('Activity created successfully.');
        this.isSubmitting.set(false);
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.showSnackBar(this.getErrorMessage(error, 'Failed to create activity.'), true);
        console.error('Error creating activity:', error);
      }
    });
  }

  submitResponse(activity: TeamActivityDto): void {
    const teamId = this.selectedTeamId();
    if (!teamId) return;

    const isPoll = activity.activityType.toLowerCase() === 'poll';
    const dto: SubmitTeamActivityResponseDto = isPoll
      ? { selectedOptionIndex: this.getSelectedPollOption(activity.id) }
      : { textResponse: this.getResponseDraft(activity.id).trim() };

    if (isPoll && dto.selectedOptionIndex == null) {
      this.showSnackBar('Please choose an option.', true);
      return;
    }
    if (!isPoll && !dto.textResponse?.trim()) {
      this.showSnackBar('Please enter a response.', true);
      return;
    }

    this.teamActivitiesService.respondToActivity(teamId, activity.id, dto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.activities.update(items => items.map(item => item.id === updated.id ? updated : item));
        this.setResponseDraft(activity.id, '');
        this.setSelectedPollOption(activity.id, null);
        this.activityCollapsed.update(map => ({ ...map, [updated.id]: true }));
        this.showSnackBar('Response submitted.');
      },
      error: (error) => {
        this.showSnackBar(this.getErrorMessage(error, 'Failed to submit response.'), true);
        console.error('Error submitting response:', error);
      }
    });
  }

  completeActivity(activity: TeamActivityDto): void {
    const teamId = this.selectedTeamId();
    if (!teamId) return;

    this.teamActivitiesService.completeActivity(teamId, activity.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.activities.update(items => items.map(item => item.id === updated.id ? updated : item));
        this.summary.update(summary => summary ? {
          ...summary,
          openActivities: Math.max(0, summary.openActivities - 1),
          closedActivities: summary.closedActivities + 1,
        } : summary);
        this.showSnackBar('Activity closed.');
      },
      error: (error) => {
        this.showSnackBar(this.getErrorMessage(error, 'Failed to close activity.'), true);
        console.error('Error closing activity:', error);
      }
    });
  }

  activityTypeLabel(activityType: string): string {
    return activityType.toLowerCase() === 'syncmeeting' ? 'Meeting' : activityType;
  }

  displayName(userId: string, fullName: string | null | undefined, email: string | null | undefined): string {
    if (userId === this.currentUser()?.id) return 'You';
    return fullName || email || userId;
  }

  getTeamName(teamId: string): string {
    return this.teams().find(team => team.id === teamId)?.name ?? 'Selected team';
  }

  getResponseDraft(activityId: string): string {
    return this.responseDrafts()[activityId] ?? '';
  }

  setResponseDraft(activityId: string, value: string): void {
    this.responseDrafts.update(drafts => ({ ...drafts, [activityId]: value }));
  }

  getSelectedPollOption(activityId: string): number | null {
    return this.selectedPollOptions()[activityId] ?? null;
  }

  setSelectedPollOption(activityId: string, value: number | null): void {
    this.selectedPollOptions.update(options => ({ ...options, [activityId]: value }));
  }

  setActivityFilter(filter: ActivityFilter): void {
    this.activityFilter.set(filter);
  }

  setActivityCategoryFilter(filter: ActivityCategoryFilter): void {
    this.activityCategoryFilter.set(filter);
  }

  isActivityCollapsed(activityId: string): boolean {
    return !!this.activityCollapsed()[activityId];
  }

  toggleActivityCollapse(activityId: string): void {
    this.activityCollapsed.update(map => ({ ...map, [activityId]: !map[activityId] }));
  }

  toggleSummary(): void {
    this.summaryCollapsed.update(v => !v);
  }

  openProfile(userId: string): void {
    this.dialog.open(ColleagueProfileDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { userId }
    });
  }

  getInitials(value: string | null | undefined): string {
    const trimmed = (value || '').trim();
    if (!trimmed) return 'U';

    const parts = trimmed.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }

    return trimmed.slice(0, 2).toUpperCase();
  }

  getAvatarColor(value: string | null | undefined): string {
    const palette = [
      '#0f766e', '#0ea5e9', '#2563eb', '#4f46e5', '#7c3aed',
      '#c026d3', '#db2777', '#ea580c', '#d97706', '#16a34a'
    ];

    const key = (value || '').trim().toLowerCase();
    if (!key) return palette[0];

    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = (hash << 5) - hash + key.charCodeAt(i);
      hash |= 0;
    }

    const index = Math.abs(hash) % palette.length;
    return palette[index];
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