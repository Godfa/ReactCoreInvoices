# MokkilanInvoices - Architecture Documentation

## Table of Contents
1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Backend Architecture](#backend-architecture)
4. [Frontend Architecture](#frontend-architecture)
5. [Database Schema](#database-schema)
6. [Data Flow](#data-flow)
7. [External Integrations](#external-integrations)
8. [Security](#security)
9. [Deployment](#deployment)

---

## Overview

MokkilanInvoices is a full-stack invoice management application built with **Clean Architecture** and **CQRS pattern** (Command Query Responsibility Segregation). It enables collaborative expense tracking, participant management, receipt scanning, and automated payment calculations.

### Technology Stack

**Backend:**
- ASP.NET Core 8.0 Web API
- Entity Framework Core with PostgreSQL
- MediatR (CQRS implementation)
- ASP.NET Core Identity (authentication)
- JWT Bearer tokens
- QuestPDF (PDF generation)
- MailKit (email notifications)
- AutoMapper, FluentValidation

**Frontend:**
- React 18 with TypeScript
- Vite (build tool)
- MobX (state management)
- React Router v6
- Axios (HTTP client)
- Semantic UI React
- Formik + Yup (forms and validation)
- Vitest (testing)

---

## Project Structure

```
MokkilanInvoices/
├── API/                          # Web API Layer
│   ├── Controllers/              # HTTP endpoints
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Extensions/               # Service configuration
│   └── Services/                 # Application services
├── Application/                  # Business Logic Layer (CQRS)
│   ├── Core/                     # AutoMapper profiles
│   ├── Interfaces/               # Service interfaces
│   ├── Invoices/                 # Invoice feature handlers
│   ├── ExpenseItems/             # Expense item handlers
│   ├── ExpenseLineItems/         # Line item handlers
│   └── ExpenseTypes/             # Expense type handlers
├── Domain/                       # Domain Model Layer
│   └── *.cs                      # Entity classes
├── Persistence/                  # Data Access Layer
│   ├── Migrations/               # EF Core migrations
│   ├── DataContext.cs            # DbContext
│   └── Seed.cs                   # Database seeding
├── Tests/
│   ├── API.UnitTests/            # API layer tests
│   └── Application.UnitTests/    # Business logic tests
└── client-app/                   # React Frontend
    └── src/
        ├── app/                  # Core app configuration
        │   ├── api/              # HTTP client
        │   ├── layout/           # Layout components
        │   ├── models/           # TypeScript interfaces
        │   ├── router/           # Routing configuration
        │   └── stores/           # MobX state stores
        └── features/             # Feature modules
            ├── invoices/         # Invoice management
            ├── receipts/         # Receipt scanning
            ├── admin/            # User administration
            ├── profile/          # User profile
            └── users/            # Authentication
```

---

## Backend Architecture

### Layer Responsibilities

#### 1. API Layer (`/API`)

**Purpose:** HTTP request/response handling, dependency injection, authentication

**Key Components:**

**Controllers:**
- [AccountController.cs](API/Controllers/AccountController.cs) - User authentication (login, register, password management, profile)
- [AdminController.cs](API/Controllers/AdminController.cs) - User management (CRUD, role assignment)
- [InvoicesController.cs](API/Controllers/InvoicesController.cs) - Invoice CRUD, participants, status changes, approvals, PDF generation
- [ExpenseItemsController.cs](API/Controllers/ExpenseItemsController.cs) - Expense item management
- [ExpenseLineItemsController.cs](API/Controllers/ExpenseLineItemsController.cs) - Line item CRUD
- [ExpenseTypesController.cs](API/Controllers/ExpenseTypesController.cs) - Expense type enumeration
- [ReceiptsController.cs](API/Controllers/ReceiptsController.cs) - Receipt scanning proxy
- [UsersController.cs](API/Controllers/UsersController.cs) - User listing
- [BaseApiController.cs](API/Controllers/BaseApiController.cs) - Base class with MediatR

**Services:**
- [TokenService.cs](API/Services/TokenService.cs) - JWT token generation
- [EmailService.cs](API/Services/EmailService.cs) - SMTP email notifications
- [PdfService.cs](API/Services/PdfService.cs) - QuestPDF invoice generation with payment optimization
- [ReceiptScannerService.cs](API/Services/ReceiptScannerService.cs) - Receipt OCR integration

**Extensions:**
- [ApplicationServiceExtensions.cs](API/Extensions/ApplicationServiceExtensions.cs) - Service registration (DbContext, CORS, MediatR, AutoMapper, FluentValidation)
- [IdentityServiceExtensions.cs](API/Extensions/IdentityServiceExtensions.cs) - ASP.NET Identity & JWT setup

---

#### 2. Application Layer (`/Application`)

**Purpose:** Business logic, CQRS handlers, validation, mapping

**Pattern:** Feature-based organization with MediatR commands and queries

**Structure:**

```
Application/
├── Invoices/
│   ├── Create.cs                 # Command: Create new invoice
│   ├── Edit.cs                   # Command: Update invoice
│   ├── Delete.cs                 # Command: Delete invoice
│   ├── Details.cs                # Query: Get single invoice
│   ├── List.cs                   # Query: Get all invoices
│   ├── AddParticipant.cs         # Command: Add user to invoice
│   ├── RemoveParticipant.cs      # Command: Remove participant
│   ├── ChangeStatus.cs           # Command: Change invoice status
│   ├── ApproveInvoice.cs         # Command: User approval
│   ├── UnapproveInvoice.cs       # Command: Remove approval
│   └── SendPaymentNotifications.cs # Command: Email participants
├── ExpenseItems/
│   ├── Create.cs, Edit.cs, Delete.cs, Details.cs, List.cs
│   ├── AddPayer.cs               # Command: Add payer to expense
│   ├── RemovePayer.cs            # Command: Remove payer
│   └── ExpenseItemValidator.cs   # FluentValidation rules
└── ExpenseLineItems/
    ├── Create.cs, Edit.cs, Delete.cs, List.cs
    └── ExpenseLineItemValidator.cs
```

**CQRS Example:**

```csharp
// Command definition
public class Create
{
    public class Command : IRequest<Invoice>
    {
        public Invoice Invoice { get; set; }
    }

    // Handler implementation
    public class Handler : IRequestHandler<Command, Invoice>
    {
        private readonly DataContext _context;

        public async Task<Invoice> Handle(Command request, CancellationToken cancellationToken)
        {
            // Business logic: validate, auto-generate LanNumber
            _context.Invoices.Add(request.Invoice);
            await _context.SaveChangesAsync();
            return request.Invoice;
        }
    }
}
```

---

#### 3. Domain Layer (`/Domain`)

**Purpose:** Core business entities and enumerations

**Entities:**

1. **[Invoice](Domain/Invoice.cs)** - Main aggregate root
   - Properties: `Id`, `LanNumber`, `Title`, `Description`, `Image`, `Status`
   - Computed: `Amount` (sum of ExpenseItems)
   - Collections: `ExpenseItems`, `Participants`, `Approvals`

2. **[ExpenseItem](Domain/ExpenseItem.cs)** - Expense category container
   - Properties: `Id`, `OrganizerId`, `ExpenseType`, `Name`
   - Computed: `Amount` (sum of LineItems)
   - Collections: `Payers`, `LineItems`

3. **[ExpenseLineItem](Domain/ExpenseLineItem.cs)** - Individual purchase item
   - Properties: `Id`, `ExpenseItemId`, `Name`, `Quantity`, `UnitPrice`
   - Computed: `Total` (Quantity × UnitPrice)

4. **[User](Domain/User.cs)** (extends IdentityUser)
   - Properties: `DisplayName`, `MustChangePassword`, `BankAccount`, `PhoneNumber`, `PreferredPaymentMethod`
   - Collections: `Invoices`, `ExpenseItemPayers`

5. **[InvoiceParticipant](Domain/InvoiceParticipant.cs)** - Many-to-many junction
6. **[ExpenseItemPayer](Domain/ExpenseItemPayer.cs)** - Many-to-many junction
7. **[InvoiceApproval](Domain/InvoiceApproval.cs)** - Approval tracking

**Enumerations:**
- `InvoiceStatus`: Aktiivinen (0), Maksussa (1), Arkistoitu (2)
- `ExpenseType`: ShoppingList, Gasoline, Personal

---

#### 4. Persistence Layer (`/Persistence`)

**Purpose:** Database context, migrations, seed data

**Key Files:**
- [DataContext.cs](Persistence/DataContext.cs) - EF Core DbContext with Fluent API configuration
- [Seed.cs](Persistence/Seed.cs) - Database seeding (roles, users, test data)
- [Migrations/](Persistence/Migrations/) - EF Core migrations

**Database:** PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL)

**Cascade Delete Rules:**
- Delete Invoice → deletes ExpenseItems, Participants, Approvals
- Delete ExpenseItem → deletes ExpenseLineItems, Payers

---

## Frontend Architecture

### State Management (MobX)

#### [InvoiceStore](client-app/src/app/stores/invoiceStore.ts)
Main application state for invoice management.

**State:**
- `invoiceRegistry: Map<string, Invoice>` - Cached invoices
- `userRegistry: Map<string, string>` - User ID → DisplayName mapping
- `selectedInvoice: Invoice | undefined` - Currently viewed invoice
- `editMode: boolean` - Form edit mode

**Actions:**
- `loadInvoices()` - Fetch all invoices
- `createInvoice()`, `updateInvoice()`, `deleteInvoice()`
- `addParticipant()`, `removeParticipant()`
- `approveInvoice()`, `unapproveInvoice()`
- `createExpenseItem()`, `updateExpenseItem()`, `deleteExpenseItem()`
- `addPayer()`, `removePayer()`
- `createLineItem()`, `updateLineItem()`, `deleteLineItem()`

**Computed:**
- `invoices` - Sorted array of invoices
- `canCreateInvoice` - Validates only one active invoice allowed

#### [UserStore](client-app/src/app/stores/userStore.ts)
Authentication and user state.

**State:**
- `user: User | null` - Current logged-in user
- `token: string | null` - JWT token

**Actions:**
- `login()`, `logout()`, `register()`
- `changePassword()`, `forgotPassword()`, `resetPassword()`

---

### API Communication

#### [agent.ts](client-app/src/app/api/agent.ts)
Centralized Axios HTTP client with request/response interceptors.

**Configuration:**
- Base URL: `http://localhost:5000/api` (proxied via Vite)
- JWT Interceptor: Adds `Authorization: Bearer <token>` header
- Error Interceptor: Toast notifications for errors

**API Modules:**
- `Invoices` - Invoice CRUD, participants, status, approvals, PDF generation
- `ExpenseItems` - Expense CRUD, payer management
- `ExpenseLineItems` - Line item CRUD
- `ExpenseTypes` - Get type options
- `Account` - Authentication, profile management
- `Admin` - User management, role assignment
- `Users` - User listing
- `Receipts` - Receipt scanning (multipart file upload)

---

### Component Organization

#### Feature-Based Structure

```
features/
├── invoices/
│   ├── dashboard/
│   │   ├── InvoiceDashboard.tsx      # Main dashboard
│   │   └── InvoiceList.tsx           # Invoice grid
│   ├── details/
│   │   ├── InvoiceDetails.tsx        # Invoice detail view
│   │   ├── ExpenseItemList.tsx       # Expense items
│   │   ├── ParticipantList.tsx       # Participant management
│   │   ├── PaymentSettlement.tsx     # Payment calculations
│   │   ├── InvoicePrintView.tsx      # Full invoice print view
│   │   └── ParticipantInvoicePrintView.tsx
│   └── form/
│       ├── InvoiceForm.tsx           # Invoice create/edit
│       ├── ExpenseItemForm.tsx       # Expense item form
│       └── ExpenseLineItemForm.tsx   # Line item form
├── receipts/
│   └── ReceiptScanner.tsx            # Receipt OCR interface
├── admin/
│   └── AdminPage.tsx                 # User management
├── profile/
│   └── ProfilePage.tsx               # User profile editing
└── users/
    ├── LoginForm.tsx
    ├── ChangePasswordForm.tsx
    ├── ForgotPasswordForm.tsx
    └── ResetPasswordForm.tsx
```

---

## Database Schema

### Entity Relationship Diagram

```
┌─────────────────┐
│     Users       │
│  (IdentityUser) │
│─────────────────│
│ Id (PK)         │
│ UserName        │
│ Email           │
│ DisplayName     │
│ BankAccount     │
│ PhoneNumber     │
│ PreferredPayment│
└────────┬────────┘
         │
         │ 1:N
         ↓
┌────────────────────────┐         ┌──────────────────┐
│   InvoiceParticipant   │────────→│     Invoice      │
│  (Junction Table)      │  N:1    │─────────────────│
│────────────────────────│         │ Id (PK)          │
│ InvoiceId (FK, PK)     │    ┌───→│ LanNumber        │
│ AppUserId (FK, PK)     │    │    │ Title            │
└────────────────────────┘    │    │ Description      │
                               │    │ Image            │
                               │    │ Status (enum)    │
┌────────────────────────┐    │    │ Amount (computed)│
│   InvoiceApproval      │────┘    └────────┬─────────┘
│  (Tracking Table)      │  N:1             │
│────────────────────────│                  │ 1:N
│ InvoiceId (FK, PK)     │                  ↓
│ AppUserId (FK, PK)     │         ┌─────────────────┐
│ ApprovedAt             │         │  ExpenseItem    │
└────────────────────────┘    ┌───→│─────────────────│
                               │    │ Id (PK)          │
                               │    │ InvoiceId (FK)   │
┌────────────────────────┐    │    │ OrganizerId (FK) │
│  ExpenseItemPayer      │────┘    │ ExpenseType      │
│  (Junction Table)      │  N:1    │ Name             │
│────────────────────────│         │ Amount (computed)│
│ ExpenseItemId (FK, PK) │         └────────┬─────────┘
│ AppUserId (FK, PK)     │                  │
└────────────────────────┘                  │ 1:N
                                            ↓
                                   ┌──────────────────┐
                                   │ ExpenseLineItem  │
                                   │──────────────────│
                                   │ Id (PK)          │
                                   │ ExpenseItemId(FK)│
                                   │ Name             │
                                   │ Quantity         │
                                   │ UnitPrice        │
                                   │ Total (computed) │
                                   └──────────────────┘
```

### Key Relationships

- **Invoice** ← 1:N → **ExpenseItem** ← 1:N → **ExpenseLineItem** (hierarchical aggregation)
- **Invoice** ← N:M → **User** (via InvoiceParticipant) - who participates
- **Invoice** ← N:M → **User** (via InvoiceApproval) - who approved
- **ExpenseItem** ← N:M → **User** (via ExpenseItemPayer) - who owes money
- **ExpenseItem** ← N:1 → **User** (Organizer) - who paid

---

## Data Flow

### Request Flow (Frontend → Backend)

```
1. User Interaction (React Component)
   ↓
2. MobX Store Action (invoiceStore.createInvoice())
   ↓
3. API Agent Call (agent.Invoices.create())
   ↓
4. Axios HTTP Request (POST /api/invoices) + JWT token
   ↓
5. API Controller (InvoicesController.CreateInvoice())
   ↓
6. MediatR Command (Mediator.Send(new Create.Command))
   ↓
7. Command Handler (Create.Handler.Handle())
   ↓
8. Business Logic Execution
   ↓
9. EF Core DbContext (context.Invoices.Add())
   ↓
10. PostgreSQL Database
   ↓
11. Response (Invoice entity)
   ↓
12. MobX Observable State Update (runInAction)
   ↓
13. React Re-render (MobX observer HOC)
```

### Authentication Flow

```
1. User submits login form (LoginForm.tsx)
   ↓
2. userStore.login(credentials)
   ↓
3. agent.Account.login() → POST /api/account/login
   ↓
4. AccountController validates credentials with SignInManager
   ↓
5. TokenService generates JWT token
   ↓
6. UserDto returned with token
   ↓
7. Token stored in localStorage
   ↓
8. Axios interceptor adds token to all subsequent requests
   ↓
9. JWT validation on protected endpoints
```

---

## Main Features

### 1. Invoice Management
- Create invoices with auto-generated LanNumber and title
- Only one active invoice allowed at a time (business rule)
- Status workflow: Aktiivinen → Maksussa → Arkistoitu
- Multi-participant support
- Approval workflow (participants must approve before payment)
- PDF generation (full invoice and participant-specific)

### 2. Expense Tracking
- Three expense types: ShoppingList, Gasoline, Personal
- Hierarchical structure: Invoice → ExpenseItem → ExpenseLineItem
- Line items with quantity, unit price, and computed totals
- Automatic amount aggregation (LineItems → ExpenseItem → Invoice)
- Multiple payers per expense item

### 3. Payment Settlement
- Optimized payment calculations using intermediary model
- Minimizes number of transactions between participants
- Algorithm implementation in [PdfService.cs:394-453](API/Services/PdfService.cs#L394-L453)
- Displays preferred payment method with fallback options
- Validation: Pankki requires valid IBAN, MobilePay requires phone number

### 4. Receipt Scanning
- Upload receipt images (JPEG, PNG, WEBP, max 10MB)
- OCR extraction of line items (name, quantity, unit price)
- Multi-provider support:
  - Azure AI Document Intelligence
  - Ollama (local vision models: llama3.2-vision, minicpm-v)
  - Auto (falls back to available provider)
- Direct integration with ExpenseLineItem creation

### 5. User Management (Admin)
- Create users with auto-generated temporary passwords
- Update user profiles (name, email, bank account, payment method)
- Delete users
- Grant/revoke admin role
- Password reset functionality
- Email notifications for new users and password resets

### 6. Email Notifications
- New user welcome emails with credentials
- Password reset notifications
- Payment notification emails to participants
- SMTP configuration via appsettings

---

## External Integrations

### 1. ReceiptScanner Service
- **Type:** External microservice (separate codebase)
- **Purpose:** OCR extraction from receipt images
- **Protocol:** HTTP REST API
- **Configuration:** `ReceiptScanner:BaseUrl` in appsettings (default: `http://localhost:5205`)
- **Providers:** Azure AI Document Intelligence, Ollama (vision models)
- **Request:** Multipart file upload with language and provider parameters
- **Response:** JSON with line items (description, quantity, unitPrice, lineTotal)
- **Error Handling:** Service unavailable detection, timeout handling (2 min)

### 2. Email (SMTP)
- **Protocol:** SMTP via MailKit
- **Configuration:** Email:SmtpHost, SmtpPort, SmtpUsername, SmtpPassword
- **Use Cases:**
  - New user account creation
  - Password reset notifications
  - Payment notifications to participants

---

## Security

### Authentication
- JWT tokens with configurable secret key (User Secrets or environment variable)
- Token stored in localStorage (client-side)
- Bearer token sent in `Authorization` header
- Password hashing via ASP.NET Core Identity
- Password change enforcement (`MustChangePassword` flag)

### Authorization
- **Role-based:** Admin role for user management and invoice deletion
- **User-specific:** Users can only approve invoices they participate in (admin override allowed)

### Validation
- FluentValidation on backend (ExpenseItemValidator, ExpenseLineItemValidator)
- Formik + Yup validation on frontend
- IBAN validation (regex pattern: `/^[A-Z]{2}\d{2}[A-Z0-9]+$/`, min length 15)
- Phone number validation (must exist for MobilePay option)

### CORS
- Configurable allowed origins via `AllowedOrigins` environment variable or appsettings
- Default: `http://localhost:3000` (development)

### Sensitive Data
- User secrets for connection strings (development)
- Environment variables for production
- Bank account and payment method in User entity (not exposed in tokens)
- Secure password reset with token expiration

---

## Deployment

### Development Environment
```bash
# Backend (from root directory)
dotnet watch run --project API
# Runs on http://localhost:5000

# Frontend (from client-app directory)
npm start
# Runs on http://localhost:3000 with Vite dev server
# Vite proxy: /api → http://localhost:5000

# Database
# PostgreSQL: Local or Docker container
# Auto-migration and seed data on startup
```

### Production (VPS)
- **Backend:** Systemd service (MokkilanInvoices.service)
- **Reverse Proxy:** Nginx
- **Database:** PostgreSQL
- **Deployment:** GitHub Actions CI/CD
- **SSL/TLS:** Certbot (Let's Encrypt)
- **Process Management:** Systemd
- **User:** `www-data` for security

### Environment Variables (Production)
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<PostgreSQL connection string>
TokenKey=<JWT secret key>
AllowedOrigins=https://yourdomain.com
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUsername=<email>
Email__SmtpPassword=<app password>
ReceiptScanner__BaseUrl=http://localhost:5205
```

---

## Testing

### Backend Tests
- **Framework:** xUnit
- **Mocking:** Moq
- **Projects:**
  - `API.UnitTests` - Controller tests (AccountController, AdminController, EmailService)
  - `Application.UnitTests` - Handler tests (ExpenseItems, ExpenseLineItems, cascade deletes)

### Frontend Tests
- **Framework:** Vitest + jsdom
- **Libraries:** @testing-library/react, @testing-library/user-event
- **Coverage:** InvoiceStore tests (invoiceStore.test.ts, invoiceStore.lineItems.test.ts)

---

## Architectural Patterns

### Clean Architecture
- **Domain Layer:** Core business entities, no dependencies
- **Application Layer:** Business logic, depends on Domain
- **Persistence Layer:** Data access, depends on Domain
- **API Layer:** HTTP interface, depends on Application and Persistence

### CQRS (Command Query Responsibility Segregation)
- Commands: Modify state (Create, Edit, Delete, AddParticipant, etc.)
- Queries: Read state (List, Details)
- Implemented via MediatR handlers
- Clear separation of read/write operations

### Repository Pattern
- Entity Framework Core DbContext acts as repository
- No additional abstraction layer (YAGNI principle)
- Direct context usage in handlers

### Dependency Injection
- Services registered in `ApplicationServiceExtensions` and `IdentityServiceExtensions`
- Constructor injection throughout the application
- Scoped lifetime for DbContext, Singleton for stores

---

## Key Design Decisions

### Why PostgreSQL?
- Open-source, production-ready RDBMS
- Strong support for relational data and complex queries
- Cost-effective for VPS deployment
- Excellent EF Core integration

### Why MobX over Redux?
- Less boilerplate code
- Reactive programming model (observables)
- Easier to learn and use for smaller teams
- Automatic dependency tracking for computed values

### Why CQRS?
- Clear separation of concerns
- Easy to test handlers in isolation
- Scalable for complex business logic
- Supports future read/write separation if needed

### Why QuestPDF over iTextSharp?
- Modern, fluent API
- Open-source with permissive license
- Excellent documentation and community support
- Type-safe document composition

---

## Future Considerations

### Potential Enhancements
- Real-time updates with SignalR
- File attachments for receipts
- Multi-currency support
- Recurring invoices
- Export to Excel/CSV
- Mobile app (React Native or MAUI)
- Audit logging
- Advanced reporting and analytics

### Scalability
- Read/write database separation
- Redis caching for user sessions
- Azure Blob Storage for images
- Horizontal scaling with load balancer
- Microservices decomposition (if needed)

---

## Conclusion

MokkilanInvoices demonstrates a well-architected, maintainable full-stack application following industry best practices. The Clean Architecture approach ensures separation of concerns, testability, and long-term maintainability. The CQRS pattern provides clarity in business logic and supports future scalability needs.

The codebase is suitable for collaborative invoice management, expense tracking, and automated payment settlement workflows in small to medium-sized teams or organizations.
