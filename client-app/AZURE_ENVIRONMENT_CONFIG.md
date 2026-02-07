# Azure Environment Configuration

## Overview

The application uses **build-time environment variable injection** to configure the API URL. The `VITE_API_URL` is injected during the GitHub Actions deployment process from GitHub Secrets, ensuring the URL is never hardcoded in the repository.

## How It Works

1. **GitHub Secret**: Store your API URL in GitHub repository secrets
2. **Build Time**: GitHub Actions injects the secret as an environment variable during build
3. **Vite**: Vite replaces `import.meta.env.VITE_API_URL` with the actual value in the built code
4. **Deployment**: The built application contains the correct API URL

## Setup Instructions

### 1. Add GitHub Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the following secrets:

   **Secret 1: API URL**
   - **Name**: `VITE_API_URL`
   - **Value**: Your API URL (e.g., `https://your-server.com/api`)

   **Secret 2: API Key** (if your backend requires it)
   - **Name**: `VITE_API_KEY`
   - **Value**: Your API authentication key

5. Click **Add secret** for each

### 2. Workflow Configuration

The GitHub Actions workflow (`.github/workflows/azure-static-web-apps-zealous-flower-06949bf03.yml`) is already configured to inject the environment variables:

```yaml
env:
  VITE_API_URL: ${{ secrets.VITE_API_URL }}
  VITE_API_KEY: ${{ secrets.VITE_API_KEY }}
```

### 3. Deploy

Push your changes to the `main` branch. The workflow will:
1. Checkout the code
2. Inject `VITE_API_URL` from GitHub secrets
3. Build the application with the injected URL
4. Deploy to Azure Static Web Apps

## Local Development

For local development, the application falls back to `http://localhost:5000/api` if `VITE_API_URL` is not set.

You can create a `.env.local` file (not committed to git) for local testing:

```env
VITE_API_URL=http://localhost:5000/api
VITE_API_KEY=your-local-api-key
```

> [!IMPORTANT]
> Never commit `.env.local` or `.env.production` to version control. These files should be in `.gitignore`.

## Benefits

✅ **No secrets in repository** - API URL and API Key stored securely in GitHub Secrets  
✅ **Build-time injection** - Values baked into the build, no runtime overhead  
✅ **Easy updates** - Change secrets in GitHub, redeploy automatically  
✅ **Environment-specific** - Different secrets for different environments  
✅ **Secure** - Secrets never exposed in source code or version control  
✅ **API Key protection** - Authentication credentials never committed to git

## Troubleshooting

**Problem**: Application can't connect to API after deployment

**Solution**: 
1. Verify the GitHub secret `VITE_API_URL` is set correctly
2. Check the workflow run logs to confirm the environment variable was injected
3. Ensure your backend server allows CORS from your Azure Static Web App domain

**Problem**: Local development not working

**Solution**: 
- The app falls back to `http://localhost:5000/api` by default
- Or create `.env.local` with your local API URL
