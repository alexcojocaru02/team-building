import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../services/auth.service';
import { UsersService } from '../services/users.service';
import { UserDto } from '../models/auth.models';
import { UserAvatarComponent } from './user-avatar.component';

export interface ColleagueProfileDialogData {
  userId: string;
}

@Component({
  selector: 'app-colleague-profile-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, UserAvatarComponent],
  templateUrl: './colleague-profile-dialog.component.html',
  styleUrl: './colleague-profile-dialog.component.scss',
})
export class ColleagueProfileDialogComponent implements OnInit {
  private dialogRef = inject(MatDialogRef<ColleagueProfileDialogComponent>);
  private usersService = inject(UsersService);
  private authService = inject(AuthService);
  protected data = inject<ColleagueProfileDialogData>(MAT_DIALOG_DATA);

  profile = signal<UserDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');

  protected readonly title = this.data.userId === this.authService.currentUser()?.id
    ? 'Your profile'
    : 'Colleague profile';

  ngOnInit(): void {
    this.usersService.getUserProfile(this.data.userId).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load profile.');
        this.isLoading.set(false);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
