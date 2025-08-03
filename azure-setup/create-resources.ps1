# Helper functions
function Test-AzCli {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Host "❌ Azure CLI is not installed. Please install it before running this script."
        return $false
    }
    return $true
}

function Invoke-AzCommand {
    param([string]$Command, [string]$ErrorMessage)
    try {
        $result = Invoke-Expression "az $Command"
        if ($LASTEXITCODE -ne 0) { throw "Az command failed" }
        return $result
    }
    catch {
        Write-Host "❌ $ErrorMessage"
        Write-Host "Command: az $Command"
        Write-Host "Error: $_"
        return $null
    }
}

function Test-OpenAIExists {
    param($name, $resourceGroup)
    $result = Invoke-AzCommand "cognitiveservices account show --name $name --resource-group $resourceGroup 2>$null" "Failed to check OpenAI existence"
    return $null -ne $result
}

function Get-SoftDeletedOpenAI {
    param($name, $resourceGroup)
    $result = Invoke-AzCommand "cognitiveservices account list-deleted --resource-group $resourceGroup" "Failed to list deleted OpenAI resources"
    if ($result) {
        return ($result | ConvertFrom-Json) | Where-Object { $_.name -eq $name }
    }
    return $null
}

function Restore-OpenAI {
    param(
        [string]$name,
        [string]$resourceGroup,
        [string]$location
    )
    try {
        $result = az cognitiveservices account recover `
            --name $name `
            --resource-group $resourceGroup `
            --location $location `
            --yes
        return $true
    }
    catch {
        Write-Host "❌ Failed to restore Azure OpenAI resource: $name"
        return $false
    }
}

