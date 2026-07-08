import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ToastContainer, toast } from 'react-toastify';
import Swal from 'sweetalert2';
import 'react-toastify/dist/ReactToastify.css';
import './styles.css';
import { api } from './api';
import { Appointment, AppointmentNote, AuthResponse, AuthUser } from './types';
import { FormEvent, useEffect, useMemo, useState } from 'react';

const tokenKey = 'slotra_staff_token';
const refreshTokenKey = 'slotra_staff_refresh_token';
const userKey = 'slotra_staff_user';

function App() {
  const [token, setToken] = useState('');
  const [refreshToken, setRefreshToken] = useState('');
  const [user, setUser] = useState<AuthUser | null>(null);
  const [tab, setTab] = useState<'today' | 'all'>('today');
  const [today, setToday] = useState<Appointment[]>([]);
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [selected, setSelected] = useState<Appointment | null>(null);
  const [notes, setNotes] = useState<AppointmentNote[]>([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [noteBody, setNoteBody] = useState('');
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);

  const sourceAppointments = tab === 'today' ? today : appointments;
  const filteredAppointments = useMemo(() => {
    const term = search.trim().toLowerCase();
    return sourceAppointments.filter((appointment) => {
      const matchesStatus = !status || appointment.status === status;
      const matchesSearch = !term || [
        appointment.serviceName,
        appointment.status,
        appointment.startsAt,
        appointment.endsAt
      ].some((value) => value.toLowerCase().includes(term));
      return matchesStatus && matchesSearch;
    });
  }, [sourceAppointments, search, status]);

  useEffect(() => {
    const storedToken = localStorage.getItem(tokenKey) ?? '';
    const storedRefreshToken = localStorage.getItem(refreshTokenKey) ?? '';
    const storedUser = localStorage.getItem(userKey);
    const parsedUser = storedUser ? JSON.parse(storedUser) as AuthUser : null;

    setToken(storedToken);
    setRefreshToken(storedRefreshToken);
    setUser(parsedUser);

    if (storedToken && parsedUser && !parsedUser.roles.includes('Staff')) {
      signOut('Staff portal is only for staff accounts.');
    } else if (storedToken) {
      loadData(storedToken);
    }
  }, []);

  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const email = String(form.get('email') ?? '');
    const password = String(form.get('password') ?? '');

    setBusy(true);
    try {
      const auth = await api.login({ email, password });
      if (!auth.user.roles.includes('Staff')) {
        toast.error('Staff portal is only for staff accounts.');
        return;
      }

      applyAuth(auth);
      toast.success('Signed in.');
      await loadData(auth.token);
    } catch (err) {
      toast.error(errorMessage(err, 'Login failed.'));
    } finally {
      setBusy(false);
    }
  }

  async function loadData(authToken = token) {
    setLoading(true);
    try {
      const [todayItems, allItems] = await Promise.all([
        withAuth((validToken) => api.today(validToken), authToken),
        withAuth((validToken) => api.appointments(validToken), authToken)
      ]);
      setToday(todayItems);
      setAppointments(allItems.items);
    } catch (err) {
      toast.error(errorMessage(err, 'Could not load appointments.'));
    } finally {
      setLoading(false);
    }
  }

  async function selectAppointment(appointment: Appointment) {
    setSelected(appointment);
    setNoteBody('');
    try {
      setNotes(await withAuth((authToken) => api.notes(appointment.id, authToken)));
    } catch (err) {
      toast.error(errorMessage(err, 'Could not load notes.'));
    }
  }

  async function updateStatus(nextStatus: 'Completed' | 'NoShow') {
    if (!selected) {
      return;
    }

    const result = await Swal.fire({
      title: `Mark as ${nextStatus}?`,
      text: selected.serviceName,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Update'
    });

    if (!result.isConfirmed) {
      return;
    }

    setBusy(true);
    try {
      const updated = await withAuth((authToken) => api.updateStatus(selected.id, nextStatus, authToken));
      setSelected(updated);
      toast.success('Appointment status updated.');
      await loadData();
    } catch (err) {
      toast.error(errorMessage(err, 'Could not update appointment status.'));
    } finally {
      setBusy(false);
    }
  }

  async function addNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected || !noteBody.trim()) {
      return;
    }

    setBusy(true);
    try {
      await withAuth((authToken) => api.addNote(selected.id, noteBody.trim(), authToken));
      setNoteBody('');
      setNotes(await withAuth((authToken) => api.notes(selected.id, authToken)));
      toast.success('Note added.');
    } catch (err) {
      toast.error(errorMessage(err, 'Could not add note.'));
    } finally {
      setBusy(false);
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
        if (!auth.user.roles.includes('Staff')) {
          signOut('Staff portal is only for staff accounts.');
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
    setToday([]);
    setAppointments([]);
    setSelected(null);
    setNotes([]);
    toast.info(message);
  }

  if (!token) {
    return (
      <main className="login-page">
        <ToastContainer position="top-right" newestOnTop />
        <header className="login-topbar">
          <h1>Slotra</h1>
          <p>Manage your assigned appointments and daily schedule.</p>
        </header>

        <section className="login-layout">
          <div className="login-copy">
            <span>Appointment booking software</span>
            <h1>Work your schedule with Slotra</h1>
            <p>
              Slotra keeps appointment teams aligned with daily schedules,
              service details, status updates, and appointment notes.
            </p>
            <img className="login-visual" src="/images/login-appointments.png" alt="Appointment scheduling workspace" />
          </div>

          <div className="login-panel">
            <div className="login-card-copy">
              <h2>Staff sign in</h2>
              <p>Review today's schedule, update appointment outcomes, and keep notes for smooth handoffs.</p>
            </div>
            <form onSubmit={login}>
              <label>
                Email
                <input name="email" type="email" defaultValue="dr.smith@slotra.local" required />
              </label>
              <label>
                Password
                <input name="password" type="password" defaultValue="Staff123!" required />
              </label>
              <button type="submit" disabled={busy}>{busy ? 'Signing in...' : 'Sign in'}</button>
            </form>
            <p className="login-hint">Use your staff account to access assigned appointments only.</p>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main>
      <ToastContainer position="top-right" newestOnTop />
      <header className="topbar">
        <div>
          <h1>Slotra Staff</h1>
          <p>{user?.displayName} · {user?.email}</p>
        </div>
        <button type="button" className="ghost" onClick={() => signOut()}>Logout</button>
      </header>

      <nav className="tabs">
        <button type="button" className={tab === 'today' ? 'active' : ''} onClick={() => setTab('today')}>Today</button>
        <button type="button" className={tab === 'all' ? 'active' : ''} onClick={() => setTab('all')}>All appointments</button>
        <button type="button" className="ghost" onClick={() => loadData()} disabled={loading}>{loading ? 'Loading...' : 'Refresh'}</button>
      </nav>

      <section className="workspace">
        <section className="panel">
          <div className="filters">
            <label>
              Search
              <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search appointments" />
            </label>
            <label>
              Status
              <select value={status} onChange={(event) => setStatus(event.target.value)}>
                <option value="">All statuses</option>
                <option value="Confirmed">Confirmed</option>
                <option value="Completed">Completed</option>
                <option value="NoShow">No show</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </label>
          </div>

          <div className="list">
            {filteredAppointments.map((appointment) => (
              <button
                type="button"
                className={selected?.id === appointment.id ? 'appointment-row selected' : 'appointment-row'}
                key={appointment.id}
                onClick={() => selectAppointment(appointment)}
              >
                <span>
                  <strong>{appointment.serviceName}</strong>
                  <small>{new Date(appointment.startsAt).toLocaleString()} - {new Date(appointment.endsAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</small>
                </span>
                <em className={`status ${appointment.status.toLowerCase()}`}>{appointment.status}</em>
              </button>
            ))}
            {filteredAppointments.length === 0 ? <p className="muted">No appointments found.</p> : null}
          </div>
        </section>

        <section className="panel">
          <h2>Appointment details</h2>
          {selected ? (
            <>
              <div className="summary">
                <p><strong>Service</strong><span>{selected.serviceName}</span></p>
                <p><strong>Starts</strong><span>{new Date(selected.startsAt).toLocaleString()}</span></p>
                <p><strong>Ends</strong><span>{new Date(selected.endsAt).toLocaleString()}</span></p>
                <p><strong>Status</strong><span>{selected.status}</span></p>
              </div>

              <div className="actions">
                <button type="button" onClick={() => updateStatus('Completed')} disabled={busy || selected.status === 'Completed'}>Complete</button>
                <button type="button" className="ghost" onClick={() => updateStatus('NoShow')} disabled={busy || selected.status === 'NoShow'}>No show</button>
              </div>

              <form className="note-form" onSubmit={addNote}>
                <label>
                  Add note
                  <textarea value={noteBody} onChange={(event) => setNoteBody(event.target.value)} rows={3} />
                </label>
                <button type="submit" disabled={busy || !noteBody.trim()}>{busy ? 'Saving...' : 'Add note'}</button>
              </form>

              <div className="notes">
                {notes.map((note) => (
                  <article key={note.id} className="note">
                    <strong>{note.authorDisplayName}</strong>
                    <span>{new Date(note.createdAt).toLocaleString()}</span>
                    <p>{note.body}</p>
                  </article>
                ))}
                {notes.length === 0 ? <p className="muted">No notes yet.</p> : null}
              </div>
            </>
          ) : (
            <p className="muted">Select an appointment to view details.</p>
          )}
        </section>
      </section>
    </main>
  );
}

function errorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback;
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
