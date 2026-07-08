export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
}

export interface Appointment {
  id: string;
  customerId: string;
  staffProfileId: string;
  staffDisplayName: string;
  serviceId: string;
  serviceName: string;
  startsAt: string;
  endsAt: string;
  status: string;
}

export interface AppointmentNote {
  id: string;
  appointmentId: string;
  authorId: string;
  authorDisplayName: string;
  body: string;
  createdAt: string;
}
