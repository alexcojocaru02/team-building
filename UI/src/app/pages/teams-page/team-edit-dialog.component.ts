import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CreateTeamDto } from '../../models/auth.models';

export interface TeamEditDialogData {
  team: CreateTeamDto;
  title?: string;
  confirmText?: string;
}

export type TeamEditDialogResult = CreateTeamDto;

@Component({
  selector: 'app-team-edit-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './team-edit-dialog.component.html'
})
export class TeamEditDialogComponent {
  private dialogRef = inject(MatDialogRef<TeamEditDialogComponent, TeamEditDialogResult | undefined>);
  protected data = inject<TeamEditDialogData>(MAT_DIALOG_DATA);

  protected readonly title = this.data.title ?? 'Edit team';
  protected readonly confirmText = this.data.confirmText ?? 'Save';
  protected teamName = this.data.team.name;
  protected teamDescription = this.data.team.description ?? '';
  protected errorMessage = '';
  protected isSaving = false;

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    const name = this.teamName.trim();
    const description = this.teamDescription.trim();

    if (!name) {
      this.errorMessage = 'Team name is required.';
      return;
    }

    this.errorMessage = '';
    this.isSaving = true;
    this.dialogRef.close({
      name,
      description: description || undefined
    });
  }
}