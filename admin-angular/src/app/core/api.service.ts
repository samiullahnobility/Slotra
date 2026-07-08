import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map } from 'rxjs';
import { apiBaseUrl } from './api.config';
import {
  Appointment,
  AppointmentNote,
  CreateStaffRequest,
  DashboardSummary,
  PagedResponse,
  ServiceItem,
  ServiceUpsertRequest,
  StaffAvailability,
  StaffAvailabilityRequest,
  StaffMember,
  StaffServiceAssignment,
  UpdateStaffRequest
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}

  dashboardSummary() {
    return this.http.get<DashboardSummary>(`${apiBaseUrl}/admin/dashboard/summary`);
  }

  services(page = 1, pageSize = 50) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http
      .get<PagedResponse<ServiceItem> | ServiceItem[]>(`${apiBaseUrl}/services`, { params })
      .pipe(map((response) => Array.isArray(response) ? response : response.items));
  }

  createService(request: ServiceUpsertRequest) {
    return this.http.post<ServiceItem>(`${apiBaseUrl}/services`, request);
  }

  updateService(id: string, request: ServiceUpsertRequest) {
    return this.http.put<ServiceItem>(`${apiBaseUrl}/services/${id}`, request);
  }

  deleteService(id: string) {
    return this.http.delete<void>(`${apiBaseUrl}/services/${id}`);
  }

  staff() {
    return this.http.get<StaffMember[]>(`${apiBaseUrl}/staff`);
  }

  createStaff(request: CreateStaffRequest) {
    return this.http.post<StaffMember>(`${apiBaseUrl}/staff`, request);
  }

  updateStaff(id: string, request: UpdateStaffRequest) {
    return this.http.put<void>(`${apiBaseUrl}/staff/${id}`, request);
  }

  deleteStaff(id: string) {
    return this.http.delete<void>(`${apiBaseUrl}/staff/${id}`);
  }

  staffById(id: string) {
    return this.http.get<StaffMember>(`${apiBaseUrl}/staff/${id}`);
  }

  staffServices(id: string) {
    return this.http.get<StaffServiceAssignment[]>(`${apiBaseUrl}/staff/${id}/services`);
  }

  assignStaffService(id: string, serviceId: string) {
    return this.http.post<void>(`${apiBaseUrl}/staff/${id}/services`, { serviceId });
  }

  removeStaffService(id: string, serviceId: string) {
    return this.http.delete<void>(`${apiBaseUrl}/staff/${id}/services/${serviceId}`);
  }

  staffAvailability(id: string) {
    return this.http.get<StaffAvailability[]>(`${apiBaseUrl}/staff/${id}/availability`);
  }

  addStaffAvailability(id: string, request: StaffAvailabilityRequest) {
    return this.http.post<StaffAvailability>(`${apiBaseUrl}/staff/${id}/availability`, request);
  }

  updateStaffAvailability(id: string, availabilityId: string, request: StaffAvailabilityRequest) {
    return this.http.put<void>(`${apiBaseUrl}/staff/${id}/availability/${availabilityId}`, request);
  }

  deleteStaffAvailability(id: string, availabilityId: string) {
    return this.http.delete<void>(`${apiBaseUrl}/staff/${id}/availability/${availabilityId}`);
  }

  appointments(filters: {
    status?: string;
    fromDate?: string;
    toDate?: string;
    staffId?: string;
    serviceId?: string;
    page?: number;
    pageSize?: number;
  }) {
    let params = new HttpParams()
      .set('page', filters.page ?? 1)
      .set('pageSize', filters.pageSize ?? 50);

    for (const key of ['status', 'fromDate', 'toDate', 'staffId', 'serviceId'] as const) {
      const value = filters[key];
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<PagedResponse<Appointment>>(`${apiBaseUrl}/appointments`, { params });
  }

  updateAppointmentStatus(id: string, status: string) {
    return this.http.put<Appointment>(`${apiBaseUrl}/appointments/${id}/status`, { status });
  }

  appointmentNotes(id: string) {
    return this.http.get<AppointmentNote[]>(`${apiBaseUrl}/appointments/${id}/notes`);
  }

  addAppointmentNote(id: string, body: string) {
    return this.http.post<AppointmentNote>(`${apiBaseUrl}/appointments/${id}/notes`, { body });
  }
}
