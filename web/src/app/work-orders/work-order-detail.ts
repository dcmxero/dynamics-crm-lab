import { CurrencyPipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Observable } from 'rxjs';
import { WorkOrderService } from '../core/work-order.service';
import { ApiFailure, WORK_ORDER_STATUS_LABELS, WorkOrder } from '../core/work-order.model';

@Component({
  selector: 'app-work-order-detail',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  templateUrl: './work-order-detail.html',
  styleUrl: './work-order-detail.scss',
})
export class WorkOrderDetail {
  private readonly workOrders = inject(WorkOrderService);
  private readonly snackBar = inject(MatSnackBar);

  /** Bound from the route by withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly labels = WORK_ORDER_STATUS_LABELS;

  protected readonly columns = ['description', 'quantity', 'unitPrice', 'lineTotal'] as const;

  protected readonly job = signal<WorkOrder | null>(null);
  protected readonly loading = signal(false);
  protected readonly working = signal(false);
  protected readonly failure = signal<ApiFailure | null>(null);

  protected readonly resolution = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(2000)],
  });

  constructor() {
    // Wait a tick so the route input is set before the first read.
    queueMicrotask(() => this.load());
  }

  protected load(): void {
    this.loading.set(true);
    this.failure.set(null);

    this.workOrders.get(this.id()).subscribe({
      next: (job) => {
        this.job.set(job);
        this.loading.set(false);
      },
      error: (failure: ApiFailure) => {
        this.failure.set(failure);
        this.loading.set(false);
      },
    });
  }

  protected assign(): void {
    this.run(
      this.workOrders.assign(this.id(), null),
      (assigned) => `Assigned to ${assigned.technicianName}.`,
    );
  }

  protected close(): void {
    if (this.resolution.invalid) {
      this.resolution.markAsTouched();
      return;
    }

    this.run(
      this.workOrders.close(this.id(), this.resolution.getRawValue()),
      (closed) => `${closed.number} closed, ${closed.totalPrice} ${closed.currency} to invoice.`,
    );
  }

  private run<T>(call: Observable<T>, describe: (value: T) => string): void {
    this.working.set(true);

    call.subscribe({
      next: (value) => {
        this.working.set(false);
        this.snackBar.open(describe(value), 'Dismiss', { duration: 5000 });
        this.load();
      },
      error: (failure: ApiFailure) => {
        this.working.set(false);
        // A broken rule is the job answering back, not a fault, so it is shown
        // in place rather than thrown at the user as an error dialog.
        this.snackBar.open(failure.message, 'Dismiss', { duration: 8000 });
      },
    });
  }
}
