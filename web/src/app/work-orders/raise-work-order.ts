import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { WorkOrderService } from '../core/work-order.service';
import { ApiFailure } from '../core/work-order.model';

/**
 * Rejects a quantity that is not a whole number.
 *
 * The number input allows a fraction whatever its step says, and the column
 * behind it holds whole numbers, so a typed 1.5 would otherwise travel to the
 * API only to be refused there.
 */
function wholeNumber(control: AbstractControl): ValidationErrors | null {
  const value = control.value as unknown;

  return typeof value === 'number' && !Number.isInteger(value) ? { wholeNumber: true } : null;
}

/** Matches the identifier format the API expects for lookups. */
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

@Component({
  selector: 'app-raise-work-order',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './raise-work-order.html',
  styleUrl: './raise-work-order.scss',
})
export class RaiseWorkOrder {
  private readonly builder = inject(FormBuilder);
  private readonly workOrders = inject(WorkOrderService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly failure = signal<ApiFailure | null>(null);

  protected readonly form = this.builder.nonNullable.group({
    customerId: ['', [Validators.required, Validators.pattern(GUID)]],
    equipmentId: ['', [Validators.required, Validators.pattern(GUID)]],
    lines: this.builder.array([this.newLine()]),
  });

  protected get lines(): FormArray<FormGroup> {
    return this.form.controls.lines as FormArray<FormGroup>;
  }

  protected addLine(): void {
    this.lines.push(this.newLine());
  }

  protected removeLine(index: number): void {
    // A job with no charges is allowed, so the last line may go too.
    this.lines.removeAt(index);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.failure.set(null);

    const value = this.form.getRawValue();

    this.workOrders
      .raise({
        customerId: value.customerId,
        equipmentId: value.equipmentId,
        lines: value.lines.map((line) => ({
          description: String(line['description']),
          quantity: Number(line['quantity']),
          unitPrice: Number(line['unitPrice']),
        })),
      })
      .subscribe({
        next: (created) => {
          this.submitting.set(false);
          void this.router.navigate(['/work-orders', created.id]);
        },
        error: (failure: ApiFailure) => {
          this.submitting.set(false);
          this.failure.set(failure);
        },
      });
  }

  private newLine(): FormGroup {
    return this.builder.nonNullable.group({
      description: ['', [Validators.required, Validators.maxLength(200)]],
      // The column behind this holds whole numbers between 1 and 10,000, so a
      // form that accepts anything else is offering a request the platform
      // refuses.
      quantity: [1, [Validators.required, Validators.min(1), Validators.max(10000), wholeNumber]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
    });
  }
}
