'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { ToastContainer, toast } from 'react-toastify';
import Swal from 'sweetalert2';
import 'react-toastify/dist/ReactToastify.css';
import { api } from '@/lib/api';
import { Appointment, AuthResponse, AuthUser, AvailableSlot, AvailableStaff, BookingService } from '@/lib/types';

const tokenKey = 'slotra_customer_token';
const refreshTokenKey = 'slotra_customer_refresh_token';
const userKey = 'slotra_customer_user';

export default function CustomerHome() {
  const [token, setToken] = useState('');
  const [refreshToken, setRefreshToken] = useState('');
  const [user, setUser] = useState<AuthUser | null>(null);
  const [activeTab, setActiveTab] = useState<'book' | 'appointments'>('book');
  const [services, setServices] = useState<BookingService[]>([]);
  const [staff, setStaff] = useState<AvailableStaff[]>([]);
  const [slots, setSlots] = useState<AvailableSlot[]>([]);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [serviceSearch, setServiceSearch] = useState('');
  const [appointmentSearch, setAppointmentSearch] = useState('');
  const [appointmentStatus, setAppointmentStatus] = useState('');
  const [selectedServiceId, setSelectedServiceId] = useState('');
  const [selectedStaffId, setSelectedStaffId] = useState('');
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().slice(0, 10));
  const [selectedSlot, setSelectedSlot] = useState<AvailableSlot | null>(null);
  const [rescheduling, setRescheduling] = useState<Appointment | null>(null);
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [loadingStaff, setLoadingStaff] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [loadingAppointments, setLoadingAppointments] = useState(false);
  const [workingAppointmentId, setWorkingAppointmentId] = useState('');
  const [busy, setBusy] = useState(false);

  const selectedService = useMemo(
    () => services.find((service) => service.id === selectedServiceId),
    [services, selectedServiceId]
  );

  const filteredServices = useMemo(() => {
    const term = serviceSearch.trim().toLowerCase();
    if (!term) {
      return services;
    }

    return services.filter((service) =>
      [service.name, service.description ?? '', service.durationMinutes.toString(), service.price.toString()]
        .some((value) => value.toLowerCase().includes(term))
    );
  }, [services, serviceSearch]);

  const canSubmitBooking = Boolean(token && selectedServiceId && selectedStaffId && selectedSlot && !busy);
  const filteredAppointments = useMemo(() => {
    const term = appointmentSearch.trim().toLowerCase();

    return appointments.filter((appointment) => {
      const matchesStatus = !appointmentStatus || appointment.status === appointmentStatus;
      const matchesSearch = !term || [
        appointment.serviceName,
        appointment.staffDisplayName,
        appointment.status,
        appointment.startsAt,
        appointment.endsAt
      ].some((value) => value.toLowerCase().includes(term));

      return matchesStatus && matchesSearch;
    });
  }, [appointments, appointmentSearch, appointmentStatus]);

  useEffect(() => {
    const storedToken = localStorage.getItem(tokenKey) ?? '';
    const storedRefreshToken = localStorage.getItem(refreshTokenKey) ?? '';
    const storedUser = localStorage.getItem(userKey);
    const parsedUser = storedUser ? JSON.parse(storedUser) as AuthUser : null;

    setToken(storedToken);
    setRefreshToken(storedRefreshToken);
    setUser(parsedUser);
    loadServices();

    if (storedToken && parsedUser && !parsedUser.roles.includes('Customer')) {
      signOut('Customer portal is only for customer accounts.');
    } else if (storedToken) {
      loadAppointments(storedToken);
    }
  }, []);

  useEffect(() => {
    if (selectedServiceId && selectedStaffId && selectedDate && token) {
      loadSlots(selectedStaffId);
    }
  }, [selectedDate]);

  async function loadServices() {
    try {
      setServices(await api.services());
    } catch (err) {
      toast.error(errorMessage(err, 'Could not load services.'));
    }
  }

  async function loadStaff(serviceId: string) {
    setSelectedServiceId(serviceId);
    setSelectedStaffId('');
    setSelectedSlot(null);
    setSlots([]);

    if (!token) {
      toast.info('Sign in before choosing staff.');
      return;
    }

    setLoadingStaff(true);
    try {
      const items = await withAuth((authToken) => api.staff(serviceId, authToken));
      setStaff(items);
    } catch (err) {
      toast.error(errorMessage(err, 'Could not load staff.'));
    } finally {
      setLoadingStaff(false);
    }
  }

  async function loadSlots(staffId = selectedStaffId) {
    if (!token || !selectedServiceId || !selectedDate || !staffId) {
      return;
    }

    setSelectedStaffId(staffId);
    setSelectedSlot(null);
    setLoadingSlots(true);

    try {
      const items = await withAuth((authToken) => api.slots(selectedServiceId, selectedDate, staffId, authToken));
      setSlots(items);
    } catch (err) {
      toast.error(errorMessage(err, 'Could not load slots.'));
    } finally {
      setLoadingSlots(false);
    }
  }

  async function submitAuth(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const email = String(form.get('email') ?? '');
    const password = String(form.get('password') ?? '');
    const displayName = String(form.get('displayName') ?? '');

    setBusy(true);

    try {
      if (mode === 'register') {
        await api.register({ email, password, confirmPassword: password, displayName });
      }

      const auth = await api.login({ email, password });
      if (!auth.user.roles.includes('Customer')) {
        toast.error('Customer portal is only for customer accounts.');
        return;
      }

      applyAuth(auth);
      toast.success('Signed in successfully.');
      loadAppointments(auth.token);
    } catch (err) {
      toast.error(errorMessage(err, 'Authentication failed.'));
    } finally {
      setBusy(false);
    }
  }

  async function bookAppointment() {
    if (!selectedServiceId || !selectedSlot) {
      return;
    }

    setBusy(true);

    try {
      if (rescheduling) {
        await withAuth((authToken) => api.rescheduleAppointment(rescheduling.id, {
          staffProfileId: selectedSlot.staffProfileId,
          startsAt: selectedSlot.startsAt
        }, authToken));
        toast.success('Appointment rescheduled.');
        setRescheduling(null);
      } else {
        await withAuth((authToken) => api.book({
          serviceId: selectedServiceId,
          staffProfileId: selectedSlot.staffProfileId,
          startsAt: selectedSlot.startsAt
        }, authToken));
        toast.success('Appointment booked.');
      }

      setActiveTab('appointments');
      setSelectedSlot(null);
      setSlots([]);
      await loadAppointments();
    } catch (err) {
      toast.error(errorMessage(err, 'Could not save appointment.'));
    } finally {
      setBusy(false);
    }
  }

  async function loadAppointments(authToken = token) {
    if (!authToken) {
      return;
    }

    setLoadingAppointments(true);
    try {
      const items = await withAuth((validToken) => api.myAppointments(validToken), authToken);
      setAppointments(items);
    } catch (err) {
      toast.error(errorMessage(err, 'Could not load appointments.'));
    } finally {
      setLoadingAppointments(false);
    }
  }

  async function cancel(id: string) {
    const result = await Swal.fire({
      title: 'Cancel appointment?',
      text: 'This appointment will be marked as cancelled.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#b42318',
      confirmButtonText: 'Cancel appointment'
    });

    if (!result.isConfirmed) {
      return;
    }

    setWorkingAppointmentId(id);
    try {
      await withAuth((authToken) => api.cancelAppointment(id, 'Cancelled by customer.', authToken));
      toast.success('Appointment cancelled.');
      await loadAppointments();
    } catch (err) {
      toast.error(errorMessage(err, 'Could not cancel appointment.'));
    } finally {
      setWorkingAppointmentId('');
    }
  }

  async function startReschedule(appointment: Appointment) {
    setWorkingAppointmentId(appointment.id);
    const service = services.find((item) => item.id === appointment.serviceId);
    setActiveTab('book');
    setRescheduling(appointment);
    setSelectedServiceId(appointment.serviceId);
    setSelectedStaffId(appointment.staffProfileId);
    setSelectedDate(appointment.startsAt.slice(0, 10));
    setSelectedSlot(null);
    toast.info(`Choose a new time for ${service?.name ?? appointment.serviceName}.`);

    try {
      setLoadingStaff(true);
      const staffItems = await withAuth((authToken) => api.staff(appointment.serviceId, authToken));
      setStaff(staffItems);
      await loadSlots(appointment.staffProfileId);
    } catch (err) {
      toast.error(errorMessage(err, 'Could not prepare reschedule.'));
    } finally {
      setLoadingStaff(false);
      setWorkingAppointmentId('');
    }
  }

  async function withAuth<T>(operation: (authToken: string) => Promise<T>, tokenOverride?: string): Promise<T> {
    const activeToken = tokenOverride || token;

    try {
      return await operation(activeToken);
    } catch (err) {
      if (!refreshToken) {
        throw err;
      }

      try {
        const auth = await api.refresh(refreshToken);
        if (!auth.user.roles.includes('Customer')) {
          signOut('Customer portal is only for customer accounts.');
          throw new Error('Invalid account role.');
        }

        applyAuth(auth);
        return operation(auth.token);
      } catch (refreshError) {
        signOut('Session expired. Please sign in again.');
        throw refreshError;
      }
    }
  }

  function applyAuth(auth: AuthResponse) {
    localStorage.setItem(tokenKey, auth.token);
    localStorage.setItem(refreshTokenKey, auth.refreshToken);
    localStorage.setItem(userKey, JSON.stringify(auth.user));
    setToken(auth.token);
    setRefreshToken(auth.refreshToken);
    setUser(auth.user);
  }

  function signOut(message = 'Signed out.') {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(refreshTokenKey);
    localStorage.removeItem(userKey);
    setToken('');
    setRefreshToken('');
    setUser(null);
    setAppointments([]);
    setStaff([]);
    setSlots([]);
    setRescheduling(null);
    toast.info(message);
  }

  return (
    <main>
      <ToastContainer position="top-right" newestOnTop />

      <header className="topbar">
        <div>
          <h1>Slotra</h1>
          <p>Book appointments with the right service and staff member.</p>
        </div>
        {user ? (
          <div className="userbox">
            <span>
              <strong>{user.displayName}</strong>
              <small>{user.email}</small>
            </span>
            <button type="button" className="ghost" onClick={() => signOut()}>Logout</button>
          </div>
        ) : null}
      </header>

      {!token ? (
        <section className="auth-panel">
          <div>
            <h2>{mode === 'login' ? 'Welcome back' : 'Create account'}</h2>
            <p>{mode === 'login' ? 'Sign in to book and manage appointments.' : 'Register as a customer to start booking.'}</p>
          </div>
          <form onSubmit={submitAuth}>
            {mode === 'register' ? (
              <label>
                Name
                <input name="displayName" defaultValue="Slotra Customer" required />
              </label>
            ) : null}
            <label>
              Email
              <input name="email" type="email" defaultValue="customer@slotra.local" required />
            </label>
            <label>
              Password
              <input name="password" type="password" defaultValue="Customer123!" required />
            </label>
            <button type="submit" disabled={busy}>{busy ? 'Please wait...' : mode === 'login' ? 'Sign in' : 'Register'}</button>
            <button type="button" className="ghost" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>
              {mode === 'login' ? 'Create an account' : 'Use existing account'}
            </button>
          </form>
        </section>
      ) : (
        <>
          <nav className="tabs">
            <button type="button" className={activeTab === 'book' ? 'active' : ''} onClick={() => setActiveTab('book')}>Book</button>
            <button type="button" className={activeTab === 'appointments' ? 'active' : ''} onClick={() => setActiveTab('appointments')}>My appointments</button>
          </nav>

          {activeTab === 'book' ? (
            <section className="workspace">
              <section className="panel">
                <h2>{rescheduling ? 'Choose new service time' : 'Choose a service'}</h2>
                <label>
                  Search services
                  <input value={serviceSearch} onChange={(event) => setServiceSearch(event.target.value)} placeholder="Search services" />
                </label>
                <div className="service-grid">
                  {filteredServices.map((service) => (
                    <button
                      type="button"
                      className={selectedServiceId === service.id ? 'service-card selected' : 'service-card'}
                      key={service.id}
                      onClick={() => loadStaff(service.id)}
                    >
                      <strong>{service.name}</strong>
                      <span>{service.durationMinutes} min | ${service.price.toFixed(2)}</span>
                      <small>{service.description}</small>
                    </button>
                  ))}
                  {filteredServices.length === 0 ? <p className="muted">No services match your search.</p> : null}
                </div>
              </section>

              <section className="panel">
                <h2>Pick staff and time</h2>
                <div className="field-row">
                  <label>
                    Date
                    <input type="date" value={selectedDate} onChange={(event) => setSelectedDate(event.target.value)} />
                  </label>
                  <label>
                    Staff
                    <select value={selectedStaffId} onChange={(event) => loadSlots(event.target.value)} disabled={!selectedServiceId || loadingStaff}>
                      <option value="">{loadingStaff ? 'Loading staff...' : 'Select staff'}</option>
                      {staff.map((item) => (
                        <option key={item.staffProfileId} value={item.staffProfileId}>{item.displayName}</option>
                      ))}
                    </select>
                  </label>
                </div>

                {selectedServiceId && !loadingStaff && staff.length === 0 ? <p className="muted">No staff are available for this service.</p> : null}
                {loadingSlots ? <p className="muted">Loading slots...</p> : null}

                <div className="slot-grid">
                  {slots.map((slot) => (
                    <button
                      type="button"
                      className={selectedSlot?.startsAt === slot.startsAt ? 'slot selected' : 'slot'}
                      key={slot.startsAt}
                      onClick={() => setSelectedSlot(slot)}
                    >
                      {new Date(slot.startsAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </button>
                  ))}
                  {selectedService && selectedStaffId && !loadingSlots && slots.length === 0 ? <p className="muted">No slots available for this date.</p> : null}
                </div>

                <button type="button" disabled={!canSubmitBooking} onClick={bookAppointment}>
                  {busy ? 'Saving...' : rescheduling ? 'Reschedule appointment' : 'Book appointment'}
                </button>
                {rescheduling ? <button type="button" className="ghost" onClick={() => setRescheduling(null)}>Cancel reschedule</button> : null}
              </section>
            </section>
          ) : (
            <section className="panel appointments">
              <div className="panel-heading">
                <h2>My appointments</h2>
                <button type="button" className="ghost" onClick={() => loadAppointments()} disabled={loadingAppointments}>
                  {loadingAppointments ? 'Loading...' : 'Refresh'}
                </button>
              </div>
              <div className="filters">
                <label>
                  Search
                  <input value={appointmentSearch} onChange={(event) => setAppointmentSearch(event.target.value)} placeholder="Search appointments" />
                </label>
                <label>
                  Status
                  <select value={appointmentStatus} onChange={(event) => setAppointmentStatus(event.target.value)}>
                    <option value="">All statuses</option>
                    <option value="Confirmed">Confirmed</option>
                    <option value="Completed">Completed</option>
                    <option value="Cancelled">Cancelled</option>
                    <option value="NoShow">No show</option>
                  </select>
                </label>
              </div>
              <div className="list">
                {filteredAppointments.map((appointment) => (
                  <article key={appointment.id} className="appointment-row">
                    <div>
                      <strong>{appointment.serviceName}</strong>
                      <span>{appointment.staffDisplayName} | {new Date(appointment.startsAt).toLocaleString()}</span>
                      <small className={`status ${appointment.status.toLowerCase()}`}>{appointment.status}</small>
                    </div>
                    {appointment.status !== 'Cancelled' ? (
                      <div className="row-actions">
                        <button
                          type="button"
                          className="ghost"
                          onClick={() => startReschedule(appointment)}
                          disabled={workingAppointmentId === appointment.id}
                        >
                          Reschedule
                        </button>
                        <button
                          type="button"
                          className="danger"
                          onClick={() => cancel(appointment.id)}
                          disabled={workingAppointmentId === appointment.id}
                        >
                          {workingAppointmentId === appointment.id ? 'Working...' : 'Cancel'}
                        </button>
                      </div>
                    ) : null}
                  </article>
                ))}
                {appointments.length === 0 ? <p className="muted">No appointments yet.</p> : null}
                {appointments.length > 0 && filteredAppointments.length === 0 ? <p className="muted">No appointments match your filters.</p> : null}
              </div>
            </section>
          )}
        </>
      )}
    </main>
  );
}

function errorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback;
}
