import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { UserDto } from '../../models/auth.models';
import { ConfirmDialogComponent } from '../teams-page/confirm-dialog.component';
import { UserAvatarComponent } from '../../shared/user-avatar.component';

@Component({
  selector: 'app-profile-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule, MatDialogModule, UserAvatarComponent],
  templateUrl: './profile-view.component.html',
  styleUrls: ['./profile-view.component.scss']
})
export class ProfileViewComponent implements OnInit {
  private authService = inject(AuthService);
  private usersService = inject(UsersService);
  private router = inject(Router);
  private dialog = inject(MatDialog);

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

  isProfileIncomplete(p: UserDto): boolean {
    return !p.bio && !p.department && !p.location && !p.icebreaker
      && !(p.hobbies?.length) && !(p.strengths?.length);
  }

  editProfile() {
    // Navigate to edit page
    this.router.navigate(['/profile/edit']);
  }

  logout(): void {
    this.authService.logout();
  }

  requestDeleteAccount(): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Delete account',
        message: 'Are you sure you want to permanently delete your account? This action cannot be undone.',
        confirmText: 'Delete account',
        cancelText: 'Cancel',
        confirmColor: 'warn',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;

      this.authService.deleteAccount().subscribe({
        error: (err) => console.error('Failed to delete account:', err),
      });
    });
  }

}
