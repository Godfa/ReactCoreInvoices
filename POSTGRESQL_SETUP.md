# PostgreSQL Setup Guide

## Migration from Azure SQL Server to PostgreSQL

MokkilanInvoices on siirretty käyttämään PostgreSQL-tietokantaa Azure SQL Serverin sijaan.

## Prerequisites

### 1. Install PostgreSQL

**Windows:**
```bash
# Download from: https://www.postgresql.org/download/windows/
# Or use Chocolatey:
choco install postgresql
```

**macOS:**
```bash
brew install postgresql
brew services start postgresql
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### 2. Create Database

```bash
# Login to PostgreSQL as postgres user
psql -U postgres

# Create database
CREATE DATABASE "LanInvoices";

# Create user (optional, if not using default postgres user)
CREATE USER laninvoices_user WITH PASSWORD 'your_secure_password';

# Grant privileges
GRANT ALL PRIVILEGES ON DATABASE "LanInvoices" TO laninvoices_user;

# Exit
\q
```

## Configuration

### Development Environment

1. Copy `.env.example` to `.env` (or set environment variables):
```bash
cp .env.example .env
```

2. Edit `.env` or `appsettings.Development.json` with your PostgreSQL connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LanInvoices;Username=postgres"
  }
}
```

3. Set the PostgreSQL password as an environment variable:
```bash
# Windows (PowerShell)
$env:PGPASSWORD="your_password"

# Windows (Command Prompt)
set PGPASSWORD=your_password

# Linux/macOS
export PGPASSWORD=your_password
```

**Or use .NET User Secrets (recommended for development):**
```bash
cd API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=LanInvoices;Username=postgres;Password=your_password"
```

### Production Environment

Set the full connection string as an environment variable:

```bash
# Linux/Docker
export ConnectionStrings__DefaultConnection="Host=your-server.com;Port=5432;Database=LanInvoices;Username=your_user;Password=your_password;SSL Mode=Require"

# Or in appsettings.Production.json (without password):
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-server.com;Port=5432;Database=LanInvoices;Username=your_user;SSL Mode=Require"
  }
}
# And set password via environment variable:
export PGPASSWORD="your_secure_password"
```

## Running Migrations

Apply the PostgreSQL migrations to create all tables:

```bash
cd API
dotnet ef database update --project ../Persistence
```

This will create all necessary tables:
- AspNetUsers (Identity tables)
- Invoices
- ExpenseItems
- ExpenseLineItems
- InvoiceParticipants
- ExpenseItemPayers
- InvoiceApprovals

## Verify Connection

Test the database connection:

```bash
# Using psql
psql -h localhost -U postgres -d LanInvoices -c "\dt"

# Or start the API and check logs
cd API
dotnet run
```

You should see all tables listed without errors.

## Troubleshooting

### Connection Issues

**Error: "password authentication failed"**
- Check that PGPASSWORD environment variable is set
- Or add Password to connection string (not recommended for production)
- Verify user credentials in PostgreSQL

**Error: "could not connect to server"**
- Verify PostgreSQL is running: `sudo systemctl status postgresql` (Linux) or check Services (Windows)
- Check port 5432 is not blocked by firewall
- Verify Host in connection string is correct

**Error: "database does not exist"**
- Create database manually: `CREATE DATABASE "LanInvoices";`
- Check database name spelling (case-sensitive)

### Migration Issues

**Error: "A network-related or instance-specific error"**
- Ensure PostgreSQL service is running
- Check connection string format

**Error: "relation already exists"**
- Database already has tables from previous migrations
- Either drop database and recreate, or remove conflicting migrations

### Drop and Recreate Database (Fresh Start)

```bash
# Connect to PostgreSQL
psql -U postgres

# Drop database
DROP DATABASE "LanInvoices";

# Recreate database
CREATE DATABASE "LanInvoices";

# Exit and run migrations
\q
cd API
dotnet ef database update --project ../Persistence
```

## Connection String Format

PostgreSQL connection strings use different syntax than SQL Server:

```
# Basic connection
Host=localhost;Port=5432;Database=LanInvoices;Username=postgres;Password=yourpassword

# With SSL (recommended for production)
Host=your-server.com;Port=5432;Database=LanInvoices;Username=user;Password=pass;SSL Mode=Require

# With connection pooling (optional, good for performance)
Host=localhost;Port=5432;Database=LanInvoices;Username=postgres;Password=pass;Minimum Pool Size=5;Maximum Pool Size=100

# Unix socket (Linux/macOS local development)
Host=/var/run/postgresql;Port=5432;Database=LanInvoices;Username=postgres
```

## Performance Tips

1. **Connection Pooling**: Enabled by default in Npgsql, configure in connection string if needed
2. **Indexes**: PostgreSQL automatically creates indexes for primary keys and unique constraints
3. **Vacuum**: PostgreSQL auto-vacuum is enabled by default, no manual intervention needed
4. **Monitoring**: Use pgAdmin or `pg_stat_statements` extension for query analysis

## Backup and Restore

### Backup

```bash
# Backup entire database
pg_dump -U postgres -d LanInvoices -F c -f laninvoices_backup.dump

# Backup as SQL script
pg_dump -U postgres -d LanInvoices -f laninvoices_backup.sql
```

### Restore

```bash
# Restore from custom format
pg_restore -U postgres -d LanInvoices laninvoices_backup.dump

# Restore from SQL script
psql -U postgres -d LanInvoices -f laninvoices_backup.sql
```

## Docker Setup (Optional)

Run PostgreSQL in Docker for easy development:

```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: LanInvoices
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

```bash
# Start PostgreSQL
docker-compose up -d

# Run migrations
cd API
dotnet ef database update --project ../Persistence

# Stop PostgreSQL
docker-compose down
```

## Next Steps

1. ✅ PostgreSQL installed and running
2. ✅ Database created
3. ✅ Connection string configured
4. ✅ Migrations applied
5. ✅ API starts without errors

You're ready to use MokkilanInvoices with PostgreSQL!

## Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/)
