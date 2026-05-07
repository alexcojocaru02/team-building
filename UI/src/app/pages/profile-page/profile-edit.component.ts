import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { UpdateProfileDto, UserDto } from '../../models/auth.models';

@Component({
  selector: 'app-profile-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile-edit.component.html',
  styleUrls: ['./profile-edit.component.scss']
})
export class ProfileEditComponent implements OnInit {
  private authService = inject(AuthService);
  private usersService = inject(UsersService);
  private _router = inject(Router);

  get router() {
    return this._router;
  }

  profile = signal<UserDto | null>(null);
  formData = signal<UpdateProfileDto>({});
  isSaving = signal(false);
  saveMessage = signal('');
  saveError = signal('');

  get currentUser() {
    return this.authService.currentUser();
  }

  get hobbiesInput(): string {
    return (this.formData().hobbies || []).join(', ');
  }

  get strengthsInput(): string {
    return (this.formData().strengths || []).join(', ');
  }

  ngOnInit() {
    const currentUser = this.authService.currentUser();
    if (currentUser?.id) {
      this.usersService.getUserProfile(currentUser.id).subscribe({
        next: (profile) => {
          this.profile.set(profile);
          // Update signal with profile data
          this.formData.set({
            bio: profile.bio,
            avatarUrl: profile.avatarUrl,
            department: profile.department,
            location: profile.location,
            timezone: profile.timezone,
            pronouns: profile.pronouns,
            preferredWorkStyle: profile.preferredWorkStyle,
            hobbies: profile.hobbies,
            strengths: profile.strengths,
            icebreaker: profile.icebreaker
          });
        },
        error: (err) => {
          this.saveError.set('Failed to load profile');
          console.error('Failed to load profile:', err);
        }
      });
    }
  }

  updateHobbies(event: Event): void {
    const input = (event.target as HTMLInputElement).value;
    const currentData = this.formData();
    currentData.hobbies = input
      .split(',')
      .map(h => h.trim())
      .filter(h => h.length > 0);
    this.formData.set(currentData);
  }

  updateStrengths(event: Event): void {
    const input = (event.target as HTMLInputElement).value;
    const currentData = this.formData();
    currentData.strengths = input
      .split(',')
      .map(s => s.trim())
      .filter(s => s.length > 0);
    this.formData.set(currentData);
  }

  onSubmit(): void {
    const currentUser = this.authService.currentUser();
    if (!currentUser?.id) {
      this.saveError.set('User ID not found');
      return;
    }

    this.isSaving.set(true);
    this.saveMessage.set('');
    this.saveError.set('');

    this.usersService.updateProfile(currentUser.id, this.formData()).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.saveMessage.set('Profile updated successfully!');
        setTimeout(() => {
          this._router.navigate(['/profile']);
        }, 1500);
      },
      error: (err) => {
        this.isSaving.set(false);
        this.saveError.set('Failed to update profile');
        console.error('Failed to update profile:', err);
      }
    });
  }
}
