# MokkilanInvoices VPS Deployment Guide

Complete guide for deploying MokkilanInvoices Backend API to your own Linux VPS using GitHub Actions.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Server Setup](#server-setup)
3. [GitHub Secrets Configuration](#github-secrets-configuration)
4. [Systemd Service Setup](#systemd-service-setup)
5. [Nginx Reverse Proxy Setup](#nginx-reverse-proxy-setup)
6. [First Deployment](#first-deployment)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Your VPS should have:
- Ubuntu 24.04 LTS (recommended) or Ubuntu 22.04+ or Debian 11+ (or similar Linux distribution)
- Root or sudo access
- Public IP address or domain name
- SSH access enabled
- At least 1GB RAM and 10GB disk space

### Required software on VPS:
- .NET 8 Runtime
- Nginx (for reverse proxy)
- PostgreSQL (if using database)

---

## Server Setup

### 1. Connect to Your VPS

```bash
ssh <your-username>@<your-server-ip>
```

### 2. Install .NET 8 Runtime

```bash
# Add Microsoft package repository (Ubuntu 24.04 LTS)
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package list
sudo apt update

# Install .NET Runtime (not SDK, we only need runtime on server)
sudo apt install -y aspnetcore-runtime-8.0

# Verify installation
dotnet --version
```

**Note:** Ubuntu 24.04 LTS is recommended as it has extended support until 2029 (compared to Ubuntu 22.04 LTS which ends support in 2027).

For other Linux distributions, see: https://learn.microsoft.com/en-us/dotnet/core/install/linux

### 3. Create Application and Deployment Users

Create dedicated users for running the application and for deployments:

```bash
# Create application user (runs the service)
sudo useradd -r -s /bin/false invoicer-deploy

# Create deployment user (used by GitHub Actions)
sudo useradd -m -s /bin/bash deploy

# Add deploy user to invoicer-deploy group (for file access)
sudo usermod -a -G invoicer-deploy deploy

# Create .ssh directory for deploy user
sudo -u deploy mkdir -p /home/deploy/.ssh
sudo -u deploy chmod 700 /home/deploy/.ssh
sudo -u deploy touch /home/deploy/.ssh/authorized_keys
sudo -u deploy chmod 600 /home/deploy/.ssh/authorized_keys
```

### 4. Configure Sudo Access for Deployment User

The deployment user needs limited sudo access to restart the service:

```bash
# Create sudoers file for deploy user
sudo nano /etc/sudoers.d/deploy
```

Add this content (replace `<your-app-name>` with your service name):

```
# Allow deploy user to manage the application service without password
deploy ALL=(ALL) NOPASSWD: /bin/systemctl start <your-app-name>
deploy ALL=(ALL) NOPASSWD: /bin/systemctl stop <your-app-name>
deploy ALL=(ALL) NOPASSWD: /bin/systemctl restart <your-app-name>
deploy ALL=(ALL) NOPASSWD: /bin/systemctl status <your-app-name>
deploy ALL=(ALL) NOPASSWD: /bin/systemctl daemon-reload
deploy ALL=(ALL) NOPASSWD: /bin/chown -R invoicer-deploy\:invoicer-deploy *
```

Set correct permissions:

```bash
sudo chmod 440 /etc/sudoers.d/deploy

# Verify syntax
sudo visudo -c
```

### 5. Create Application Directory

```bash
# Create directory for the application
sudo mkdir -p </your/app/path>

# Create backup directory
sudo mkdir -p </your/backup/path>

# Set ownership to invoicer-deploy (app runs as invoicer-deploy)
sudo chown -R invoicer-deploy:invoicer-deploy </your/app/path>
sudo chown -R invoicer-deploy:invoicer-deploy </your/backup/path>

# Give deploy user write access to app directory (deploy is in invoicer-deploy group)
sudo chmod -R 775 </your/app/path>
sudo chmod -R 775 </your/backup/path>
```

### 6. Install Nginx (Reverse Proxy)

```bash
sudo apt install -y nginx

# Enable and start nginx
sudo systemctl enable nginx
sudo systemctl start nginx
```

### 7. Setup Firewall (UFW)

```bash
# Allow SSH (IMPORTANT: Do this first!)
sudo ufw allow 22/tcp

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Enable firewall
sudo ufw enable

# Check status
sudo ufw status
```

### 8. Install PostgreSQL (if needed)

```bash
sudo apt install -y postgresql postgresql-contrib

# Start and enable PostgreSQL
sudo systemctl enable postgresql
sudo systemctl start postgresql

# Create database and user with proper permissions
PW=$(tr -dc 'A-Za-z0-9' </dev/urandom | head -c20); \
sudo -u postgres psql -v pw="'$PW'" <<'EOF'
CREATE DATABASE <your-db-name>;
CREATE USER <your-db-user> WITH PASSWORD :'pw';

-- Grant database privileges
GRANT ALL PRIVILEGES ON DATABASE <your-db-name> TO <your-db-user>;

-- Connect to the database to grant schema permissions
\c <your-db-name>

-- Grant schema permissions (required for Entity Framework migrations)
GRANT ALL ON SCHEMA public TO <your-db-user>;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO <your-db-user>;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO <your-db-user>;

-- Set default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO <your-db-user>;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO <your-db-user>;
EOF
echo "Created user <your-db-user> with password: $PW"
echo "IMPORTANT: Save this password securely - you'll need it for the connection string"
```

**Note:** The schema permissions (`GRANT ALL ON SCHEMA public`) are essential for Entity Framework Core migrations to work properly. Without these, your application won't be able to create or modify database tables.

**Testing the connection:**
```bash
# Test the database connection with the new user
psql -U <your-db-user> -d <your-db-name> -h localhost
# Enter the password when prompted
# If successful, you'll see the PostgreSQL prompt
```

---

## GitHub Secrets Configuration

### 1. Generate SSH Key Pair

On your local machine:

```bash
# Generate SSH key (do NOT set a passphrase for automation)
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/<your-app-name>-deploy

# This creates two files:
# ~/.ssh/<your-app-name>-deploy (private key)
# ~/.ssh/<your-app-name>-deploy.pub (public key)
```

### 2. Add Public Key to VPS

Add the public key to the **deploy** user (not your personal user):

```bash
# Display your public key
cat ~/.ssh/<your-app-name>-deploy.pub

# Copy the output, then SSH to your VPS as your admin user
ssh <your-admin-username>@<your-server-ip>

# Add the public key to deploy user's authorized_keys
# Paste the public key when prompted
sudo bash -c 'cat >> /home/deploy/.ssh/authorized_keys'
# Press Ctrl+D after pasting

# Verify the key was added
sudo cat /home/deploy/.ssh/authorized_keys

# Test SSH connection as deploy user (from your local machine)
ssh -i ~/.ssh/<your-app-name>-deploy deploy@<your-server-ip>
```

**Alternative method using ssh-copy-id:**
```bash
# From your local machine
ssh-copy-id -i ~/.ssh/<your-app-name>-deploy.pub deploy@<your-server-ip>
```

### 3. Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions → New repository secret

Add these secrets:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `VPS_HOST` | Your VPS IP address or domain | `123.456.789.0` or `api.yourdomain.com` |
| `VPS_USERNAME` | SSH username (use `deploy` user) | `deploy` |
| `VPS_SSH_KEY` | Private SSH key content | Copy entire content of `~/.ssh/<your-app-name>-deploy` |
| `VPS_SSH_PORT` | SSH port (optional, defaults to 22) | `22` |
| `APP_DIR` | Application directory path on server | `</your/app/path>` |
| `BACKUP_DIR` | Backup directory path on server | `</your/backup/path>` |
| `SERVICE_NAME` | Systemd service name | `<your-app-name>` |
| `APP_URL` | Your application URL | `https://api.yourdomain.com` |

**Important:** Use the `deploy` user for `VPS_USERNAME`, not your personal admin user.

**To copy private key:**
```bash
# macOS/Linux
cat ~/.ssh/<your-app-name>-deploy | pbcopy

# Or just display it
cat ~/.ssh/<your-app-name>-deploy
```

Copy the entire output including `-----BEGIN OPENSSH PRIVATE KEY-----` and `-----END OPENSSH PRIVATE KEY-----`

### 4. Optional: Database Secrets

Add these if your application needs database connection:

| Secret Name | Description |
|-------------|-------------|
| `DB_CONNECTION_STRING` | PostgreSQL connection string |
| `DB_PASSWORD` | Database password |

---

## Systemd Service Setup

### 1. Create Systemd Service File

SSH to your VPS and create the service file:

```bash
sudo nano /etc/systemd/system/<your-app-name>.service
```

Paste this configuration:

```ini
[Unit]
Description=MokkilanInvoices Backend API
After=network.target

[Service]
Type=notify
# User and Group
User=invoicer-deploy
Group=invoicer-deploy

# Working directory and executable
WorkingDirectory=</your/app/path>
ExecStart=/usr/bin/dotnet </your/app/path>/API.dll

# Restart policy
Restart=always
RestartSec=10
KillSignal=SIGINT

# Environment variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5000

# Database connection string (REQUIRED if using PostgreSQL)
# IMPORTANT: Never commit this file with real passwords to git
# The double underscore (__) is required for ASP.NET Core to read nested config values
Environment=ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=<your-db-name>;Username=<your-db-user>;Password=<your-password>

# Logging
SyslogIdentifier=<your-app-name>
StandardOutput=journal
StandardError=journal

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=</your/app/path>
ReadWritePaths=/var/log

[Install]
WantedBy=multi-user.target
```

### 2. Enable and Start Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable <your-app-name>

# Start the service
sudo systemctl start <your-app-name>

# Check status
sudo systemctl status <your-app-name>

# View logs
sudo journalctl -u <your-app-name> -f
```

### Common Service Commands

```bash
# Start
sudo systemctl start <your-app-name>

# Stop
sudo systemctl stop <your-app-name>

# Restart
sudo systemctl restart <your-app-name>

# View status
sudo systemctl status <your-app-name>

# View logs (last 100 lines)
sudo journalctl -u <your-app-name> -n 100

# Follow logs (real-time)
sudo journalctl -u <your-app-name> -f

# View logs with timestamps
sudo journalctl -u <your-app-name> --since "1 hour ago"
```

---

## Nginx Reverse Proxy Setup

### 1. Create Nginx Configuration

```bash
sudo nano /etc/nginx/sites-available/<your-app-name>
```

Paste this configuration:

```nginx
# Upstream definition
upstream <your-app-name>_backend {
    server localhost:5000;
    keepalive 32;
}

# HTTP Server (redirect to HTTPS)
server {
    listen 80;
    listen [::]:80;
    server_name api.yourdomain.com;  # Change to your domain

    # Redirect all HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

# HTTPS Server
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name api.yourdomain.com;  # Change to your domain

    # SSL Configuration (will be configured by Certbot)
    # ssl_certificate /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/api.yourdomain.com/privkey.pem;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Logging
    access_log /var/log/nginx/<your-app-name>-access.log;
    error_log /var/log/nginx/<your-app-name>-error.log;

    # Client body size limit (for file uploads)
    client_max_body_size 10M;

    # Proxy settings
    location / {
        proxy_pass http://<your-app-name>_backend;
        proxy_http_version 1.1;

        # Headers
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $server_name;

        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;

        # Buffering
        proxy_buffering off;
        proxy_cache_bypass $http_upgrade;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://<your-app-name>_backend/health;
        access_log off;
    }
}
```

### 2. Enable Site and Test Configuration

```bash
# Create symbolic link to enable site
sudo ln -s /etc/nginx/sites-available/<your-app-name> /etc/nginx/sites-enabled/

# Test nginx configuration
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx
```

### 3. Setup SSL with Let's Encrypt (Recommended)

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obtain SSL certificate (replace with your domain)
sudo certbot --nginx -d api.yourdomain.com

# Follow the prompts:
# - Enter email address
# - Agree to terms
# - Choose whether to redirect HTTP to HTTPS (recommended: yes)

# Certbot will automatically:
# - Obtain certificate
# - Update nginx configuration
# - Setup auto-renewal

# Test auto-renewal
sudo certbot renew --dry-run
```

### 4. Alternative: Self-Signed Certificate (Development Only)

```bash
# Generate self-signed certificate
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/<your-app-name>-selfsigned.key \
  -out /etc/ssl/certs/<your-app-name>-selfsigned.crt

# Update nginx config to use the certificate
# ssl_certificate /etc/ssl/certs/<your-app-name>-selfsigned.crt;
# ssl_certificate_key /etc/ssl/private/<your-app-name>-selfsigned.key;
```

---

## First Deployment

### 1. Push to Main Branch

```bash
cd /path/to/MokkilanInvoices
git add .
git commit -m "Add VPS deployment workflow"
git push origin main
```

### 2. Monitor GitHub Actions

1. Go to your repository on GitHub
2. Click "Actions" tab
3. Watch the workflow execution
4. Check for any errors

### 3. Verify Deployment

```bash
# Check if service is running
curl http://<your-server-ip>:5000/health

# Or with domain
curl https://api.yourdomain.com/health

# Check service status on VPS
ssh <your-username>@<your-server-ip>
sudo systemctl status <your-app-name>
sudo journalctl -u <your-app-name> -n 50
```

---

## Troubleshooting

### Service Won't Start

```bash
# Check service status
sudo systemctl status <your-app-name>

# View detailed logs
sudo journalctl -u <your-app-name> -n 100 --no-pager

# Check if port is already in use
sudo netstat -tlnp | grep :5000

# Check permissions
ls -la </your/app/path>
```

### GitHub Actions Deployment Fails

**SSH Connection Issues:**
```bash
# Test SSH connection manually as deploy user
ssh -i ~/.ssh/<your-app-name>-deploy deploy@<your-server-ip>

# Check SSH key format (should be OpenSSH format)
head -n 1 ~/.ssh/<your-app-name>-deploy
# Should show: -----BEGIN OPENSSH PRIVATE KEY-----

# Check if public key is in deploy user's authorized_keys
ssh <your-admin-user>@<your-server-ip>
sudo cat /home/deploy/.ssh/authorized_keys

# Check deploy user's sudo permissions
ssh -i ~/.ssh/<your-app-name>-deploy deploy@<your-server-ip>
sudo -l
# Should show the allowed systemctl commands
```

**Permission Issues:**
```bash
# Fix ownership
sudo chown -R invoicer-deploy:invoicer-deploy </your/app/path>

# Fix executable permissions
sudo chmod +x </your/app/path>/API
```

### Application Not Accessible

**Check if service is running:**
```bash
sudo systemctl status <your-app-name>
```

**Check if nginx is running:**
```bash
sudo systemctl status nginx
```

**Check nginx logs:**
```bash
sudo tail -f /var/log/nginx/<your-app-name>-error.log
```

**Test direct connection:**
```bash
# From VPS
curl http://localhost:5000/health

# From outside
curl http://<your-server-ip>:5000/health
```

### Database Connection Issues

**Check PostgreSQL is running:**
```bash
sudo systemctl status postgresql
```

**Test database connection:**
```bash
# Test connection (use -h localhost to force TCP connection)
psql -U <your-db-user> -d <your-db-name> -h localhost

# Check connection string in systemd service
sudo systemctl cat <your-app-name> | grep ConnectionStrings
```

**Common Error: "permission denied for schema public"**

If you see this error in logs (`sudo journalctl -u <your-app-name> -n 100`), the database user lacks schema permissions:

```bash
# Fix permissions as postgres user
sudo -u postgres psql -d <your-db-name> <<'EOF'
-- Grant schema permissions
GRANT ALL ON SCHEMA public TO <your-db-user>;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO <your-db-user>;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO <your-db-user>;

-- Set default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO <your-db-user>;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO <your-db-user>;
EOF

# Restart the application
sudo systemctl restart <your-app-name>
```

**Common Error: "Peer authentication failed"**

If you see this error when testing connection, you're using Unix socket instead of TCP:

```bash
# Wrong (uses Unix socket)
psql -U <your-db-user> -d <your-db-name>

# Correct (uses TCP with password)
psql -U <your-db-user> -d <your-db-name> -h localhost
```

### High Memory Usage

```bash
# Check memory usage
free -h

# Check process memory
top -p $(pgrep -f "dotnet.*API.dll")

# Restart service if needed
sudo systemctl restart <your-app-name>
```

---

## Manual Deployment (Alternative)

If you prefer manual deployment without GitHub Actions:

```bash
# On your local machine
cd /path/to/MokkilanInvoices
dotnet publish API/API.csproj -c Release -o ./publish

# Create tarball
cd publish
tar -czf ../<your-app-name>.tar.gz .
cd ..

# Copy to server
scp <your-app-name>.tar.gz <your-username>@<your-server-ip>:/tmp/

# On server
ssh <your-username>@<your-server-ip>
sudo systemctl stop <your-app-name>
sudo tar -xzf /tmp/<your-app-name>.tar.gz -C </your/app/path>
sudo chown -R invoicer-deploy:invoicer-deploy </your/app/path>
sudo systemctl start <your-app-name>
```

---

## Monitoring and Maintenance

### Setup Log Rotation

```bash
sudo nano /etc/logrotate.d/<your-app-name>
```

```
/var/log/nginx/<your-app-name>-*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 invoicer-deploy adm
    sharedscripts
    postrotate
        systemctl reload nginx > /dev/null 2>&1
    endscript
}
```

### Setup Monitoring (Optional)

Consider installing monitoring tools:
- **Netdata** - Real-time monitoring dashboard
- **Prometheus + Grafana** - Metrics collection and visualization
- **Uptime Kuma** - Uptime monitoring

---

## Security Best Practices

1. **Keep system updated:**
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

2. **Disable SSH password authentication:**
   ```bash
   sudo nano /etc/ssh/sshd_config
   # Set: PasswordAuthentication no
   sudo systemctl restart sshd
   ```

3. **Setup fail2ban:**
   ```bash
   sudo apt install fail2ban
   sudo systemctl enable fail2ban
   ```

4. **Regular backups:**
   - Database backups
   - Application backups (already handled by deployment script)
   - Server snapshots

5. **Protect database credentials and secrets:**
   - **Never** commit database passwords or connection strings to git
   - Store connection strings in systemd service environment variables (not in appsettings files)
   - Create `appsettings.Production.json` with production settings (without secrets)
   - Use `.gitignore` to exclude files containing secrets
   - Rotate database passwords periodically

6. **Deployment user security:**
   - The `deploy` user has limited sudo access only for service management
   - Never allow the `deploy` user to run arbitrary commands with sudo
   - Regularly audit `/etc/sudoers.d/deploy` for unauthorized changes
   - Consider using SSH key rotation for additional security

---

## API Key Authentication

When exposing your backend API to external clients (e.g., Azure Static Web App), use API key authentication to secure access.

### Step 1: Generate API Key

On your VPS or local machine, generate a secure API key:

```bash
# Generate a random 32-character API key
openssl rand -base64 32

# Example output: K7xP2mN9qR4wL8vT3yU6zA1bC5dE0fG=
# SAVE THIS KEY! You'll need it for both backend and frontend
```

**Important:** Use the SAME key for both backend and frontend!

---

### Step 2: Configure Backend (VPS)

SSH to your VPS and add the API key to your systemd service:

```bash
# Edit the systemd service file
sudo nano /etc/systemd/system/<your-app-name>.service
```

Add the `ApiKey` environment variable in the `[Service]` section:

```ini
[Service]
# ... existing settings ...
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=ConnectionStrings__DefaultConnection=Host=localhost;...

# ADD THIS LINE with your generated API key:
Environment=ApiKey=K7xP2mN9qR4wL8vT3yU6zA1bC5dE0fG=
```

Save and reload:

```bash
# Save: Ctrl+O, Enter, Ctrl+X

# Reload systemd configuration
sudo systemctl daemon-reload

# Restart the application
sudo systemctl restart <your-app-name>

# Verify it's running
sudo systemctl status <your-app-name>
```

---

### Step 3: Configure Frontend (Azure Static Web App)

#### Option A: Azure Portal (Recommended for Production)

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your **Static Web App**
3. Go to **Settings** → **Configuration**
4. Click **+ Add**
5. Add the following application settings:

| Name | Value |
|------|-------|
| `VITE_API_URL` | `https://api.yourdomain.com/api` |
| `VITE_API_KEY` | `K7xP2mN9qR4wL8vT3yU6zA1bC5dE0fG=` |

6. Click **Save**
7. Your app will automatically redeploy with the new settings

#### Option B: Local Development (.env file)

For local development, create or update `.env.development`:

```bash
# client-app/.env.development
VITE_API_URL=http://localhost:5000/api
# No API key needed for local development (middleware skips if not configured)
```

For production builds, create `.env.production`:

```bash
# client-app/.env.production
VITE_API_URL=https://api.yourdomain.com/api
VITE_API_KEY=K7xP2mN9qR4wL8vT3yU6zA1bC5dE0fG=
```

**Important:** Never commit `.env.production` to git! Add it to `.gitignore`.

---

### Step 4: Verify API Key Authentication

Test from your local machine or any computer:

```bash
# Test WITHOUT API key (should return 401 Unauthorized)
curl -I https://api.yourdomain.com/api/invoices

# Expected response:
# HTTP/2 401
# {"error":"API key is required"}

# Test WITH API key (should return 200 OK or redirect to login)
curl -H "X-API-Key: K7xP2mN9qR4wL8vT3yU6zA1bC5dE0fG=" \
     https://api.yourdomain.com/api/invoices

# Test health endpoint (should work WITHOUT API key)
curl https://api.yourdomain.com/health

# Expected response:
# {"status":"healthy","timestamp":"2024-01-15T12:00:00Z"}
```

---

### Step 5: Test Full Flow

1. **Open your Static Web App** in a browser
2. **Open Developer Tools** (F12) → Network tab
3. **Try to login or access any feature**
4. **Check the request headers** - you should see:
   - `X-API-Key: K7xP2mN9qR4wL8vT3yU6zA1bC5dE0fG=`
   - `Authorization: Bearer <jwt-token>` (after login)

---

### Troubleshooting

#### "API key is required" error
- Check that `VITE_API_KEY` is set in Azure Static Web App Configuration
- Redeploy the frontend after adding environment variables
- Clear browser cache and try again

#### "Invalid API key" error
- Verify the API key matches EXACTLY (copy-paste, no extra spaces)
- Check backend logs: `sudo journalctl -u <your-app-name> -f`

#### API key not being sent
- Check browser DevTools → Network → Request Headers
- Verify `agent.ts` includes the API key interceptor
- Rebuild and redeploy frontend

#### Local development issues
- API key is optional in development (middleware skips if not configured)
- Don't set `VITE_API_KEY` in `.env.development` for easier local testing

---

### Security Notes

1. **HTTPS Required**: API key is only secure over HTTPS (encrypted in transit)
2. **Frontend Visibility**: API key is visible in browser DevTools - this is expected
3. **Defense in Depth**: API key + JWT + CORS together provide security
4. **Key Rotation**: Change API key periodically by updating both backend and frontend

---

### CORS Configuration

Ensure CORS is configured to allow your frontend and the `X-API-Key` header:

```csharp
// Program.cs or ApplicationServiceExtensions.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
            "https://your-static-web-app.azurestaticapps.net",
            "http://localhost:3000",  // Local development
            "http://localhost:5173"   // Vite dev server
        )
        .AllowAnyMethod()
        .AllowAnyHeader()  // This allows X-API-Key header
        .AllowCredentials();
    });
});
```

---

### Rate Limiting (Optional but Recommended)

Add rate limiting to prevent abuse:

```csharp
// Program.cs (.NET 7+)
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = 429;
});

// Add after app.UseCors()
app.UseRateLimiter();
```

---

## Next Steps

- [ ] Setup monitoring and alerting
- [ ] Configure automated database backups
- [ ] Setup CI/CD for staging environment
- [ ] Add health check endpoints
- [ ] Configure HTTPS with Let's Encrypt
- [ ] Setup log aggregation
- [ ] Add application performance monitoring (APM)

---

## Support

For issues or questions:
- Check application logs: `sudo journalctl -u <your-app-name> -f`
- Check nginx logs: `sudo tail -f /var/log/nginx/<your-app-name>-error.log`
- Review GitHub Actions logs in repository

Good luck with your deployment! 🚀
