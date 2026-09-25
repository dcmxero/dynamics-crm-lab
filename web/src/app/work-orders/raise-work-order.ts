import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, filter, switchMap } from 'rxjs';
import { CatalogueService } from '../core/catalogue.service';
import { WorkOrderService } from '../core/work-order.service';
import { ApiFailure, Customer, Equipment } from '../core/work-order.model';

/**
 * Rejects a quantity that is not a whole number.
 *
 * The number input allows a fraction whatever its step says, and the column
 * behind it holds whole numbers, so a typed 1.5 would otherwise travel to the
 * API only to be refused there.
 */
/**
 * Rejects a name that was typed but never chosen from the list.
 *
 * The control holds the chosen record, so a leftover string means nobody was
 * picked. Without this the form would look filled in and the API would be
 * handed a name where an identifier belongs.
 */
function chosenFromTheList(control: AbstractControl): ValidationErrors | null {
  return typeof control.value === 'string' && control.value.length > 0 ? { notChosen: true } : null;
}

function wholeNumber(control: AbstractControl): ValidationErrors | null {
  const value = control.value as unknown;

  return typeof value === 'number' && !Number.isInteger(value) ? { wholeNumber: true } : null;
}

@Component({
  selector: 'app-raise-work-order',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatAutocompleteModule,
    MatProgressBarModule,
  ],
  templateUrl: './raise-work-order.html',
  styleUrl: './raise-work-order.scss',
})
export class RaiseWorkOrder {
  private readonly builder = inject(FormBuilder);
  private readonly workOrders = inject(WorkOrderService);
  private readonly catalogue = inject(CatalogueService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly failure = signal<ApiFailure | null>(null);

  protected readonly customers = signal<Customer[]>([]);
  protected readonly searching = signal(false);
  protected readonly equipment = signal<Equipment[]>([]);
  protected readonly loadingEquipment = signal(false);

  // The controls hold the chosen record, not text. A name that was typed but
  // never chosen is not a customer, and the form has to be able to tell.
  protected readonly customer = signal<Customer | null>(null);

  protected readonly form = this.builder.nonNullable.group({
    customer: this.builder.nonNullable.control<Customer | string>('', [
      Validators.required,
      chosenFromTheList,
    ]),
    // Disabled until somebody is chosen: there is nothing to choose from, and
    // reactive forms want that said here rather than bound in the template.
    equipmentId: [{ value: '', disabled: true }, Validators.required],
    lines: this.builder.array([this.newLine()]),
  });

  constructor() {
    this.form.controls.customer.valueChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        filter((value): value is string => typeof value === 'string'),
        switchMap((typed) => {
          // Typing again means the chosen customer no longer holds, so the
          // units offered stop belonging to anybody.
          this.chose(null);
          this.searching.set(true);

          return this.catalogue.findCustomers(typed);
        }),
        takeUntilDestroyed(),
      )
      .subscribe({
        next: (found) => {
          this.searching.set(false);
          this.customers.set(found);
        },
        error: () => {
          this.searching.set(false);
          this.customers.set([]);
        },
      });
  }

  protected nameOf(customer: Customer | string | null): string {
    return typeof customer === 'string' ? customer : (customer?.name ?? '');
  }

  protected chosen(customer: Customer): void {
    this.chose(customer);
    this.loadingEquipment.set(true);

    this.catalogue.equipmentOf(customer.id).subscribe({
      next: (found) => {
        this.loadingEquipment.set(false);
        this.equipment.set(found);

        // One unit is not a choice, so it is made for them.
        if (found.length === 1) {
          this.form.controls.equipmentId.setValue(found[0].id);
        }
      },
      error: () => {
        this.loadingEquipment.set(false);
        this.equipment.set([]);
      },
    });
  }

  private chose(customer: Customer | null): void {
    this.customer.set(customer);
    this.equipment.set([]);
    this.form.controls.equipmentId.setValue('');

    if (customer === null) {
      this.form.controls.equipmentId.disable();
    } else {
      this.form.controls.equipmentId.enable();
    }
  }

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
        customerId: this.customer()!.id,
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
