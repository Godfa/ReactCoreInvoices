# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MokkilanInvoices is a full-stack invoice management application with a React/TypeScript frontend and ASP.NET Core 8.0 backend. The application handles invoice creation, expense tracking, participant management, approval workflows, and PDF generation.

## Common Commands

### Backend (.NET)
```bash
# Run API with hot reload (from root directory)
dotnet watch run --project API

# Build solution
dotnet build

# Run all backend tests
dotnet test

# Run specific test project
dotnet test Tests/Application.UnitTests/Application.UnitTests.csproj

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project Persistence --startup-project API

# Update database
dotnet ef database update --project API
```

### Frontend (from client-app directory)
```bash
# Development server (Vite on port 3000)
npm start

# Run tests (Vitest watch mode)
npm run test

# Run tests with UI
npm run test:ui

# Production build
npm run build
```

## Architecture

### Backend Structure (Clean Architecture + CQRS)
- **API/** - Controllers, DTOs, Services (EmailService, PdfService, TokenService), Program entry point
- **Application/** - MediatR command/query handlers, business logic, FluentValidation validators
- **Domain/** - Entity models (Invoice, ExpenseItem, ExpenseLineItem, AppUser, Participant, Payer)
- **Persistence/** - EF Core DataContext, migrations, seeding
- **Tests/Application.UnitTests/** - xUnit tests with Moq

### Frontend Structure (client-app/src)
- **app/api/agent.ts** - Axios HTTP client with JWT interceptors, all API modules
- **app/stores/** - MobX stores (invoiceStore, userStore, store)
- **app/models/** - TypeScript interfaces
- **app/router/Routes.tsx** - React Router v6 configuration
- **features/** - Feature-based components (invoices, users, admin, profile)

### Key Patterns
- **Backend**: MediatR for CQRS, AutoMapper for DTOs, FluentValidation, JWT Bearer auth, ASP.NET Core Identity
- **Frontend**: MobX for state management, Formik + Yup for forms, Semantic UI React for components

### Data Flow
1. React component calls MobX store method
2. Store uses Axios client (agent.ts) to call REST API
3. Controller dispatches MediatR command/query
4. Handler uses EF Core to access database
5. Response updates MobX observable state, triggering React re-render

## Development Setup

Run both backend and frontend:
1. `dotnet watch run` in `/API` directory (runs on port 5000)
2. `npm start` in `/client-app` directory (runs on port 3000 with proxy to API)

Database auto-migrates and seeds sample data on startup. In development mode, invoice data resets on each restart.

## Configuration

- **API/appsettings.Development.json** - Local dev settings (LocalDB connection, token key)
- **client-app/vite.config.ts** - Vite dev server config with API proxy (`/api` -> `http://localhost:5000`)
- Database: SQL Server LocalDB (dev) / SQL Server (production)