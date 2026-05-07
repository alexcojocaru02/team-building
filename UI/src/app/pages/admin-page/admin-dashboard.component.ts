import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersService } from '../../services/users.service';
import { UserSummaryDto } from '../../services/users.service';
import { TeamDetailDto } from '../../models/auth.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  private usersService = inject(UsersService);

  users: UserSummaryDto[] = [];
  teams: TeamDetailDto[] = [];
  teamsWithoutOwner = 0;

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.usersService.getUsers().subscribe({
      next: (users) => {
        this.users = users;
      },
      error: (err) => {
        console.error('Failed to load users:', err);
      }
    });

    this.usersService.getAllTeams().subscribe({
      next: (teams) => {
        this.teams = teams;
        this.teamsWithoutOwner = teams.filter(t => !t.ownerId).length;
      },
      error: (err) => {
        console.error('Failed to load teams:', err);
      }
    });
  }
}
