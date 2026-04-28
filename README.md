# UL Optometry Clinical Management System

A complete ASP.NET Core 8 Web API for managing clinical operations at the University of Limpopo Optometry department.

## Architecture

- **ULOptometry.Domain** – Class library with all domain entities and enums
- **ULOptometry.API** – ASP.NET Core 8 Web API with EF Core, JWT auth, and Swagger

## Features

- **Role-based access**: Admin, Patient, Student, Supervisor
- **Booking management**: 3 sessions/day (Morning, Afternoon, Evening), 8 cubicles
- **Encounter management**: Onsite/Offsite with supervisor approval workflow
- **Portfolio of Evidence (PoE)**: Automatic tracking of clinical hours per clinic type
- **POPIA compliance**: Patient data masked after encounter sign-off
- **Audit logging**: All actions logged with before/after snapshots
- **Notifications**: In-app notifications with broadcast support

## Tech Stack

- .NET 8 / ASP.NET Core 8
- Entity Framework Core 8 with SQL Server
- JWT Bearer authentication
- BCrypt.Net-Next for password hashing
- Swagger/OpenAPI documentation

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server or SQL Server LocalDB

### Setup

1. Update the connection string in `appsettings.json`
2. Run EF Core migrations:
   ```bash
   cd src/ULOptometry.API
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
3. Run the API:
   ```bash
   dotnet run --project src/ULOptometry.API
   ```
4. Open Swagger UI at `https://localhost:{port}/swagger`

## API Endpoints

### Auth
- `POST /api/auth/login` – Login and get JWT token
- `POST /api/auth/change-password` – Change password (requires auth)

### Admin
- `GET /api/admin/dashboard` – Dashboard stats
- `GET/POST /api/admin/users` – Manage users
- `PUT /api/admin/users/{id}/activate` – Activate/deactivate user
- `DELETE /api/admin/users/{id}` – Soft delete user
- `GET/POST /api/admin/sessions` – Manage clinic sessions
- `GET /api/admin/cubicles` – List cubicles
- `POST /api/admin/cubicles/assign` – Assign student/supervisor to cubicle
- `GET /api/admin/bookings` – View all bookings
- `GET /api/admin/reports/poe-summary` – PoE summary report
- `GET /api/admin/reports/audit` – Audit log
- `POST /api/admin/notifications/broadcast` – Broadcast notifications

### Patient
- `GET/POST /api/patient/bookings` – View/create bookings
- `GET /api/patient/bookings/{id}` – Booking details
- `GET /api/patient/available-sessions` – Available sessions
- `GET /api/patient/notifications` – Notifications
- `PUT /api/patient/notifications/{id}/read` – Mark notification as read

### Student
- `GET /api/student/dashboard` – Dashboard stats
- `GET /api/student/booking-queue` – View assigned patient queue
- `POST /api/student/bookings/{id}/accept` – Accept a booking
- `POST /api/student/bookings/{id}/cancel` – Cancel a booking
- `GET /api/student/encounters` – View my encounters

### Encounter
- `POST /api/encounter` – Create encounter (Student)
- `PUT /api/encounter/{id}` – Update encounter (Student)
- `POST /api/encounter/{id}/submit` – Submit for review (Student)
- `POST /api/encounter/{id}/review` – Approve/reject (Supervisor)
- `GET /api/encounter/{id}` – Get encounter details

### Supervisor
- `GET /api/supervisor/dashboard` – Dashboard stats
- `GET /api/supervisor/review-queue` – Encounters pending review
- `GET /api/supervisor/assignments` – Cubicle assignments
- `GET /api/supervisor/poe` – View student PoE records

### PoE
- `GET /api/poe` – PoE summary
- `GET /api/poe/records` – Detailed PoE records

## Clinic Types
GeneralOptometry, PaediatricOptometry, ContactLens, LowVision, Binocular, Neuro, CommunityCare

## POPIA Compliance
Patient personal data (ID number, address) is automatically masked (set to null) after an encounter is approved, complying with the Protection of Personal Information Act.
