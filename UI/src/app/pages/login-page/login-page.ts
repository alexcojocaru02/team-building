import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';
import { LoginDto, RegisterDto } from '../../models/auth.models';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.html',
  styleUrls: ['./login-page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule],
})
export class LoginPage {
  private authService = inject(AuthService);
  private router = inject(Router);

  isLoginMode = signal(true);
  isLoading = signal(false);
  errorMessage = signal('');

  loginDto: LoginDto = {
    email: '',
    password: ''
  };

  registerDto: RegisterDto = {
    fullName: '',
    email: '',
    password: ''
  };

  toggleMode(): void {
    this.isLoginMode.update(v => !v);
    this.errorMessage.set('');
  }

  onSubmit(): void {
    this.errorMessage.set('');
    this.isLoading.set(true);

    if (this.isLoginMode()) {
      this.authService.login(this.loginDto).subscribe({
        next: () => {
          this.isLoading.set(false);
        },
        error: (error) => {
          this.isLoading.set(false);
          this.errorMessage.set('Invalid email or password');
        }
      });
    } else {
      this.authService.register(this.registerDto).subscribe({
        next: () => {
          this.isLoading.set(false);
          this.errorMessage.set('');
          // Optionally auto-login or show success message
          this.isLoginMode.set(true);
        },
        error: (error) => {
          this.isLoading.set(false);
          this.errorMessage.set(error.error?.message || 'Registration failed. User may already exist.');
        }
      });
    }
  }
}
