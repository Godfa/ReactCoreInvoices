# Tailscale Funnel Setup Guide

Complete guide for exposing MokkilanInvoices backend to Azure Static Web App using Tailscale Funnel.

## Architecture Overview

```
Azure Static Web App (Frontend)
           ↓ HTTPS (public internet)
Tailscale Funnel (public HTTPS endpoint)
           ↓ Tailscale VPN (encrypted)
Ubuntu Server (MokkilanInvoices Backend)
    localhost:5000
```

## Why Tailscale Funnel?

**Problem:** Azure Static Web App needs to connect to backend running on a friend's computer.

**Traditional Solutions:**
- ❌ Site-to-Site VPN - Static Web App is PaaS, can't join VPN
- ❌ Port Forwarding - Security risk, requires static IP
- ❌ Cloudflare Tunnel - Third-party dependency
- ❌ Azure VPN Gateway - Expensive (~€100/month)

**Tailscale Funnel Solution:**
- ✅ Free (Tailscale Free tier)
- ✅ No port forwarding required
- ✅ Automatic HTTPS with Let's Encrypt
- ✅ Backend stays on localhost (not exposed to internet)
- ✅ Public HTTPS endpoint (e.g., `https://backend.tailnet-xxxx.ts.net`)
- ✅ Works behind NAT

---

## Prerequisites

### Ubuntu Server Requirements:
- Ubuntu 20.04+ (or other Linux distribution)
- MokkilanInvoices Backend API running on port 5000
- Internet connection
- Sudo access

### Azure Requirements:
- Azure Static Web App deployed
- Frontend configured to call backend API

---

## Step 1: Install Tailscale on Ubuntu Server

```bash
# SSH to the server
ssh user@server

# Install Tailscale
curl -fsSL https://tailscale.com/install.sh | sh

# Start Tailscale and authenticate
sudo tailscale up

# Open the authentication URL in browser and sign in
# (Google, Microsoft, GitHub, or email)
```

**Save the machine name** displayed after authentication:
```
Success. You are now logged in as user@example.com.
Machine name: ubuntu-server
```

---

## Step 2: Verify Backend is Running

```bash
# Check that backend is running on port 5000
curl http://localhost:5000/health

# If not running, start it:
sudo systemctl start mokkilaninvoices
sudo systemctl status mokkilaninvoices

# Verify it's listening on port 5000
sudo netstat -tlnp | grep :5000
```

---

## Step 3: Enable Tailscale Funnel

### 3.1 Enable Funnel in Tailscale Admin Console

1. Go to https://login.tailscale.com/admin/settings/general
2. Scroll to **Funnel**
3. Click **Enable**

### 3.2 Start Funnel

```bash
# Get SSL certificate for your Tailscale hostname
sudo tailscale cert --domain $(tailscale status --json | jq -r '.Self.DNSName')

# Start Funnel on port 5000
sudo tailscale funnel 5000
```

**You should see:**
```
Available on the internet:

https://ubuntu-server.tailnet-abcd.ts.net/
|-- proxy http://127.0.0.1:5000

Funnel started and running in the background.
```

**Save this HTTPS URL!** Example: `https://ubuntu-server.tailnet-abcd.ts.net`

---

## Step 4: Test Funnel

```bash
# Test from the internet (run from another computer or phone)
curl https://ubuntu-server.tailnet-abcd.ts.net/health

# Or open in browser:
# https://ubuntu-server.tailnet-abcd.ts.net/health
```

Should return your backend's health check response.

---

## Step 5: Make Funnel Persistent (systemd service)

Funnel doesn't persist across reboots by default. Create a systemd service:

```bash
# Stop current Funnel
sudo tailscale funnel --bg=false off

# Create systemd service file
sudo nano /etc/systemd/system/tailscale-funnel.service
```

**Copy this content:**

```ini
[Unit]
Description=Tailscale Funnel for MokkilanInvoices API
After=network.target tailscaled.service
Requires=tailscaled.service

[Service]
Type=simple
User=root
ExecStart=/usr/bin/tailscale funnel --bg=false 5000
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

**Save:** `Ctrl+O`, `Enter`, `Ctrl+X`

```bash
# Reload systemd
sudo systemctl daemon-reload

