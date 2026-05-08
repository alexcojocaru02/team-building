import { Component, inject, OnInit, signal } from '@angular/core';
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

  users = signal<UserSummaryDto[]>([]);
  teams = signal<TeamDetailDto[]>([]);
  teamsWithoutOwner = signal(0);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.usersService.getUsers().subscribe({
      next: (users) => {
        this.users.set(users);
      },
      error: (err) => {
        console.error('Failed to load users:', err);
      }
    });

    this.usersService.getAllTeams().subscribe({
      next: (teams) => {
        this.teams.set(teams);
        this.teamsWithoutOwner.set(teams.filter(t => !t.ownerId).length);
      },
      error: (err) => {
        console.error('Failed to load teams:', err);
      }
    });
  }
}
