if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI is not installed. Please install it before running this script."
    exit 1
}

# Variables
$tenantId = $env:AZURE_TENANT_ID
if (-not $tenantId) {
    $tenantId = Read-Host "Enter your Azure tenant ID"
}
$subscriptionId = $env:AZURE_SUBSCRIPTION_ID
if (-not $subscriptionId) {
    $subscriptionId = Read-Host "Enter your Azure subscription ID"
}
$resourceGroup = "rag-workshop-rg"

# Validate tenant ID format (GUID)
if ($tenantId -notmatch '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') {
    Write-Host "❌ Invalid tenant ID format. Please provide a valid GUID."
    exit 1
}

# Login to Azure
az login --tenant $tenantId

# Set subscription
az account set --subscription $subscriptionId

# Confirm deletion
Write-Host "⚠️ WARNING: This will delete the entire resource group '$resourceGroup' and all resources within it."
$confirm = Read-Host "Are you absolutely sure you want to proceed? Type 'yes' to confirm"

if ($confirm -ne "yes") {
    Write-Host "❌ Cancelled by user."
    exit 0
}

$exists = az group exists --name $resourceGroup | ConvertFrom-Json

if (-not $exists) {
    Write-Host "❌ Resource group '$resourceGroup' does not exist. Nothing to delete."
    exit 0
}

Write-Host "🗑️ Resource group '$resourceGroup' exists. Proceeding with deletion..."
# Delete resource group and all resources
Write-Host "🗑️ Deleting resource group '$resourceGroup'..."
az group delete --name $resourceGroup --yes --no-wait

Write-Host "✅ Deletion initiated. The resource group and all resources will be deleted."
Write-Host "Note: Deletion may take several minutes to complete."
