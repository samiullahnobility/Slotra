# Slotra

Slotra is an appointment booking MVP built as a small multi-app system with one shared backend API. The first phase focuses on the backend foundation: identity, roles, booking, staff scheduling, service management, notifications, and admin reporting.

## Project Plan

### Apps

- Backend API: ASP.NET Core Web API with EF Core, Identity, JWT, Swagger, repositories, services, and unit of work.
- Admin dashboard: Angular app for admins to manage services, staff, appointments, and reporting.
- Customer portal: Next.js app for customers to browse services, choose staff, book appointments, and manage their bookings.
- Staff panel: React app for staff to view daily appointments, update statuses, and manage availability.
- Notification worker: Node.js worker that reads pending notifications from the API and sends email/SMS later.

### Phase 1 Backend Scope

- User registration and login.
- ASP.NET Core Identity user management.
- JWT authentication with role claims.
- Admin, Staff, and Customer roles.
- Seed admin, demo staff, demo customer, and sample service data.
- Service CRUD.
- Staff profile CRUD.
- Staff-service assignment.
- Staff availability management.
- Customer booking flow.
- Appointment list, cancel, reschedule, status update, and notes.
- Notification queue endpoints for worker integration.
- Admin dashboard summary endpoint.
- Swagger bearer authentication.
- N-tier project structure with controllers, DTOs, services, repositories, and unit of work.

## Current Backend Status

The backend API is mostly complete for Phase 1.

Implemented:

- EF Core models and migrations.
- `SlotraDbContext`.
- ASP.NET Core Identity with custom `AppUser` and `AppRole`.
- JWT login, register, refresh token, and current-user endpoint.
- Seeded roles: `Admin`, `Staff`, `Customer`.
- Seeded users:
  - Admin: `admin@slotra.local` / `Admin123!`
  - Staff: `dr.smith@slotra.local` / `Staff123!`
  - Customer: `customer@slotra.local` / `Customer123!`
- Swagger bearer auth.
- Generic repository.
- Generic service.
- Unit of work.
- Service management.
- Staff management.
- Booking service.
- Appointment management.
- Notification queue endpoints.
- Admin dashboard summary.
- Business timezone support through `BusinessClock`.
- Validation for overlapping staff availability.
- Validation for duplicate staff-service assignment.
- Validation for invalid appointment status filters.
- Audit fields on core business entities:
  - `CreatedAt`
  - `UpdatedAt`
  - `CreatedBy`
  - `UpdatedBy`

Skipped for now:

- Secret handling cleanup.
- Automated tests.

## Backend Structure

```text
backend-dotnet-api/
  Common/
  Controllers/
  Data/
  DTOs/
  Migrations/
  Models/
  Repositories/
  Services/
  UnitOfWork/
  Program.cs
  Slotra.Api.csproj
```

## Main API Areas

### Auth

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `GET /api/v1/auth/me`

### Services

- Create, update, list, get, and delete services.
- Service delete is handled safely for services with existing appointments.
- Duplicate service names are validated.

### Staff

- Create and update staff profiles.
- Assign and remove services from staff.
- Add, update, and remove staff availability.
- Reject overlapping availability windows.

### Booking

- List bookable services.
- List available staff by service.
- Return available appointment slots.
- Create appointments.
- Prevent overlapping bookings.
- Validate service, staff assignment, availability, and future appointment time.

### Appointments

- List appointments with filters and paging.
- List current customer's appointments.
- Cancel appointments.
- Reschedule appointments.
- Update appointment status.
- Add and list appointment notes.
- Invalid status filters return an error.

### Notifications

- List pending notifications for worker.
- Mark notification as sent.
- Mark notification as failed.
- Protected by worker API key.

### Admin Dashboard

- Summary endpoint for appointment totals and revenue-style reporting.

## Requirements

- .NET SDK compatible with `net10.0`.
- SQL Server or SQL Server LocalDB.
- EF Core CLI tool.

The API project includes a local tool manifest:

```powershell
dotnet tool restore
```

## Configuration

Main config file:

```text
backend-dotnet-api/appsettings.json
```

