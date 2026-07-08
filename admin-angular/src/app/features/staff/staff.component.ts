import { Component, OnInit, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { ApiService } from '../../core/api.service';
import { CreateStaffRequest, StaffMember, UpdateStaffRequest } from '../../core/models';

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <header class="page-header">
      <div>
        <h2>Staff</h2>
        <p>Create staff accounts and maintain staff profiles.</p>
      </div>
      <button type="button" (click)="resetForm()">New staff</button>
    </header>

    <section class="split-view">
      <form class="panel" [formGroup]="form" (ngSubmit)="save()">
        <h3>{{ editing() ? 'Edit staff' : 'Create staff' }}</h3>

        <label>
          Email
          <input type="email" formControlName="email" [readOnly]="!!editing()">
        </label>

        @if (!editing()) {
          <label>
            Password
            <input type="password" formControlName="password">
          </label>
        }

        <label>
          Display name
          <input type="text" formControlName="displayName">
        </label>

        <label>
          Bio
          <textarea rows="4" formControlName="bio"></textarea>
        </label>

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
          <h3>Staff list</h3>
          <button type="button" class="ghost" (click)="load()" [disabled]="loading()">
            {{ loading() ? 'Loading...' : 'Refresh' }}
          </button>
        </div>

        @if (loading()) {
          <p class="muted">Loading staff...</p>
        }

        <label>
          Search
          <input type="search" [value]="search()" (input)="search.set($any($event.target).value)" placeholder="Search staff">
        </label>

        <div class="table">
          @for (staff of pagedStaff(); track staff.id) {
            <article class="table-row">
              <div>
                <strong>{{ staff.displayName }}</strong>
                <span>{{ staff.email }} | {{ staff.isActive ? 'Active' : 'Inactive' }}</span>
              </div>
              <div class="row-actions">
                <a class="button-link" [routerLink]="['/staff', staff.id]">Details</a>
                <button type="button" class="ghost" (click)="edit(staff)">Edit</button>
                <button type="button" class="danger" (click)="remove(staff)" [disabled]="deletingId() === staff.id">
                  {{ deletingId() === staff.id ? 'Deleting...' : 'Delete' }}
                </button>
              </div>
            </article>
          } @empty {
            <p class="empty">No staff found.</p>
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
export class StaffComponent implements OnInit {
  readonly staffMembers = signal<StaffMember[]>([]);
  readonly editing = signal<StaffMember | null>(null);
  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly error = signal('');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 8;
  readonly displayedStaff = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.staffMembers();
    }

    return this.staffMembers().filter((staff) =>
      [staff.displayName, staff.email, staff.bio ?? '']
        .some((value) => value.toLowerCase().includes(term))
    );
  });
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.displayedStaff().length / this.pageSize)));
  readonly pagedStaff = computed(() => {
    const currentPage = Math.min(this.page(), this.totalPages());
    const start = (currentPage - 1) * this.pageSize;
    return this.displayedStaff().slice(start, start + this.pageSize);
  });

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    displayName: ['', [Validators.required, Validators.maxLength(120)]],
    bio: [''],
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
    this.api.staff().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (staff) => this.staffMembers.set(staff),
      error: () => {
        this.error.set('Could not load staff.');
        this.toastr.error('Could not load staff.');
      }
    });
  }

  edit(staff: StaffMember): void {
    this.editing.set(staff);
    this.error.set('');
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
    this.form.setValue({
      email: staff.email,
      password: '',
      displayName: staff.displayName,
      bio: staff.bio ?? '',
      isActive: staff.isActive
    });
  }

  save(): void {
    if (this.form.invalid) {
      return;
    }

    const values = this.form.getRawValue();
    const current = this.editing();
    const operation: Observable<unknown> = current
      ? this.api.updateStaff(current.id, {
          displayName: values.displayName,
          bio: values.bio,
          isActive: values.isActive
        } satisfies UpdateStaffRequest)
      : this.api.createStaff({
          email: values.email,
          password: values.password,
          displayName: values.displayName,
          bio: values.bio
        } satisfies CreateStaffRequest);

    this.saving.set(true);
    this.error.set('');

    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.toastr.success(current ? 'Staff profile updated.' : 'Staff account created.');
        this.resetForm();
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not save staff.');
        this.toastr.error(err.error?.message ?? 'Could not save staff.');
      }
    });
  }

  async remove(staff: StaffMember): Promise<void> {
    const result = await Swal.fire({
      title: 'Delete staff?',
      text: staff.displayName,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#b42318',
      confirmButtonText: 'Delete'
    });

    if (!result.isConfirmed) {
      return;
    }

    this.deletingId.set(staff.id);
    this.error.set('');
    this.api.deleteStaff(staff.id).pipe(finalize(() => this.deletingId.set(null))).subscribe({
      next: () => {
        this.toastr.success('Staff deleted.');
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not delete staff.');
        this.toastr.error(err.error?.message ?? 'Could not delete staff.');
      }
    });
  }

  resetForm(): void {
    this.editing.set(null);
    this.error.set('');
    this.form.controls.password.setValidators([Validators.required, Validators.minLength(8), Validators.maxLength(128)]);
    this.form.controls.password.updateValueAndValidity();
    this.form.reset({
      email: '',
      password: '',
      displayName: '',
      bio: '',
      isActive: true
    });
  }
}