# Start the service
sudo systemctl start tailscale-funnel

# Enable automatic start on boot
sudo systemctl enable tailscale-funnel

# Check status
sudo systemctl status tailscale-funnel
```

Should show: `active (running)`

---

## Step 6: Configure CORS in Backend

Update your MokkilanInvoices backend to allow requests from Azure Static Web App:

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowStaticWebApp", policy =>
    {
        policy.WithOrigins(
            "https://your-static-web-app.azurestaticapps.net",
            "http://localhost:3000"  // Local development
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// Before app.UseAuthorization();
app.UseCors("AllowStaticWebApp");
```

**Restart backend:**
```bash
sudo systemctl restart mokkilaninvoices
```

---

## Step 7: Configure Azure Static Web App

Update your frontend configuration to use the Tailscale Funnel URL:

### Option A: Environment Variables

```bash
# .env.production
REACT_APP_API_URL=https://ubuntu-server.tailnet-abcd.ts.net
```

### Option B: Configuration File

```typescript
// src/config.ts
export const API_BASE_URL =
  process.env.NODE_ENV === 'production'
    ? 'https://ubuntu-server.tailnet-abcd.ts.net'
    : 'http://localhost:5000';
```

### Usage in Frontend

```typescript
import axios from 'axios';
import { API_BASE_URL } from './config';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
});

// Make API calls
const response = await api.get('/api/receipts');
```

---

## Step 8: Deploy and Test

1. **Deploy frontend to Azure Static Web App** with updated configuration
2. **Test from browser:**
   - Open your Static Web App URL
   - Try features that call the backend API
3. **Monitor logs:**
   ```bash
   # Backend logs
   sudo journalctl -u mokkilaninvoices -f

   # Funnel logs
   sudo journalctl -u tailscale-funnel -f
   ```

---

## Security Recommendations

Since Funnel exposes your backend to the public internet, add authentication:

### 1. API Key Authentication

```csharp
// Program.cs - Add before routing
app.Use(async (context, next) =>
{
    var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
    var validKey = builder.Configuration["ApiKey"];

    if (apiKey != validKey)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    await next();
});
```

**Frontend:**
```typescript
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'X-API-Key': process.env.REACT_APP_API_KEY
  }
});
```

### 2. JWT Authentication

```csharp
// Use JWT tokens for authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
```

### 3. Rate Limiting

```csharp
// .NET 7+
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

app.UseRateLimiter();
```

### 4. IP Whitelisting (Optional)

If you know Azure Static Web App's outbound IP ranges:

```csharp
app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();
    var allowedIps = builder.Configuration
        .GetSection("AllowedIPs")
        .Get<string[]>();

    if (allowedIps?.Contains(remoteIp) == true)
    {
        await next();
    }
    else
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Forbidden");
    }
});
```

---

## Useful Commands

### Tailscale Commands

```bash
# Show Funnel status
sudo tailscale funnel status

# Show Tailscale status
tailscale status

# Show Tailscale IP and DNS name
tailscale ip -4
tailscale status --json | jq -r '.Self.DNSName'

# Restart Tailscale
sudo systemctl restart tailscaled

# Disconnect from Tailscale
sudo tailscale down

# Reconnect
sudo tailscale up
```

### Service Management

```bash
# Start Funnel
sudo systemctl start tailscale-funnel

# Stop Funnel
sudo systemctl stop tailscale-funnel

# Restart Funnel
sudo systemctl restart tailscale-funnel

# Check status
sudo systemctl status tailscale-funnel

# View logs (last 50 lines)
sudo journalctl -u tailscale-funnel -n 50

# Follow logs (real-time)
sudo journalctl -u tailscale-funnel -f

# Disable auto-start
sudo systemctl disable tailscale-funnel

# Enable auto-start
sudo systemctl enable tailscale-funnel
```

### Backend Service