function New-AzureResource {
    param(
        [string]$ResourceType,
        [string]$Name,
        [hashtable]$Params
    )
    $paramsString = $Params.GetEnumerator() | ForEach-Object { "--$($_.Key) `"$($_.Value)`"" }
    $command = "$ResourceType create --name $Name $paramsString"
    return Invoke-AzCommand $command "Failed to create $ResourceType resource: $Name"
}

# Validate prerequisites
if (-not (Test-AzCli)) { exit 1 }

# Variables
$localFolder = "$PSScriptRoot/../sample-data"
$config = @{
    resourceGroup = "rag-workshop-rg"
    location = "eastus"
    storageAccount = "ragstorageacct$((Get-Random -Minimum 1000 -Maximum 9999))"
    containerName = "documents"
    searchServiceName = "ragsearch$((Get-Random -Minimum 1000 -Maximum 9999))"
    openAIName = "ragopenai"
    dataSourceName = "rag-blob-datasource"
    indexName = "rag-index"
    indexerName = "rag-indexer"
}

$tenantId = $env:AZURE_TENANT_ID
if (-not $tenantId) {
    $tenantId = Read-Host "Enter your Azure tenant ID"
}

$subscriptionId = $env:AZURE_SUBSCRIPTION_ID
if (-not $subscriptionId) {
    $subscriptionId = Read-Host "Enter your Azure subscription ID"
}

# Login and set subscription
az login --tenant $tenantId
az account set --subscription $subscriptionId

# Create resource group
New-AzureResource "group" $config.resourceGroup @{ location = $config.location }

# Create storage account
New-AzureResource "storage account" $config.storageAccount @{
    "resource-group" = $config.resourceGroup
    sku = "Standard_LRS"
    location = $config.location
}

# Get storage account key
$storageKey = (az storage account keys list `
  --resource-group $config.resourceGroup `
  --account-name $config.storageAccount `
  --query "[0].value" `
  --output tsv).Trim()

# Create blob container
az storage container create `
  --name $config.containerName `
  --account-name $config.storageAccount `
  --account-key $storageKey `
  --public-access off

# Upload local documents
Get-ChildItem -Path $localFolder -Filter *.pdf | ForEach-Object {
    az storage blob upload `
      --account-name $config.storageAccount `
      --account-key $storageKey `
      --container-name $config.containerName `
      --name $_.Name `
      --file $_.FullName `
      --overwrite
}

Write-Host "✅ Upload complete. Blob container: $($config.containerName) in storage account: $($config.storageAccount)"

# Create Azure Search service
az search service create `
  --name $config.searchServiceName `
  --resource-group $config.resourceGroup `
  --sku basic `
  --location $config.location

Write-Host "✅ Search service created: $($config.searchServiceName) in resource group: $($config.resourceGroup)"

# Create search index
if (Test-Path -Path "./search-index.json") {
    az search index create `
        --name $config.indexName `
        --service-name $config.searchServiceName `
        --resource-group $config.resourceGroup `
        --body ./search-index.json

    Write-Host "✅ Search index created: $($config.indexName)"
} else {
    Write-Host "❌ ERROR: search-index.json file not found in current directory."
    exit 1
}

# Create data source
az search datasource create `
  --name $config.dataSourceName `
  --service-name $config.searchServiceName `
  --resource-group $config.resourceGroup `
  --type azureblob `
  --connection-string "DefaultEndpointsProtocol=https;AccountName=$($config.storageAccount);AccountKey=$storageKey;EndpointSuffix=core.windows.net" `
  --container name=$($config.containerName)

Write-Host "✅ Data source created: $($config.dataSourceName)"

# Get admin key
$searchKey = (az search admin-key show `
    --service-name $config.searchServiceName `
    --resource-group $config.resourceGroup `
    --query primaryKey `
    --output tsv).Trim()

# Retry loop to wait for data source
$maxRetries = 5
$retryCount = 0
$success = $false
while (-not $success -and $retryCount -lt $maxRetries) {
    Start-Sleep -Seconds 5
    try {
        $url = "https://$($config.searchServiceName).search.windows.net/datasources/$($config.dataSourceName)?api-version=2023-07-01-Preview"
        $headers = @{ 'api-key' = $searchKey; 'Content-Type' = 'application/json' }
        $response = Invoke-RestMethod -Uri $url -Headers $headers -Method Get
        $success = $true
    } catch {
        $retryCount++
        Write-Host "Waiting for data source... ($retryCount/$maxRetries)"
    }
}

if (-not $success) {
    Write-Host "❌ ERROR: Data source not ready after $maxRetries attempts."
    exit 1
}

# Create indexer
$indexerBody = @{
    name = $config.indexerName
    dataSourceName = $config.dataSourceName
    targetIndexName = $config.indexName
    schedule = @{ interval = "PT5M" }
    parameters = @{ configuration = @{ parsingMode = "default"; indexStorageMetadataOnlyForOversizedDocuments = $false } }
} | ConvertTo-Json -Depth 10

$indexerUrl = "https://$($config.searchServiceName).search.windows.net/indexers/$($config.indexerName)?api-version=2023-07-01-Preview"
Invoke-RestMethod -Uri $indexerUrl -Headers $headers -Method Put -Body $indexerBody

Write-Host "✅ Indexer created: $($config.indexerName)"

# Create or restore Azure OpenAI
Write-Host "Checking Azure OpenAI resource..."
if (Test-OpenAIExists -name $config.openAIName -resourceGroup $config.resourceGroup) {
    Write-Host "✅ Azure OpenAI resource exists: $($config.openAIName)"
} else {
    $softDeleted = Get-SoftDeletedOpenAI -name $config.openAIName -resourceGroup $config.resourceGroup
    if ($softDeleted) {
        Write-Host "🔄 Restoring soft-deleted Azure OpenAI..."
        if (-not (Restore-OpenAI -name $config.openAIName -resourceGroup $config.resourceGroup -location $config.location)) {
            Write-Host "❌ Restore failed. Creating new..."
        }
    }
    az cognitiveservices account create `
        --name $config.openAIName `
        --resource-group $config.resourceGroup `
        --kind OpenAI `
        --sku S0 `
        --location $config.location `
        --yes
}

# Final output
Write-Host "`n🎉 All resources created:"
$config.GetEnumerator() | ForEach-Object { Write-Host "$($_.Key): $($_.Value)" }

# Trigger indexer
$runIndexerUrl = "https://$($config.searchServiceName).search.windows.net/indexers/$($config.indexerName)/run?api-version=2023-07-01-Preview"
Invoke-RestMethod -Uri $runIndexerUrl -Headers $headers -Method Post
Write-Host "✅ Indexer manually triggered"