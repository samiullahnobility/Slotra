import {
  Appointment,
  AuthResponse,
  AvailableSlot,
  AvailableStaff,
  BookingService
} from './types';

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'https://localhost:44301/api/v1';

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers
    }
  });

  if (!response.ok) {
    let message = 'Request failed.';
    try {
      const error = await response.json();
      message = error.message ?? message;
    } catch {
      message = response.statusText || message;
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  register(input: { email: string; password: string; confirmPassword: string; displayName: string }) {
    return request('/auth/register', { method: 'POST', body: JSON.stringify(input) });
  },
  login(input: { email: string; password: string }) {
    return request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(input) });
  },
  refresh(refreshToken: string) {
    return request<AuthResponse>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) });
  },
  services() {
    return request<BookingService[]>('/booking/services');
  },
  staff(serviceId: string, token: string) {
    return request<AvailableStaff[]>(`/booking/staff?serviceId=${serviceId}`, {}, token);
  },
  slots(serviceId: string, date: string, staffId: string, token: string) {
    const query = new URLSearchParams({ serviceId, date, staffId });
    return request<AvailableSlot[]>(`/booking/available-slots?${query.toString()}`, {}, token);
  },
  book(input: { serviceId: string; staffProfileId: string; startsAt: string }, token: string) {
    return request<Appointment>('/appointments', { method: 'POST', body: JSON.stringify(input) }, token);
  },
  myAppointments(token: string) {
    return request<Appointment[]>('/appointments/my', {}, token);
  },
  cancelAppointment(id: string, reason: string, token: string) {
    return request<void>(`/appointments/${id}/cancel`, { method: 'PUT', body: JSON.stringify({ reason }) }, token);
  },
  rescheduleAppointment(id: string, input: { staffProfileId: string; startsAt: string }, token: string) {
    return request<Appointment>(`/appointments/${id}/reschedule`, { method: 'PUT', body: JSON.stringify(input) }, token);
  }
};
