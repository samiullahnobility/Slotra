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

export interface BookingService {
  id: string;
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
}

export interface AvailableStaff {
  staffProfileId: string;
  displayName: string;
  bio?: string | null;
}

export interface AvailableSlot {
  staffProfileId: string;
  staffDisplayName: string;
  startsAt: string;
  endsAt: string;
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