Important settings:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiresInMinutes`
- `BusinessTimeZone`
- `Worker:ApiKey`
- `SeedAdmin`
- `SeedDemo`

Current database connection:

```json
"DefaultConnection": "Server=NOB-IT037;Database=SlotraDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

On another PC, update the SQL Server name if needed.

## Run Backend API

From the repo root:

```powershell
cd backend-dotnet-api
dotnet restore
dotnet tool restore
dotnet build
dotnet ef database update
dotnet run --launch-profile https
```

Open Swagger:

```text
https://localhost:44301/swagger
```

The root URL may return 404:

```text
https://localhost:44301/
```

That is expected for an API project unless a root redirect/controller is added.

## Login Flow in Swagger

1. Open Swagger.
2. Call `POST /api/v1/auth/login`.
3. Use one seeded account, for example:

```json
{
  "email": "admin@slotra.local",
  "password": "Admin123!"
}
```

4. Copy the returned JWT token.
5. Click Swagger `Authorize`.
6. Enter:

```text
Bearer <token>
```

7. Call protected endpoints.

## Known Runtime Note

If login throws this error:

```text
Record type 'Slotra.Api.DTOs.Auth.LoginRequest' has validation metadata defined on property 'Password'
```

The source DTO should be a normal class, not a positional record. If the source already shows `LoginRequest` as a class, stop the running API, rebuild, and run again so the app loads the latest assembly.

## Frontend Plan

### Angular Admin Dashboard

Started in:

```text
admin-angular/
```

Admin portal status: complete for MVP.

- Login page.
- JWT auth service.
- Auth interceptor.
- Admin route guard.
- Protected admin shell layout.
- Dashboard summary page.
- Services CRUD page.
- Staff CRUD page.
- Staff detail page with service assignment.
- Staff availability management.
- Appointments page with filters, status updates, and notes.
- Environment-based API URL config.
- Loading, success, and error states on key workflows.
- `ngx-toastr` for success/error notifications.
- `sweetalert2` for delete/remove confirmations.
- Search on Services and Staff lists.
- Search on Appointments list.
- Pagination controls for Services, Staff, and Appointments.
- Mobile sidebar toggle.
- Toast-only success/error notifications.

Run after Node/npm is healthy:

```powershell
cd admin-angular
npm install
npm start
```

Open:

```text
http://localhost:4200
```

The admin portal expects the backend API at:

```text
https://localhost:44301/api/v1
```

Main screens:

- Login.
- Dashboard summary.
- Services list and form.
- Staff list and profile form.
- Staff service assignment.
- Staff availability editor.
- Appointment list with filters.
- Appointment details and notes.

### Next.js Customer Portal

Main screens:

- Register/login.
- Browse services.
- Choose staff.
- Select available slot.
- Book appointment.
- My appointments.
- Cancel/reschedule appointment.

### React Staff Panel

Main screens:

- Staff login.
- Today's appointments.
- Appointment details.
- Status update.
- Availability view.

### Node Notification Worker

Worker responsibilities:

- Poll `GET /api/v1/notifications/pending`.
- Send email/SMS later.
- Call mark-as-sent on success.
- Call mark-as-failed on failure.
- Use `X-Worker-Api-Key`.

## Suggested Next Process

1. Confirm backend runs and Swagger works.
2. Fix any runtime DTO/build issue.
3. Add optional root redirect to Swagger.
4. Build Angular admin dashboard.
5. Connect Angular login to JWT auth.
6. Build admin service management screens.
7. Build staff management screens.
8. Build appointment management screens.
9. Build Next.js customer booking portal.
10. Build React staff panel.
11. Build notification worker.
12. Add automated tests.
13. Move secrets out of `appsettings.json` before production.

## Git Sharing

To move this project to another PC:

```powershell
git status
git add .
git commit -m "Build Slotra phase 1 backend API"
git push
```

On the other PC:

```powershell
git clone <repo-url>
cd Slotra/backend-dotnet-api
dotnet restore
dotnet tool restore
dotnet ef database update
dotnet run --launch-profile https
```
