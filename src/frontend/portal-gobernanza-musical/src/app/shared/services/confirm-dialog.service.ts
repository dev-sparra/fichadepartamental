import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';

import { ConfirmDialogComponent, ConfirmDialogData, ConfirmWithCommentResult } from '../ui/confirm-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  confirm(data: ConfirmDialogData): Observable<boolean> {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data,
      autoFocus: false,
      restoreFocus: true,
      panelClass: 'confirm-dialog-panel'
    });

    return dialogRef.afterClosed().pipe(map((result) => result === true));
  }

  confirmWithComment(data: Omit<ConfirmDialogData, 'showCommentField'>): Observable<ConfirmWithCommentResult> {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { ...data, showCommentField: true },
      autoFocus: false,
      restoreFocus: true,
      panelClass: 'confirm-dialog-panel'
    });

    return dialogRef.afterClosed().pipe(
      map((result) => (result && typeof result === 'object' ? (result as ConfirmWithCommentResult) : { confirmed: false, comment: null }))
    );
  }
}
