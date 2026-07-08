import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { ApiService } from '../../core/api.service';
import { Appointment, AppointmentNote, ServiceItem, StaffMember } from '../../core/models';

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <header class="page-header">
      <div>
        <h2>Appointments</h2>
        <p>Review bookings, update status, and keep internal notes.</p>
      </div>
      <button type="button" (click)="load()" [disabled]="loading()">
        {{ loading() ? 'Loading...' : 'Refresh' }}
      </button>
    </header>

    <section class="panel">
      <form class="filter-grid" [formGroup]="filters" (ngSubmit)="load()">
        <label>
          Status
          <select formControlName="status">
            <option value="">All</option>
            @for (status of statuses; track status) {
              <option [value]="status">{{ status }}</option>
            }
          </select>
        </label>

        <label>
          From
          <input type="date" formControlName="fromDate">
        </label>

        <label>
          To
          <input type="date" formControlName="toDate">
        </label>

        <label>
          Staff
          <select formControlName="staffId">
            <option value="">All staff</option>
            @for (staff of staffMembers(); track staff.id) {
              <option [value]="staff.id">{{ staff.displayName }}</option>
            }
          </select>
        </label>

        <label>
          Service
          <select formControlName="serviceId">
            <option value="">All services</option>
            @for (service of services(); track service.id) {
              <option [value]="service.id">{{ service.name }}</option>
            }
          </select>
        </label>

        <div class="actions end">
          <button type="submit" [disabled]="loading()">Apply</button>
          <button type="button" class="ghost" (click)="clearFilters()">Clear</button>
        </div>
      </form>
    </section>

    <section class="detail-grid">
      <section class="panel">
        <div class="table-header">
          <h3>Appointment list</h3>
          <span class="muted">{{ appointments().length }} shown</span>
        </div>

        @if (loading()) {
          <p class="muted">Loading appointments...</p>
        }

        <label>
          Search
          <input type="search" [value]="search()" (input)="search.set($any($event.target).value)" placeholder="Search appointments">
        </label>

        <div class="table">
          @for (appointment of pagedAppointments(); track appointment.id) {
            <article class="table-row clickable" [class.selected]="selected()?.id === appointment.id" (click)="select(appointment)">
              <div>
                <strong>{{ appointment.serviceName }}</strong>
                <span>{{ appointment.staffDisplayName }} | {{ appointment.startsAt | date:'medium' }}</span>
              </div>
              <span class="status-pill">{{ appointment.status }}</span>
            </article>
          } @empty {
            <p class="empty">No appointments found.</p>
          }
        </div>

        <div class="pager">
          <button type="button" class="ghost" (click)="previousPage()" [disabled]="page() === 1">Previous</button>
          <span>Page {{ page() }} of {{ totalPages() }}</span>
          <button type="button" class="ghost" (click)="nextPage()" [disabled]="page() === totalPages()">Next</button>
        </div>
      </section>

      <section class="panel">
        <h3>Details</h3>

        @if (selected(); as appointment) {
          <div class="summary-list">
            <p><strong>Service</strong><span>{{ appointment.serviceName }}</span></p>
            <p><strong>Staff</strong><span>{{ appointment.staffDisplayName }}</span></p>
            <p><strong>Starts</strong><span>{{ appointment.startsAt | date:'medium' }}</span></p>
            <p><strong>Ends</strong><span>{{ appointment.endsAt | date:'medium' }}</span></p>
            <p><strong>Status</strong><span>{{ appointment.status }}</span></p>
          </div>

          <form class="inline-form" [formGroup]="statusForm" (ngSubmit)="updateStatus()">
            <label>
              Update status
              <select formControlName="status">
                @for (status of statuses; track status) {
                  <option [value]="status">{{ status }}</option>
                }
              </select>
            </label>
            <button type="submit" [disabled]="updatingStatus()">
              {{ updatingStatus() ? 'Updating...' : 'Update' }}
            </button>
          </form>

          <form class="note-form" [formGroup]="noteForm" (ngSubmit)="addNote()">
            <label>
              Add note
              <textarea rows="3" formControlName="body"></textarea>
            </label>
            <button type="submit" [disabled]="addingNote() || !noteForm.controls.body.value.trim()">
              {{ addingNote() ? 'Adding...' : 'Add note' }}
            </button>
          </form>

          <div class="table">
            @for (note of notes(); track note.id) {
              <article class="note">
                <strong>{{ note.authorDisplayName }}</strong>
                <span>{{ note.createdAt | date:'short' }}</span>
                <p>{{ note.body }}</p>
              </article>
            } @empty {
              <p class="empty">No notes yet.</p>
            }
          </div>
        } @else {
          <p class="empty">Select an appointment to manage it.</p>
        }
      </section>
    </section>
  `
})
export class AppointmentsComponent implements OnInit {
  readonly statuses = ['Confirmed', 'Completed', 'Cancelled', 'NoShow'];
  readonly appointments = signal<Appointment[]>([]);
  readonly services = signal<ServiceItem[]>([]);
  readonly staffMembers = signal<StaffMember[]>([]);
  readonly selected = signal<Appointment | null>(null);
  readonly notes = signal<AppointmentNote[]>([]);
  readonly loading = signal(false);
  readonly updatingStatus = signal(false);
  readonly addingNote = signal(false);
  readonly error = signal('');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 8;
  readonly displayedAppointments = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.appointments();
    }

    return this.appointments().filter((appointment) =>
      [
        appointment.serviceName,
        appointment.staffDisplayName,
        appointment.status,
        appointment.startsAt,
        appointment.endsAt
      ].some((value) => value.toLowerCase().includes(term))
    );
  });
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.displayedAppointments().length / this.pageSize)));
  readonly pagedAppointments = computed(() => {
    const currentPage = Math.min(this.page(), this.totalPages());
    const start = (currentPage - 1) * this.pageSize;
    return this.displayedAppointments().slice(start, start + this.pageSize);
  });

  readonly filters = this.fb.nonNullable.group({
    status: [''],
    fromDate: [''],
    toDate: [''],
    staffId: [''],
    serviceId: ['']
  });

  readonly statusForm = this.fb.nonNullable.group({
    status: ['Confirmed']
  });

  readonly noteForm = this.fb.nonNullable.group({
    body: ['']
  });

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    forkJoin({
      services: this.api.services(),
      staff: this.api.staff()
    }).subscribe({
      next: ({ services, staff }) => {
        this.services.set(services);
        this.staffMembers.set(staff);
        this.load();
      },
      error: () => {
        this.error.set('Could not load appointment filters.');
        this.toastr.error('Could not load appointment filters.');
      }
    });
  }

  load(): void {
    this.error.set('');
    this.loading.set(true);
    this.api.appointments(this.filters.getRawValue()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (response) => {
        this.appointments.set(response.items);
        if (this.selected() && !response.items.some((item) => item.id === this.selected()?.id)) {
          this.selected.set(null);
          this.notes.set([]);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not load appointments.');
        this.toastr.error(err.error?.message ?? 'Could not load appointments.');
      }
    });
  }

  clearFilters(): void {
    this.filters.reset({
      status: '',
      fromDate: '',
      toDate: '',
      staffId: '',
      serviceId: ''
    });
    this.load();
  }

  nextPage(): void {
    this.page.set(Math.min(this.page() + 1, this.totalPages()));
  }

  previousPage(): void {
    this.page.set(Math.max(1, this.page() - 1));
  }

  select(appointment: Appointment): void {
    this.selected.set(appointment);
    this.statusForm.setValue({ status: appointment.status });
    this.noteForm.reset({ body: '' });
    this.loadNotes();
  }

  updateStatus(): void {
    const appointment = this.selected();
    if (!appointment) {
      return;
    }

    this.error.set('');
    this.updatingStatus.set(true);
    this.api.updateAppointmentStatus(appointment.id, this.statusForm.controls.status.value)
      .pipe(finalize(() => this.updatingStatus.set(false)))
      .subscribe({
      next: (updated) => {
        this.toastr.success('Appointment status updated.');
        this.selected.set(updated);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not update appointment status.');
        this.toastr.error(err.error?.message ?? 'Could not update appointment status.');
      }
    });
  }

  addNote(): void {
    const appointment = this.selected();
    const body = this.noteForm.controls.body.value.trim();
    if (!appointment || !body) {
      return;
    }

    this.error.set('');
    this.addingNote.set(true);
    this.api.addAppointmentNote(appointment.id, body).pipe(finalize(() => this.addingNote.set(false))).subscribe({
      next: () => {
        this.toastr.success('Note added.');
        this.noteForm.reset({ body: '' });
        this.loadNotes();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not add note.');
        this.toastr.error(err.error?.message ?? 'Could not add note.');
      }
    });
  }

  private loadNotes(): void {
    const appointment = this.selected();
    if (!appointment) {
      return;
    }

    this.api.appointmentNotes(appointment.id).subscribe({
      next: (notes) => this.notes.set(notes),
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not load notes.');
        this.toastr.error(err.error?.message ?? 'Could not load notes.');
      }
    });
  }
}
