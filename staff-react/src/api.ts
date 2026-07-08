import { Appointment, AppointmentNote, AuthResponse } from './types';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:44301/api/v1';

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
  login(input: { email: string; password: string }) {
    return request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(input) });
  },
  refresh(refreshToken: string) {
    return request<AuthResponse>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) });
  },
  today(token: string) {
    return request<Appointment[]>('/staff/me/appointments/today', {}, token);
  },
  appointments(token: string) {
    return request<{ items: Appointment[] }>('/appointments?page=1&pageSize=100', {}, token);
  },
  updateStatus(id: string, status: string, token: string) {
    return request<Appointment>(`/appointments/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }, token);
  },
  notes(id: string, token: string) {
    return request<AppointmentNote[]>(`/appointments/${id}/notes`, {}, token);
  },
  addNote(id: string, body: string, token: string) {
    return request<AppointmentNote>(`/appointments/${id}/notes`, { method: 'POST', body: JSON.stringify({ body }) }, token);
  }
};
