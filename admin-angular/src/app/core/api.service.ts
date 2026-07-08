import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { apiBaseUrl } from './api.config';
import { DashboardSummary, PagedResponse, ServiceItem, ServiceUpsertRequest } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}

  dashboardSummary() {
    return this.http.get<DashboardSummary>(`${apiBaseUrl}/admin/dashboard/summary`);
  }

  services(page = 1, pageSize = 50) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<ServiceItem>>(`${apiBaseUrl}/services`, { params });
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
}
