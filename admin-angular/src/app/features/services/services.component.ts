import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { ApiService } from '../../core/api.service';
import { ServiceItem, ServiceUpsertRequest } from '../../core/models';

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [CurrencyPipe, ReactiveFormsModule],
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
          <button type="button" class="ghost" (click)="load()" [disabled]="loading()">
            {{ loading() ? 'Loading...' : 'Refresh' }}
          </button>
        </div>

        @if (loading()) {
          <p class="muted">Loading services...</p>
        }

        <label>
          Search
          <input type="search" [value]="search()" (input)="search.set($any($event.target).value)" placeholder="Search services">
        </label>

        <div class="table">
          @for (service of pagedServices(); track service.id) {
            <article class="table-row">
              <div>
                <strong>{{ service.name }}</strong>
                <span>{{ service.durationMinutes }} min | {{ service.price | currency }} | {{ service.isActive ? 'Active' : 'Inactive' }}</span>
              </div>
              <div class="row-actions">
                <button type="button" class="ghost" (click)="edit(service)">Edit</button>
                <button type="button" class="danger" (click)="remove(service)" [disabled]="deletingId() === service.id">
                  {{ deletingId() === service.id ? 'Deleting...' : 'Delete' }}
                </button>
              </div>
            </article>
          } @empty {
            <p class="empty">No services found.</p>
          }
        </div>

        <div class="pager">
          <button type="button" class="ghost" (click)="previousPage()" [disabled]="page() === 1">Previous</button>
          <span>Page {{ page() }} of {{ totalPages() }}</span>
          <button type="button" class="ghost" (click)="nextPage()" [disabled]="page() === totalPages()">Next</button>
        </div>
      </section>
    </section>
  `
})
export class ServicesComponent implements OnInit {
  readonly services = signal<ServiceItem[]>([]);
  readonly editing = signal<ServiceItem | null>(null);
  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly error = signal('');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 8;
  readonly displayedServices = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.services();
    }

    return this.services().filter((service) =>
      [service.name, service.description ?? '', service.durationMinutes.toString(), service.price.toString()]
        .some((value) => value.toLowerCase().includes(term))
    );
  });
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.displayedServices().length / this.pageSize)));
  readonly pagedServices = computed(() => {
    const currentPage = Math.min(this.page(), this.totalPages());
    const start = (currentPage - 1) * this.pageSize;
    return this.displayedServices().slice(start, start + this.pageSize);
  });

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: [''],
    durationMinutes: [30, [Validators.required, Validators.min(1), Validators.max(1440)]],
    price: [0, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: ApiService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  nextPage(): void {
    this.page.set(Math.min(this.page() + 1, this.totalPages()));
  }

  previousPage(): void {
    this.page.set(Math.max(1, this.page() - 1));
  }

  load(): void {
    this.loading.set(true);
    this.api.services().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (services) => this.services.set(services),
      error: () => {
        this.error.set('Could not load services.');
        this.toastr.error('Could not load services.');
      }
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

    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.toastr.success(current ? 'Service updated.' : 'Service created.');
        this.resetForm();
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not save service.');
        this.toastr.error(err.error?.message ?? 'Could not save service.');
      }
    });
  }

  async remove(service: ServiceItem): Promise<void> {
    const result = await Swal.fire({
      title: 'Delete service?',
      text: service.name,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#b42318',
      confirmButtonText: 'Delete'
    });

    if (!result.isConfirmed) {
      return;
    }

    this.deletingId.set(service.id);
    this.error.set('');
    this.api.deleteService(service.id).pipe(finalize(() => this.deletingId.set(null))).subscribe({
      next: () => {
        this.toastr.success('Service deleted.');
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not delete service.');
        this.toastr.error(err.error?.message ?? 'Could not delete service.');
      }
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
