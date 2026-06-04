import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersService } from '../../services/users.service';
import { UserSummaryDto } from '../../services/users.service';
import { TeamDetailDto, TeamJoinRequestDto } from '../../models/auth.models';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatSnackBarModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  private usersService = inject(UsersService);
  private snackBar = inject(MatSnackBar);

  users = signal<UserSummaryDto[]>([]);
  teams = signal<TeamDetailDto[]>([]);
  teamsWithoutOwner = signal(0);
  joinRequests = signal<TeamJoinRequestDto[]>([]);
  processingRequestId = signal<string | null>(null);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.usersService.getUsers().subscribe({
      next: (users) => this.users.set(users),
      error: (err) => console.error('Failed to load users:', err)
    });

    this.usersService.getAllTeams().subscribe({
      next: (teams) => {
        this.teams.set(teams);
        this.teamsWithoutOwner.set(teams.filter(t => !t.ownerId).length);
      },
      error: (err) => console.error('Failed to load teams:', err)
    });

    this.loadJoinRequests();
  }

  loadJoinRequests() {
    this.usersService.getAllJoinRequests().subscribe({
      next: (requests) => this.joinRequests.set(requests),
      error: (err) => console.error('Failed to load join requests:', err)
    });
  }

  approveRequest(request: TeamJoinRequestDto) {
    this.processingRequestId.set(request.id);
    this.usersService.approveJoinRequest(request.id).subscribe({
      next: () => {
        this.processingRequestId.set(null);
        this.snackBar.open(`Approved ${request.userFullName || request.userEmail} for "${request.teamName}".`, 'Dismiss', { duration: 3000 });
        this.loadData();
      },
      error: (err) => {
        console.error('Failed to approve request:', err);
        this.processingRequestId.set(null);
        this.snackBar.open('Failed to approve request.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  rejectRequest(request: TeamJoinRequestDto) {
    this.processingRequestId.set(request.id);
    this.usersService.rejectJoinRequest(request.id).subscribe({
      next: () => {
        this.processingRequestId.set(null);
        this.snackBar.open(`Rejected request from ${request.userFullName || request.userEmail}.`, 'Dismiss', { duration: 3000 });
        this.loadData();
      },
      error: (err) => {
        console.error('Failed to reject request:', err);
        this.processingRequestId.set(null);
        this.snackBar.open('Failed to reject request.', 'Dismiss', { duration: 3000 });
      }
    });
  }
}
