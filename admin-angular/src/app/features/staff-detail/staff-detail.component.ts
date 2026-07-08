import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin, Observable } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { ApiService } from '../../core/api.service';
import {
  ServiceItem,
  StaffAvailability,
  StaffAvailabilityRequest,
  StaffMember,
  StaffServiceAssignment
} from '../../core/models';

@Component({
  selector: 'app-staff-detail',
  standalone: true,
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink],
  template: `
    <header class="page-header">
      <div>
        <a routerLink="/staff" class="back-link">Back to staff</a>
        <h2>{{ staff()?.displayName ?? 'Staff details' }}</h2>
        <p>{{ staff()?.email }}</p>
      </div>
      <button type="button" (click)="load()" [disabled]="loading()">
        {{ loading() ? 'Loading...' : 'Refresh' }}
      </button>
    </header>

    @if (loading() && !staff()) {
      <div class="panel muted">Loading staff details...</div>
    }

    <section class="detail-grid">
      <section class="panel">
        <h3>Assigned services</h3>

        <form class="inline-form" [formGroup]="serviceForm" (ngSubmit)="assignService()">
          <label>
            Service
            <select formControlName="serviceId">
              <option value="">Select service</option>
              @for (service of assignableServices(); track service.id) {
                <option [value]="service.id">{{ service.name }}</option>
              }
            </select>
          </label>
          <button type="submit" [disabled]="serviceForm.invalid || busy()">{{ busy() ? 'Working...' : 'Assign' }}</button>
        </form>

        <div class="table">
          @for (service of assignedServices(); track service.serviceId) {
            <article class="table-row">
              <div>
                <strong>{{ service.name }}</strong>
                <span>{{ service.durationMinutes }} min | {{ service.price | currency }} | {{ service.isActive ? 'Active' : 'Inactive' }}</span>
              </div>
              <button type="button" class="danger" (click)="removeService(service)" [disabled]="busy()">Remove</button>
            </article>
          } @empty {
            <p class="empty">No services assigned.</p>
          }
        </div>
      </section>

      <section class="panel">
        <h3>{{ editingAvailability() ? 'Edit availability' : 'Add availability' }}</h3>

        <form [formGroup]="availabilityForm" (ngSubmit)="saveAvailability()">
          <div class="field-row">
            <label>
              Day
              <select formControlName="dayOfWeek">
                @for (day of days; track day.value) {
                  <option [value]="day.value">{{ day.label }}</option>
                }
              </select>
            </label>

            <label>
              Active
              <select formControlName="isActive">
                <option [ngValue]="true">Active</option>
                <option [ngValue]="false">Inactive</option>
              </select>
            </label>
          </div>

          <div class="field-row">
            <label>
              Start
              <input type="time" formControlName="startTime">
            </label>

            <label>
              End
              <input type="time" formControlName="endTime">
            </label>
          </div>

          <div class="actions">
            <button type="submit" [disabled]="availabilityForm.invalid || busy()">{{ busy() ? 'Saving...' : 'Save' }}</button>
            <button type="button" class="ghost" (click)="resetAvailabilityForm()">Clear</button>
          </div>
        </form>

        <div class="table">
          @for (slot of availability(); track slot.id) {
            <article class="table-row">
              <div>
                <strong>{{ dayName(slot.dayOfWeek) }}</strong>
                <span>{{ slot.startTime }} - {{ slot.endTime }} | {{ slot.isActive ? 'Active' : 'Inactive' }}</span>
              </div>
              <div class="row-actions">
                <button type="button" class="ghost" (click)="editAvailability(slot)">Edit</button>
                <button type="button" class="danger" (click)="deleteAvailability(slot)" [disabled]="busy()">Delete</button>
              </div>
            </article>
          } @empty {
            <p class="empty">No availability set.</p>
          }
        </div>
      </section>
    </section>
  `
})
export class StaffDetailComponent implements OnInit {
  readonly staff = signal<StaffMember | null>(null);
  readonly allServices = signal<ServiceItem[]>([]);
  readonly assignedServices = signal<StaffServiceAssignment[]>([]);
  readonly availability = signal<StaffAvailability[]>([]);
  readonly editingAvailability = signal<StaffAvailability | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');