```bash
# Check backend status
sudo systemctl status mokkilaninvoices

# Restart backend
sudo systemctl restart mokkilaninvoices

# View backend logs
sudo journalctl -u mokkilaninvoices -f
```

---

## Troubleshooting

### Problem: "funnel: node is not allowed to run funnel"

**Solution:** Enable Funnel in Tailscale Admin Console
1. Go to https://login.tailscale.com/admin/settings/general
2. Scroll to **Funnel**
3. Click **Enable**

### Problem: "connection refused"

```bash
# Check backend is running
curl http://localhost:5000/health
sudo systemctl status mokkilaninvoices

# Check port is listening
sudo netstat -tlnp | grep :5000

# Check Funnel status
sudo tailscale funnel status

# Restart services
sudo systemctl restart mokkilaninvoices
sudo systemctl restart tailscale-funnel
```

### Problem: CORS errors in browser

**Solution:** Add Static Web App origin to CORS policy (see Step 6)

**Verify CORS headers:**
```bash
curl -I -X OPTIONS \
  -H "Origin: https://your-static-web-app.azurestaticapps.net" \
  -H "Access-Control-Request-Method: GET" \
  https://ubuntu-server.tailnet-abcd.ts.net/api/receipts
```

Should return `Access-Control-Allow-Origin` header.

### Problem: Certificate errors

```bash
# Regenerate certificate
sudo tailscale cert --domain $(tailscale status --json | jq -r '.Self.DNSName')

# Restart Funnel
sudo systemctl restart tailscale-funnel
```

### Problem: Funnel not starting after reboot

```bash
# Check if systemd service is enabled
sudo systemctl is-enabled tailscale-funnel

# If not enabled:
sudo systemctl enable tailscale-funnel

# Check logs for errors
sudo journalctl -u tailscale-funnel -n 100
```

### Problem: Tailscale not connected after reboot

```bash
# Check Tailscale status
tailscale status

# If disconnected, reconnect:
sudo tailscale up

# Enable Tailscale auto-start
sudo systemctl enable tailscaled
```

---

## Custom Domain (Optional)

If you want to use your own domain instead of `tailnet-xxxx.ts.net`:

**Requirements:**
- Tailscale Pro or Enterprise plan ($20/month for first user)

**Setup:**
1. Go to Tailscale Admin Console → DNS → MagicDNS
2. Add custom domain
3. Update Funnel configuration:
   ```bash
   sudo tailscale funnel --https=443 --set-path=/ 5000
   ```

---

## Cost Breakdown

| Service | Cost |
|---------|------|
| Tailscale Free Tier | €0 |
| Ubuntu Server (existing) | €0 (already running) |
| Azure Static Web App | €0 (Free tier available) |
| **Total** | **€0/month** |

**Comparison with alternatives:**
- Azure VPN Gateway: ~€100-350/month
- ngrok Pro (static domain): ~€8/month
- Cloudflare Tunnel: €0 (but requires Cloudflare)

---

## Summary

✅ **Setup Complete!**

- Backend: `http://localhost:5000` (not exposed to internet)
- Public HTTPS: `https://ubuntu-server.tailnet-abcd.ts.net`
- Frontend calls public HTTPS endpoint
- No port forwarding required
- Automatic HTTPS with Let's Encrypt
- Free solution

**Architecture:**
```
User Browser
    ↓
Azure Static Web App (Frontend)
    ↓ HTTPS
Tailscale Funnel (Public Endpoint)
    ↓ Encrypted VPN
Ubuntu Server (Backend)
    localhost:5000
```

Your Azure Static Web App can now securely communicate with the backend running on a friend's computer without VPN configuration or port forwarding! 🚀

---

## Related Documentation

- [VPS Deployment Guide](VPS_DEPLOYMENT_GUIDE.md)
- [Tailscale Official Documentation](https://tailscale.com/kb/1223/funnel)

---

## Support

For issues or questions:
- Check Funnel logs: `sudo journalctl -u tailscale-funnel -f`
- Check backend logs: `sudo journalctl -u mokkilaninvoices -f`
- Tailscale community: https://tailscale.com/contact/support

Good luck with your deployment! 🚀
