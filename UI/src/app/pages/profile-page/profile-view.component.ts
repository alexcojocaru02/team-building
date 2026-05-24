import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { UserDto } from '../../models/auth.models';

@Component({
  selector: 'app-profile-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule],
  templateUrl: './profile-view.component.html',
  styleUrls: ['./profile-view.component.scss']
})
export class ProfileViewComponent implements OnInit {
  private authService = inject(AuthService);
  private usersService = inject(UsersService);
  private router = inject(Router);

  profile = signal<UserDto | null>(null);

  ngOnInit() {
    const currentUser = this.authService.currentUser();
    if (currentUser?.id) {
      this.usersService.getUserProfile(currentUser.id).subscribe({
        next: (profile) => {
            this.profile.set(profile);
          },
        error: (err) => {
          console.error('Failed to load profile:', err);
        }
      });
    }
  }

  editProfile() {
    // Navigate to edit page
    this.router.navigate(['/profile/edit']);
  }

  logout(): void {
    this.authService.logout();
  }
}
