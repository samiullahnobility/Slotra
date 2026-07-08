export interface LoginRequest {
  email: string;
  password: string;
}

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

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface DashboardSummary {
  totalAppointments: number;
  todayAppointments: number;
  completedAppointments: number;
  cancelledAppointments: number;
  estimatedRevenue: number;
}

export interface ServiceItem {
  id: string;
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export interface ServiceUpsertRequest {
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export interface StaffMember {
  id: string;
  userId: string;
  email: string;
  displayName: string;
  bio?: string | null;
  isActive: boolean;
}

export interface CreateStaffRequest {
  email: string;
  password: string;
  displayName: string;
  bio?: string | null;
}

export interface UpdateStaffRequest {
  displayName: string;
  bio?: string | null;
  isActive: boolean;
}

export interface StaffServiceAssignment {
  serviceId: string;
  name: string;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export interface StaffAvailability {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

export interface StaffAvailabilityRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
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
