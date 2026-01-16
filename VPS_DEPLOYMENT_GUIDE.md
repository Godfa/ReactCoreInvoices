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

### 3. Create Application Directory

```bash
# Create directory for the application
sudo mkdir -p </your/app/path>

# Create backup directory
sudo mkdir -p </your/backup/path>

# Create www-data user if it doesn't exist
sudo useradd -r -s /bin/false www-data || echo "www-data user already exists"

# Set ownership
sudo chown -R www-data:www-data </your/app/path>
```

### 4. Install Nginx (Reverse Proxy)

```bash
sudo apt install -y nginx

# Enable and start nginx
sudo systemctl enable nginx
sudo systemctl start nginx
```

### 5. Setup Firewall (UFW)

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

### 6. Install PostgreSQL (if needed)

```bash
sudo apt install -y postgresql postgresql-contrib

# Start and enable PostgreSQL
sudo systemctl enable postgresql
sudo systemctl start postgresql

# Create database and user
PW=$(tr -dc 'A-Za-z0-9' </dev/urandom | head -c20); \
sudo -u postgres psql -v pw="'$PW'" <<'EOF'
CREATE DATABASE mokkilaninvoices;
CREATE USER mokkilanadmin WITH PASSWORD :'pw';
GRANT ALL PRIVILEGES ON DATABASE mokkilaninvoices TO mokkilanadmin;
EOF
echo "Created user mokkiladmin with password: $PW"
```

---

## GitHub Secrets Configuration

### 1. Generate SSH Key Pair

On your local machine:

```bash
# Generate SSH key (do NOT set a passphrase for automation)
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/mokkilaninvoices-deploy

# This creates two files:
# ~/.ssh/mokkilaninvoices-deploy (private key)
# ~/.ssh/mokkilaninvoices-deploy.pub (public key)
```

### 2. Add Public Key to VPS

```bash
# Copy the public key to your VPS
ssh-copy-id -i ~/.ssh/mokkilaninvoices-deploy.pub <your-username>@<your-server-ip>

# Or manually:
# 1. Copy content of ~/.ssh/mokkilaninvoices-deploy.pub
# 2. SSH to your VPS
# 3. Add to ~/.ssh/authorized_keys
```

### 3. Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions → New repository secret

Add these secrets:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `VPS_HOST` | Your VPS IP address or domain | `123.456.789.0` or `api.yourdomain.com` |
| `VPS_USERNAME` | SSH username | `ubuntu` or `<your-username>` |
| `VPS_SSH_KEY` | Private SSH key content | Copy entire content of `~/.ssh/<your-deploy-key>` |
| `VPS_SSH_PORT` | SSH port (optional, defaults to 22) | `22` |
| `APP_DIR` | Application directory path on server | `</your/app/path>` |
| `BACKUP_DIR` | Backup directory path on server | `</your/backup/path>` |
| `SERVICE_NAME` | Systemd service name | `<your-app-name>` |
| `APP_URL` | Your application URL | `https://api.yourdomain.com` |

**To copy private key:**
```bash
# macOS/Linux
cat ~/.ssh/mokkilaninvoices-deploy | pbcopy

# Or just display it
cat ~/.ssh/mokkilaninvoices-deploy
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
User=www-data
Group=www-data

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

# Optional: Set connection strings
# Environment=ConnectionStrings__DefaultConnection=Host=localhost;Database=mokkilaninvoices;Username=mokkilanadmin;Password=<your-password>

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
sudo journalctl -u mokkilaninvoices -f
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
sudo journalctl -u mokkilaninvoices -n 100

# Follow logs (real-time)
sudo journalctl -u mokkilaninvoices -f

# View logs with timestamps
sudo journalctl -u mokkilaninvoices --since "1 hour ago"
```

---

## Nginx Reverse Proxy Setup

### 1. Create Nginx Configuration

```bash
sudo nano /etc/nginx/sites-available/mokkilaninvoices
```

Paste this configuration:

```nginx
# Upstream definition
upstream mokkilaninvoices_backend {
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
    access_log /var/log/nginx/mokkilaninvoices-access.log;
    error_log /var/log/nginx/mokkilaninvoices-error.log;

    # Client body size limit (for file uploads)
    client_max_body_size 10M;

    # Proxy settings
    location / {
        proxy_pass http://mokkilaninvoices_backend;
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
        proxy_pass http://mokkilaninvoices_backend/health;
        access_log off;
    }
}
```

### 2. Enable Site and Test Configuration

```bash
# Create symbolic link to enable site
sudo ln -s /etc/nginx/sites-available/mokkilaninvoices /etc/nginx/sites-enabled/

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
  -keyout /etc/ssl/private/mokkilaninvoices-selfsigned.key \
  -out /etc/ssl/certs/mokkilaninvoices-selfsigned.crt

# Update nginx config to use the certificate
# ssl_certificate /etc/ssl/certs/mokkilaninvoices-selfsigned.crt;
# ssl_certificate_key /etc/ssl/private/mokkilaninvoices-selfsigned.key;
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
sudo journalctl -u mokkilaninvoices -n 50
```

---

## Troubleshooting

### Service Won't Start

```bash
# Check service status
sudo systemctl status <your-app-name>

# View detailed logs
sudo journalctl -u mokkilaninvoices -n 100 --no-pager

# Check if port is already in use
sudo netstat -tlnp | grep :5000

# Check permissions
ls -la </your/app/path>
```

### GitHub Actions Deployment Fails

**SSH Connection Issues:**
```bash
# Test SSH connection manually
ssh -i ~/.ssh/mokkilaninvoices-deploy <your-username>@<your-server-ip>

# Check SSH key format (should be OpenSSH format)
head -n 1 ~/.ssh/mokkilaninvoices-deploy
# Should show: -----BEGIN OPENSSH PRIVATE KEY-----
```

**Permission Issues:**
```bash
# Fix ownership
sudo chown -R www-data:www-data </your/app/path>

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
sudo tail -f /var/log/nginx/mokkilaninvoices-error.log
```

**Test direct connection:**
```bash
# From VPS
curl http://localhost:5000/health

# From outside
curl http://<your-server-ip>:5000/health
```

### Database Connection Issues

```bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Test database connection
psql -h localhost -U mokkilanadmin -d mokkilaninvoices

# Check connection string in appsettings
cat </your/app/path>/appsettings.Production.json
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
tar -czf ../mokkilaninvoices.tar.gz .
cd ..

# Copy to server
scp mokkilaninvoices.tar.gz <your-username>@<your-server-ip>:/tmp/

# On server
ssh <your-username>@<your-server-ip>
sudo systemctl stop <your-app-name>
sudo tar -xzf /tmp/mokkilaninvoices.tar.gz -C </your/app/path>
sudo chown -R www-data:www-data </your/app/path>
sudo systemctl start <your-app-name>
```

---

## Monitoring and Maintenance

### Setup Log Rotation

```bash
sudo nano /etc/logrotate.d/mokkilaninvoices
```

```
/var/log/nginx/mokkilaninvoices-*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data adm
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

5. **Use environment-specific appsettings:**
   - Create `appsettings.Production.json` with production settings
   - Never commit secrets to git

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
- Check application logs: `sudo journalctl -u mokkilaninvoices -f`
- Check nginx logs: `sudo tail -f /var/log/nginx/mokkilaninvoices-error.log`
- Review GitHub Actions logs in repository

Good luck with your deployment! 🚀
