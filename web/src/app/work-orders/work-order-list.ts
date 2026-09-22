import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { WorkOrderService } from '../core/work-order.service';
import {
  ApiFailure,
  WORK_ORDER_STATUSES,
  WORK_ORDER_STATUS_LABELS,
  WorkOrderStatus,
  WorkOrderSummary,
} from '../core/work-order.model';

@Component({
  selector: 'app-work-order-list',
  imports: [
    CurrencyPipe,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  templateUrl: './work-order-list.html',
  styleUrl: './work-order-list.scss',
})
export class WorkOrderList {
  private readonly workOrders = inject(WorkOrderService);

  protected readonly statuses = WORK_ORDER_STATUSES;
  protected readonly columns = ['number', 'status', 'lineCount', 'totalPrice'] as const;

  protected readonly status = signal<WorkOrderStatus>('New');
  protected readonly jobs = signal<readonly WorkOrderSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly failure = signal<ApiFailure | null>(null);

  /** Where the next page starts, or null once the list has been read to the end. */
  protected readonly nextCursor = signal<string | null>(null);

  /**
   * Which request the screen is waiting for.
   *
   * Switching stage starts a request without cancelling the one before it, so
   * a slow first answer could otherwise arrive after a fast second one and
   * overwrite it. Only the answer to the latest request is taken.
   */
  private latestRequest = 0;

  constructor() {
    this.load();
  }

  /**
   * Material table rows arrive untyped, so the lookup goes through a method
   * rather than indexing a record with an implicit any in the template.
   */
  protected label(status: WorkOrderStatus): string {
    return WORK_ORDER_STATUS_LABELS[status];
  }

  protected onStatusChange(status: WorkOrderStatus): void {
    this.status.set(status);
    this.load();
  }

  protected load(): void {
    this.jobs.set([]);
    this.nextCursor.set(null);
    this.read(null);
  }

  protected loadMore(): void {
    const cursor = this.nextCursor();

    if (cursor) {
      this.read(cursor);
    }
  }

  private read(cursor: string | null): void {
    const request = ++this.latestRequest;

    this.loading.set(true);
    this.failure.set(null);

    this.workOrders.list(this.status(), cursor).subscribe({
      next: (page) => {
        if (request !== this.latestRequest) {
          return;
        }

        this.jobs.update((already) => (cursor ? [...already, ...page.items] : [...page.items]));
        this.nextCursor.set(page.nextCursor);
        this.loading.set(false);
      },
      error: (failure: ApiFailure) => {
        if (request !== this.latestRequest) {
          return;
        }

        this.failure.set(failure);
        this.jobs.set([]);
        this.nextCursor.set(null);
        this.loading.set(false);
      },
    });
  }
}
