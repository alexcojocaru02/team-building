import { CommonModule } from '@angular/common';
import { Component, ViewEncapsulation } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-growth-page',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatInputModule,
    MatButtonModule,
    MatListModule,
    MatSelectModule,
    RouterModule,
  ],
  templateUrl: './growth-page.html',
  styleUrl: './growth-page.scss',
})
export class GrowthPage {
  goals: string[] = [
    'Improve communication in meetings',
    'Complete a course on leadership',
  ];
  selectedGoals: string[] = [];
  newGoal: string = '';
  progressValue = 68;

  impactBars = [
    { label: 'Team Collaboration', value: 80 },
    { label: 'Communication', value: 65 },
    { label: 'Reliability', value: 75 },
  ];

  receivedFeedback = [
    {
      from: 'Andrei M.',
      anonymous: false,
      interactionFrequency: 'Weekly',
      collaborationRating: 'Excellent',
      strengths: 'Responsiveness, clarity, ownership',
      message: 'Always quick to answer and very helpful with reviews!',
    },
    {
      from: '',
      anonymous: true,
      interactionFrequency: 'Monthly',
      collaborationRating: 'Good',
      strengths: 'Empathy, support',
      message: 'Appreciated your help last sprint!',
    },
  ];

  addGoal() {
    const trimmed = this.newGoal.trim();
    if (trimmed) {
      this.goals.push(trimmed);
      this.newGoal = '';
    }
  }
}
