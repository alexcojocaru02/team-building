import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export type ConfirmDialogButtonColor = 'primary' | 'accent' | 'warn';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  confirmColor?: ConfirmDialogButtonColor;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './confirm-dialog.component.html'
})
export class ConfirmDialogComponent {
  private dialogRef = inject(MatDialogRef<ConfirmDialogComponent, boolean>);
  protected data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);

  protected readonly confirmText = this.data.confirmText ?? 'Confirm';
  protected readonly cancelText = this.data.cancelText ?? 'Cancel';
  protected readonly confirmColor: ConfirmDialogButtonColor = this.data.confirmColor ?? 'primary';

  cancel(): void {
    this.dialogRef.close(false);
  }

  confirm(): void {
    this.dialogRef.close(true);
  }
}