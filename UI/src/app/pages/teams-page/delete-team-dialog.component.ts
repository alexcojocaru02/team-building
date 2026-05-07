import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

interface DeleteTeamDialogData {
  teamName: string;
}

@Component({
  selector: 'app-delete-team-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './delete-team-dialog.component.html'
})
export class DeleteTeamDialogComponent {
  private dialogRef = inject(MatDialogRef<DeleteTeamDialogComponent>);
  protected data = inject<DeleteTeamDialogData>(MAT_DIALOG_DATA);

  cancel(): void {
    this.dialogRef.close(false);
  }

  confirm(): void {
    this.dialogRef.close(true);
  }
}