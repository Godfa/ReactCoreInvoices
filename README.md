# MokkilanInvoices

A full-stack invoice management application built with React and ASP.NET Core 8.0. Manage invoices, track expenses, handle participant approvals, and generate PDF invoices.

## Features

- **Invoice Management** - Create, edit, and track invoices with multiple expense items
- **Expense Tracking** - Line-item expense tracking with categorization
- **Participant Management** - Add participants to invoices, track individual shares
- **Approval Workflow** - Multi-participant approval process with status tracking
- **PDF Generation** - Generate printable PDF invoices using QuestPDF
- **Email Notifications** - Send payment notifications to participants
- **User Authentication** - JWT-based authentication with role-based authorization
- **Admin Panel** - User management for administrators

## Tech Stack

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- MediatR (CQRS pattern)
- AutoMapper
- FluentValidation
- ASP.NET Core Identity + JWT
- QuestPDF for PDF generation
- SQL Server

### Frontend
- React 18 with TypeScript
- Vite
- MobX for state management
- React Router v6
- Axios
- Formik + Yup for forms
- Semantic UI React

## Project Structure

```
MokkilanInvoices/
├── API/                 # ASP.NET Core Web API
├── Application/         # Business logic, MediatR handlers
├── Domain/              # Entity models
├── Persistence/         # EF Core DbContext, migrations
├── Tests/               # Unit tests
└── client-app/          # React frontend
```

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- SQL Server LocalDB (included with Visual Studio) or SQL Server

### Setup

1. Clone the repository
   ```bash
   git clone https://github.com/yourusername/MokkilanInvoices.git
   cd MokkilanInvoices
   ```

2. Start the API (from root directory)
   ```bash
   dotnet watch run --project API
   ```
   The API runs on `http://localhost:5000`. Database migrations run automatically on startup.

3. Start the frontend (in a new terminal)
   ```bash
   cd client-app
   npm install
   npm start
   ```
   The frontend runs on `http://localhost:3000` with API proxy configured.

4. Open `http://localhost:3000` in your browser

## Development

### Running Tests

```bash
# Backend tests
dotnet test

# Frontend tests
cd client-app
npm run test
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project Persistence --startup-project API

# Update database
dotnet ef database update --project API
```

## Configuration

- **API/appsettings.Development.json** - Development settings (connection string, JWT key, CORS origins)
- **API/appsettings.json** - Production settings
- **client-app/vite.config.ts** - Vite configuration with API proxy

## License

MIT
