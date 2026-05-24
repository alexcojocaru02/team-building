import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';
import { ICONS } from '../../shared/icons';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss'
})
export class HomePage {
  private authService = inject(AuthService);
  private sanitizer = inject(DomSanitizer);

  currentUser = this.authService.currentUser;

  icons = {
    feed: this.sanitizer.bypassSecurityTrustHtml(ICONS.feed),
    feedback: this.sanitizer.bypassSecurityTrustHtml(ICONS.feedback),
    dashboard: this.sanitizer.bypassSecurityTrustHtml(ICONS.dashboard)
  };

  logout(): void {
    this.authService.logout();
  }
}
