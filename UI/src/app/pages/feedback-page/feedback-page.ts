import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-feedback-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  templateUrl: './feedback-page.html',
  styleUrl: './feedback-page.scss',
})
export class FeedbackPage {
  feedbackForm: FormGroup;

  users = [
    { id: '1', name: 'Alexandra Ionescu' },
    { id: '2', name: 'Mihai Popescu' },
    { id: '3', name: 'Ioana Marinescu' },
  ];

  constructor(private fb: FormBuilder, private router: Router) {
    this.feedbackForm = this.fb.group({
      recipient: [''],
      interactionFrequency: [''],
      collaborationRating: [''],
      strengths: [''],
      message: [''],
    });
  }

  submitFeedback() {
    if (this.feedbackForm.valid) {
      console.log(this.feedbackForm.value);
      this.feedbackForm.reset();
    }
  }
}