  readonly days = [
    { value: 0, label: 'Sunday' },
    { value: 1, label: 'Monday' },
    { value: 2, label: 'Tuesday' },
    { value: 3, label: 'Wednesday' },
    { value: 4, label: 'Thursday' },
    { value: 5, label: 'Friday' },
    { value: 6, label: 'Saturday' }
  ];

  readonly serviceForm = this.fb.nonNullable.group({
    serviceId: ['', Validators.required]
  });

  readonly availabilityForm = this.fb.nonNullable.group({
    dayOfWeek: [1, Validators.required],
    startTime: ['09:00', Validators.required],
    endTime: ['17:00', Validators.required],
    isActive: [true, Validators.required]
  });

  private readonly staffId = this.route.snapshot.paramMap.get('id') ?? '';

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  assignableServices(): ServiceItem[] {
    const assignedIds = new Set(this.assignedServices().map((service) => service.serviceId));
    return this.allServices().filter((service) => service.isActive && !assignedIds.has(service.id));
  }

  load(): void {
    this.error.set('');
    this.loading.set(true);
    forkJoin({
      staff: this.api.staffById(this.staffId),
      services: this.api.services(),
      assigned: this.api.staffServices(this.staffId),
      availability: this.api.staffAvailability(this.staffId)
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (result) => {
        this.staff.set(result.staff);
        this.allServices.set(result.services);
        this.assignedServices.set(result.assigned);
        this.availability.set(result.availability);
      },
      error: () => {
        this.error.set('Could not load staff details.');
        this.toastr.error('Could not load staff details.');
      }
    });
  }

  assignService(): void {
    const serviceId = this.serviceForm.controls.serviceId.value;
    this.handle(this.api.assignStaffService(this.staffId, serviceId), 'Could not assign service.', () => {
      this.toastr.success('Service assigned.');
      this.serviceForm.reset({ serviceId: '' });
      this.load();
    });
  }

  async removeService(service: StaffServiceAssignment): Promise<void> {
    const result = await Swal.fire({
      title: 'Remove service?',
      text: service.name,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#b42318',
      confirmButtonText: 'Remove'
    });

    if (!result.isConfirmed) {
      return;
    }

    this.handle(this.api.removeStaffService(this.staffId, service.serviceId), 'Could not remove service.', () => {
      this.toastr.success('Service removed.');
      this.load();
    });
  }

  saveAvailability(): void {
    if (this.availabilityForm.invalid) {
      return;
    }

    const request = this.availabilityForm.getRawValue() as StaffAvailabilityRequest;
    const editing = this.editingAvailability();
    const operation = editing
      ? this.api.updateStaffAvailability(this.staffId, editing.id, request)
      : this.api.addStaffAvailability(this.staffId, request);

    this.handle(operation, 'Could not save availability.', () => {
      this.toastr.success(editing ? 'Availability updated.' : 'Availability added.');
      this.resetAvailabilityForm();
      this.load();
    });
  }

  editAvailability(slot: StaffAvailability): void {
    this.editingAvailability.set(slot);
    this.availabilityForm.setValue({
      dayOfWeek: slot.dayOfWeek,
      startTime: slot.startTime.slice(0, 5),
      endTime: slot.endTime.slice(0, 5),
      isActive: slot.isActive
    });
  }

  async deleteAvailability(slot: StaffAvailability): Promise<void> {
    const result = await Swal.fire({
      title: 'Delete availability?',
      text: `${this.dayName(slot.dayOfWeek)} ${slot.startTime} - ${slot.endTime}`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#b42318',
      confirmButtonText: 'Delete'
    });

    if (!result.isConfirmed) {
      return;
    }

    this.handle(this.api.deleteStaffAvailability(this.staffId, slot.id), 'Could not delete availability.', () => {
      this.toastr.success('Availability deleted.');
      this.load();
    });
  }

  resetAvailabilityForm(): void {
    this.editingAvailability.set(null);
    this.availabilityForm.reset({
      dayOfWeek: 1,
      startTime: '09:00',
      endTime: '17:00',
      isActive: true
    });
  }

  dayName(value: number): string {
    return this.days.find((day) => day.value === value)?.label ?? 'Unknown';
  }

  private handle(operation: Observable<unknown>, fallback: string, onSuccess: () => void): void {
    this.error.set('');
    this.busy.set(true);
    operation.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: onSuccess,
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? fallback);
        this.toastr.error(err.error?.message ?? fallback);
      }
    });
  }
}
