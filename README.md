# 🧠 Simple RAG Pipeline using Azure & .NET

This project walks through how to build a simple Retrieval-Augmented Generation (RAG) pipeline using:

- Azure Blob Storage
- Azure AI Search (Cognitive Search)
- Azure OpenAI (GPT-4)
- .NET 6 Console App or API

### Architecture Overview

![Simple RAG Pipeline Diagram](./Simple%20Rag%20Pipeline%20Diagram.png)

---

## 🔧 What You'll Build

- Upload documents to Azure Blob
- Index them using Azure Cognitive Search
- Retrieve relevant chunks via .NET
- Pass them to Azure OpenAI for a GPT-4 generated response

---

## 📦 Prerequisites & Requirements

### 🔧 Development Environment
- **.NET 9 SDK** - Download from [Microsoft .NET](https://dotnet.microsoft.com/download)
- **Visual Studio Code** or **Visual Studio 2022** (recommended for development)
- **PowerShell 7+** (for running Azure provisioning scripts)
- **Git** (for cloning the repository)

### ☁️ Azure Requirements
- **Azure Subscription** with sufficient permissions to create resources
- **Azure CLI** - Download from [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- **Azure OpenAI Service Access** - Request access at [Azure OpenAI](https://aka.ms/oai/access)
- **Azure Cognitive Search** service availability in your region
- **Azure Blob Storage** service availability in your region

### 🔑 Required Azure Services & Permissions
Your Azure subscription must have access to:
- **Azure OpenAI Service** (with GPT-4 or GPT-4o deployment)
- **Azure Cognitive Search** (Basic tier or higher recommended)
- **Azure Blob Storage** (Standard LRS or higher)
- **Resource Groups** (Contributor access to create and manage resources)

### 🌍 Regional Considerations
- Ensure Azure OpenAI is available in your chosen region
- Verify Azure Cognitive Search availability in the same region
- Consider data residency requirements for your organization

### 💻 Local Development Setup
1. **Clone the repository**:
   ```bash
   git clone https://github.com/justintindle/simple-azue-rag-pipeline-2025.git
   cd simple-azue-rag-pipeline-2025
   ```

2. **Verify .NET installation**:
   ```bash
   dotnet --version  # Should show 9.0 or later
   ```

3. **Install required packages** (automatic with first build):
   - DotNetEnv (3.1.1) - for environment variable loading
   - Microsoft.AspNetCore.OpenApi (9.0.7) - for API documentation
   - Swashbuckle.AspNetCore (9.0.3) - for Swagger UI

### 🔐 Environment Configuration
Before running the RagAPI, you need to configure environment variables. The application looks for a `.env` file in the `src/RagApi/` directory with the following variables:

```env
# Azure OpenAI Configuration
AZURE_OPENAI_API_KEY=your-openai-api-key
AZURE_OPENAI_ENDPOINT=https://your-region.api.cognitive.microsoft.com/
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_API_VERSION=2025-01-01-preview

# Azure Cognitive Search Configuration
AZURE_SEARCH_SERVICE_NAME=your-search-service-name
AZURE_SEARCH_API_KEY=your-search-api-key
AZURE_SEARCH_INDEX_NAME=rag-index
AZURE_SEARCH_API_VERSION=2023-07-01-Preview

# Azure Blob Storage Configuration
AZURE_STORAGE_ACCOUNT_NAME=your-storage-account-name
AZURE_STORAGE_ACCOUNT_KEY=your-storage-account-key
AZURE_STORAGE_CONTAINER_NAME=documents

# Azure General Configuration
AZURE_REGION=eastus
AZURE_TENANT_ID=your-tenant-id
AZURE_SUBSCRIPTION_ID=your-subscription-id
```

### 🚨 Important Notes
- **Never commit the .env file** to version control (it's in .gitignore)
- **Create your own .env file** after provisioning Azure resources
- **Use the provided PowerShell script** to automatically provision and configure Azure resources
- **Ensure your Azure account has sufficient quota** for the required services

### 🔍 Troubleshooting Prerequisites
- **Azure CLI Login**: Run `az login` and ensure you're logged into the correct subscription
- **Subscription Access**: Verify with `az account show` that you have the correct subscription selected
- **Resource Quotas**: Check Azure portal for any quota limitations in your chosen region
- **Service Availability**: Confirm all required services are available in your target Azure region

---

## 🚀 Quickstart

```bash
### Step 1: Provision Azure Resources
```bash
# Navigate to the Azure setup directory
cd azure-setup

# Run the provisioning script (requires Azure CLI and PowerShell)
./create-resources.ps1

# This script will:
# - Create a Resource Group
# - Provision Azure OpenAI Service with GPT-4 deployment
# - Create Azure Cognitive Search service
# - Set up Azure Blob Storage
# - Generate a .env file with all required configuration
```

### Step 2: Configure Environment Variables
```bash
# The create-resources.ps1 script automatically creates a .env file
# Verify the .env file exists in src/RagApi/.env
# If you need to create it manually, copy the template from the prerequisites section above
```

### Step 3: Upload Sample Documents (Optional)
```bash
# Upload the provided sample documents to test the pipeline
# Sample PDFs are included in the sample-data/ directory
# The provisioning script can optionally upload these for you
```

### Step 4: Run the RagAPI
```bash
# Navigate to the API project
cd src/RagApi

# Restore dependencies and build the project
dotnet restore
dotnet build

# Run the API (will start on http://localhost:5000)
dotnet run

# The API will be available at:
# - http://localhost:5000/swagger (Swagger UI for testing)
# - http://localhost:5000/api/rag (RAG endpoint)
```

### Step 5: Test the API
```bash
# Option 1: Use Swagger UI
# Navigate to http://localhost:5000/swagger in your browser
# Use the interactive interface to test the /api/rag endpoint

# Option 2: Use cURL
curl -X POST "http://localhost:5000/api/rag" 
     -H "Content-Type: application/json" 
     -d '{"question": "What is the remote work policy?"}'

# Option 3: Run the Console App
cd ../RagConsole
dotnet run
# Follow the interactive prompts to ask questions
```

### 🔧 Alternative: Manual Configuration
If you prefer to set up Azure resources manually:

1. **Create Azure Resources** in the Azure Portal:
   - Resource Group
   - Azure OpenAI Service (with GPT-4 deployment)
   - Azure Cognitive Search Service
   - Azure Blob Storage Account

2. **Create Search Index** using the provided schema:
   ```bash
   # Use the search-index.json file in azure-setup/
   # Import this schema into your Azure Cognitive Search service
   ```

3. **Configure .env file** with your resource details

4. **Upload documents** to your Blob Storage container

5. **Index documents** in Azure Cognitive Search

---

## 🚨 Troubleshooting

### Common Issues and Solutions

#### ❌ "Azure CLI not found" Error
**Problem**: PowerShell script fails with Azure CLI error
**Solution**: 
- Install Azure CLI from [Microsoft Docs](https://docs.microsoft.com/cli/azure/install-azure-cli)
- Restart your terminal after installation
- Run `az login` to authenticate

#### ❌ ".env file not found" Error
**Problem**: RagAPI can't find environment variables
**Solution**:
- Ensure `.env` file exists in `src/RagApi/.env`
- Check that the file contains all required variables
- Verify file is not named `.env.txt` (common Windows issue)

#### ❌ "Unauthorized" or "403 Forbidden" Errors
**Problem**: Authentication issues with Azure services
**Solution**:
- Verify API keys are correct in `.env` file
- Check that Azure OpenAI deployment name matches your actual deployment
- Ensure Azure Cognitive Search service is running

#### ❌ "No search results" or Empty Responses
**Problem**: Search index is empty or not configured properly
**Solution**:
- Upload documents to your Blob Storage container
- Run the indexing process in Azure Cognitive Search
- Verify the search index schema matches the provided `search-index.json`

#### ❌ Port Already in Use (5000)
**Problem**: Another application is using port 5000
**Solution**:
```bash
# Run on a different port
dotnet run --urls "http://localhost:5001"

# Or kill the process using port 5000
# Windows:
netstat -ano | findstr :5000
taskkill /PID <process_id> /F

# macOS/Linux:
lsof -ti:5000 | xargs kill
```

#### ❌ "Model deployment not found" Error
**Problem**: Azure OpenAI deployment name is incorrect
**Solution**:
- Check your Azure OpenAI resource in the Azure portal
- Verify the exact deployment name in the "Deployments" section
- Update `AZURE_OPENAI_DEPLOYMENT` in your `.env` file

#### ❌ API Version Compatibility Issues
**Problem**: Using outdated API versions
**Solution**:
- Update API versions in `.env` file:
  - `AZURE_OPENAI_API_VERSION=2025-01-01-preview`
  - `AZURE_SEARCH_API_VERSION=2023-07-01-Preview`
- Check Azure documentation for latest supported versions

### 🔍 Debug Mode
To enable detailed logging for troubleshooting:

1. **Set environment variable**:
   ```bash
   # Windows
   set ASPNETCORE_ENVIRONMENT=Development
   
   # macOS/Linux  
   export ASPNETCORE_ENVIRONMENT=Development
   ```

2. **Run with verbose logging**:
   ```bash
   dotnet run --verbosity diagnostic
   ```

### 📞 Getting Help
- **Azure Support**: If you encounter Azure-specific issues, consult [Azure Support](https://azure.microsoft.com/support/)
- **GitHub Issues**: Report bugs or request features in the repository issues
- **Documentation**: Check Azure service documentation for the latest API changes

---

## 🎯 Quick Verification Checklist
Before running the RagAPI, verify:

- [ ] ✅ .NET 9 SDK installed (`dotnet --version`)
- [ ] ✅ Azure CLI installed and logged in (`az account show`)
- [ ] ✅ Azure resources provisioned (or manually created)
- [ ] ✅ `.env` file exists in `src/RagApi/.env`
- [ ] ✅ All required environment variables set in `.env`
- [ ] ✅ Documents uploaded to Blob Storage (optional for testing)
- [ ] ✅ Search index created and configured
- [ ] ✅ Azure OpenAI deployment active and accessible
- [ ] ✅ Port 5000 available (or alternative port configured)
