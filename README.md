# UL Optometry Clinical Management System

A clinical management platform for the University of Limpopo Optometry Department, designed to manage clinical training, patient flow, supervision, and HPCSA compliance.

## Architecture

```
.NET MAUI (Mobile App)        ← Future mobile client
        ↓
ASP.NET Core 8 API            ← This repository
        ↓
SQL Server Database
```

## Solution Structure

```
ULOptometry.slnx
└── src/
    ├── ULOptometry.Domain/        # Shared domain models & enums
    │   ├── Entities/              # EF Core entity classes
    │   └── Enums/                 # Domain enumerations
    └── ULOptometry.API/           # ASP.NET Core 8 Web API
        ├── Controllers/           # REST API endpoints per role
        ├── Data/                  # EF Core DbContext
        ├── DTOs/                  # Request/Response models
        └── Services/              # Business logic services
```

## User Roles

| Role | Responsibility |
|------|---------------|
| **Admin** | System control & operations |
| **Patient** | Booking & attendance |
| **Student** | Clinical execution |
| **Supervisor** | Clinical validation |

## Design System

| Color | Code | Meaning |
|-------|------|---------|
| �� Primary | `#1E3A8A` | Main actions, headers |
| 🔵 Accent | `#3B82F6` | Active states |
| ⚪ White | `#FFFFFF` | Cards/background |
| ⚫ Dark | `#1F2937` | Text |
| 🟢 Green | `#22C55E` | Approved / Success |
| 🟡 Orange | `#F59E0B` | Pending |
| 🔴 Red | `#EF4444` | Cancel / Reject |
| ⚫ Grey | `#6B7280` | Secondary info |

## Key Business Rules

- **3 sessions/day** (Morning, Afternoon, Evening), **8 cubicles**
- Students can **Accept** or **Cancel** bookings (no decline)
- Cancel returns booking to the queue
- Encounter types:
  - **Onsite**: Submit → Supervisor Review → Approved → Locked
  - **Offsite**: Enter supervisor name + OP number → Auto-approved → Locked
- Approved encounters are **immutable** (locked)
- Only **approved** encounters count toward PoE
- After sign-off: **patient details masked** (POPIA compliance)
- **Minimum data retention: 6 years**
- **All actions audited** (user, action, timestamp, before/after)
- First login requires **password change**

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login (returns JWT) |
| POST | `/api/auth/change-password` | Change password (first-login) |

### Admin
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/dashboard` | Dashboard stats |
| GET/POST | `/api/admin/users` | Manage users |
| PUT | `/api/admin/users/{id}/activate` | Activate/deactivate user |
| DELETE | `/api/admin/users/{id}` | Soft-delete user |
| GET/POST | `/api/admin/sessions` | Manage clinic sessions |
| GET | `/api/admin/cubicles` | List cubicles |
| POST | `/api/admin/cubicles/assign` | Assign student+supervisor to cubicle |
| GET | `/api/admin/bookings` | Monitor all bookings |
| GET | `/api/admin/reports/poe-summary` | PoE reports |
| GET | `/api/admin/reports/audit` | Audit log |
| POST | `/api/admin/notifications/broadcast` | Broadcast notification |

### Patient
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/patient/available-sessions` | View available sessions |
| GET/POST | `/api/patient/bookings` | Manage own bookings |
| GET | `/api/patient/bookings/{id}` | View booking (read-only encounter) |
| GET | `/api/patient/notifications` | View notifications |
| PUT | `/api/patient/notifications/{id}/read` | Mark notification read |

### Student
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/student/dashboard` | Dashboard stats |
| GET | `/api/student/booking-queue` | View pending bookings |
| POST | `/api/student/bookings/{id}/accept` | Accept a booking |
| POST | `/api/student/bookings/{id}/cancel` | Cancel accepted booking |
| GET | `/api/student/encounters` | View own encounters |

### Encounter
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/encounter` | Create encounter (Student) |
| PUT | `/api/encounter/{id}` | Update encounter (Student) |
| POST | `/api/encounter/{id}/submit` | Submit encounter (Student) |
| POST | `/api/encounter/{id}/review` | Approve/Reject (Supervisor) |
| GET | `/api/encounter/{id}` | Get encounter details |

### Supervisor
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/supervisor/dashboard` | Dashboard stats |
| GET | `/api/supervisor/review-queue` | Encounters awaiting review |
| GET | `/api/supervisor/assignments` | Cubicle assignments |
| GET | `/api/supervisor/poe` | View student PoE |

### PoE
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/poe` | PoE summary |
| GET | `/api/poe/records` | Detailed PoE records |

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or full instance)

### Setup

```bash
# Restore dependencies
dotnet restore ULOptometry.slnx

# Navigate to API project
cd src/ULOptometry.API

# Create initial migration
dotnet ef migrations add Initial

# Apply migration (creates database)
dotnet ef database update

# Run the API
dotnet run
```

The API will be available at `https://localhost:5001` and the Swagger UI at `https://localhost:5001/swagger`.

### Configuration

Update `appsettings.json` or use environment variables / User Secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ULOptometryDB;Trusted_Connection=True"
  },
  "Jwt": {
    "Key": "<your-secret-key-min-32-chars>",
    "Issuer": "ULOptometryAPI",
    "Audience": "ULOptometryClients"
  }
}
```

> **Security Note:** Move the JWT key to environment variables or User Secrets before deploying to production.

## System Flow

```
Admin sets up sessions & cubicle assignments
        ↓
Patient books appointment
        ↓
Student accepts booking from queue
        ↓
Encounter performed in cubicle
        ↓
[Onsite]  Student submits → Supervisor reviews → Approved
[Offsite] Student enters supervisor name + OP → Auto-approved
        ↓
Encounter locked (immutable)
        ↓
PoE record created
        ↓
Patient data masked (POPIA)
        ↓
Admin monitors via dashboard & reports
```

## Compliance

- **POPIA**: Patient data masked after sign-off, role-based access control
- **HPCSA**: Encounter audit trail, PoE tracking, supervisor sign-off
- **Data Retention**: Minimum 6-year retention policy (enforced via soft deletes)
- **Audit Log**: All actions tracked with user, action, timestamp, before/after state
