import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ServiceItem, ServiceUpsertRequest } from '../../core/models';

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <header class="page-header">
      <div>
        <h2>Services</h2>
        <p>Create and maintain bookable appointment services.</p>
      </div>
      <button type="button" (click)="resetForm()">New service</button>
    </header>

    <section class="split-view">
      <form class="panel" [formGroup]="form" (ngSubmit)="save()">
        <h3>{{ editing() ? 'Edit service' : 'Create service' }}</h3>

        <label>
          Name
          <input type="text" formControlName="name">
        </label>

        <label>
          Description
          <textarea rows="4" formControlName="description"></textarea>
        </label>

        <div class="field-row">
          <label>
            Duration
            <input type="number" formControlName="durationMinutes">
          </label>

          <label>
            Price
            <input type="number" formControlName="price">
          </label>
        </div>

        <label class="check">
          <input type="checkbox" formControlName="isActive">
          Active
        </label>

        @if (error()) {
          <div class="error">{{ error() }}</div>
        }

        <div class="actions">
          <button type="submit" [disabled]="form.invalid || saving()">
            {{ saving() ? 'Saving...' : 'Save' }}
          </button>
          <button type="button" class="ghost" (click)="resetForm()">Clear</button>
        </div>
      </form>

      <section class="panel">
        <div class="table-header">
          <h3>Service list</h3>
          <button type="button" class="ghost" (click)="load()">Refresh</button>
        </div>

        <div class="table">
          @for (service of services(); track service.id) {
            <article class="table-row">
              <div>
                <strong>{{ service.name }}</strong>
                <span>{{ service.durationMinutes }} min · ${{ service.price }} · {{ service.isActive ? 'Active' : 'Inactive' }}</span>
              </div>
              <div class="row-actions">
                <button type="button" class="ghost" (click)="edit(service)">Edit</button>
                <button type="button" class="danger" (click)="remove(service)">Delete</button>
              </div>
            </article>
          } @empty {
            <p class="empty">No services found.</p>
          }
        </div>
      </section>
    </section>
  `
})
export class ServicesComponent implements OnInit {
  readonly services = signal<ServiceItem[]>([]);
  readonly editing = signal<ServiceItem | null>(null);
  readonly saving = signal(false);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: [''],
    durationMinutes: [30, [Validators.required, Validators.min(1), Validators.max(1440)]],
    price: [0, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: ApiService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.services().subscribe({
      next: (response) => this.services.set(response.items),
      error: () => this.error.set('Could not load services.')
    });
  }

  edit(service: ServiceItem): void {
    this.editing.set(service);
    this.error.set('');
    this.form.setValue({
      name: service.name,
      description: service.description ?? '',
      durationMinutes: service.durationMinutes,
      price: service.price,
      isActive: service.isActive
    });
  }

  save(): void {
    if (this.form.invalid) {
      return;
    }

    const request = this.form.getRawValue() as ServiceUpsertRequest;
    const current = this.editing();
    const operation = current
      ? this.api.updateService(current.id, request)
      : this.api.createService(request);

    this.saving.set(true);
    this.error.set('');

    operation.subscribe({
      next: () => {
        this.resetForm();
        this.load();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Could not save service.');
        this.saving.set(false);
      },
      complete: () => this.saving.set(false)
    });
  }

  remove(service: ServiceItem): void {
    if (!confirm(`Delete ${service.name}?`)) {
      return;
    }

    this.api.deleteService(service.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err.error?.message ?? 'Could not delete service.')
    });
  }

  resetForm(): void {
    this.editing.set(null);
    this.error.set('');
    this.form.reset({
      name: '',
      description: '',
      durationMinutes: 30,
      price: 0,
      isActive: true
    });
  }
}
