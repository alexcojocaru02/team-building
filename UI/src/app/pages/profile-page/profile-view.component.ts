import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { UserDto } from '../../models/auth.models';
import { ConfirmDialogComponent } from '../teams-page/confirm-dialog.component';

@Component({
  selector: 'app-profile-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, MatDialogModule],
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

  private readonly avatarPalette = [
    '#6366f1', '#8b5cf6', '#ec4899', '#f59e0b',
    '#10b981', '#3b82f6', '#ef4444', '#14b8a6',
  ];

  getInitials(value: string | null | undefined): string {
    const trimmed = (value || '').trim();
    if (!trimmed) return 'U';
    const parts = trimmed.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return trimmed.slice(0, 2).toUpperCase();
  }

  getAvatarColor(value: string | null | undefined): string {
    const key = (value || '').trim().toLowerCase();
    if (!key) return this.avatarPalette[0];
    let hash = 0;
    for (let i = 0; i < key.length; i++) hash = key.charCodeAt(i) + ((hash << 5) - hash);
    return this.avatarPalette[Math.abs(hash) % this.avatarPalette.length];
  }
}
