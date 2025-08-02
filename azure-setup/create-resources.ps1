if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI is not installed. Please install it before running this script."
    exit 1
}

$proceed = Read-Host "This will create resources in Azure. Continue? (Y/N)"
if ($proceed -ne "Y") {
    Write-Host "❌ Cancelled by user."
    exit 0
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
$location = "eastus"
$storageAccount = "ragstorageacct$((Get-Random -Minimum 1000 -Maximum 9999))"
$containerName = "documents"
$localFolder = "../sample-data"  # Adjust if needed

# Login to Azure
az login --tenant $tenantId

# Set subscription
az account set --subscription $subscriptionId

# 1. Create resource group
az group create --name $resourceGroup --location $location

# 2. Create storage account
az storage account create `
  --name $storageAccount `
  --resource-group $resourceGroup `
  --sku Standard_LRS `
  --location $location

# 3. Get storage account key
$storageKey = (az storage account keys list `
  --resource-group $resourceGroup `
  --account-name $storageAccount `
  --query "[0].value" `
  --output tsv).Trim()

# 4. Create blob container
az storage container create `
  --name $containerName `
  --account-name $storageAccount `
  --account-key $storageKey `
  --public-access off

# 5. Upload local documents to blob container
Get-ChildItem -Path $localFolder -Filter *.pdf | ForEach-Object {
    az storage blob upload `
      --account-name $storageAccount `
      --account-key $storageKey `
      --container-name $containerName `
      --name $_.Name `
      --file $_.FullName `
      --overwrite
}

Write-Host "✅ Upload complete. Blob container: $containerName in storage account: $storageAccount"

# 6. Create Azure Search Service resource
$searchServiceName = "ragsearch$((Get-Random -Minimum 1000 -Maximum 9999))"

az search service create `
  --name $searchServiceName `
  --resource-group $resourceGroup `
  --sku basic `
  --location $location

Write-Host "✅ Search service created: $searchServiceName in resource group: $resourceGroup"

# 7. Create Azure Search index
# Ensure you have a search-index.json file in the current directory with the correct index definition.
# See Azure Search index documentation: https://learn.microsoft.com/en-us/azure/search/search-howto-create-index-powershell
# Example search-index.json:
# {
#   "name": "rag-index",
#   "fields": [
#     { "name": "id", "type": "Edm.String", "key": true, "searchable": false },
#     { "name": "content", "type": "Edm.String", "searchable": true, "retrievable": true, "analyzer": "en.lucene" }
#   ]
# }

if (Test-Path "./search-index.json") {
    az search index create `
      --name rag-index `
      --service-name $searchServiceName `
      --resource-group $resourceGroup `
      --body ./search-index.json

    Write-Host "✅ Search index created: rag-index in search service: $searchServiceName"
} else {
    Write-Host "❌ ERROR: search-index.json file not found in current directory. Please provide the file before running this step."
}

# 8. Create Azure Search data source
$dataSourceName = "rag-blob-datasource"

az search data-source create `
  --name $dataSourceName `
  --service-name $searchServiceName `
  --resource-group $resourceGroup `
  --type azureblob `
  --connection-string "DefaultEndpointsProtocol=https;AccountName=$storageAccount;AccountKey=$storageKey;EndpointSuffix=core.windows.net" `
  --container name=$containerName

Write-Host "✅ Data source created: $dataSourceName in search service: $searchServiceName"

# 9. Create Azure Search indexer

$indexerName = "rag-indexer"

az search indexer create `
  --name $indexerName `
  --service-name $searchServiceName `
  --resource-group $resourceGroup `
  --data-source-name $dataSourceName `
  --target-index-name rag-index `
  --schedule-interval PT5M

Write-Host "✅ Indexer created: $indexerName (runs every 5 minutes)"

# 10. Create Azure OpenAI resource
az cognitiveservices account create `
  --name ragopenai `
  --resource-group $resourceGroup `
  --kind OpenAI `
  --sku S0 `
  --location $location `
  --yes

Write-Host "✅ Azure OpenAI resource created: ragopenai in resource group: $resourceGroup"

Write-Host "`n🎉 All resources created successfully!"
Write-Host "Storage Account: $storageAccount"
Write-Host "Blob Container: $containerName"
Write-Host "Search Service: $searchServiceName"
Write-Host "Search Index: rag-index"
Write-Host "Data Source: $dataSourceName"
Write-Host "Indexer: $indexerName"
Write-Host "OpenAI Resource: ragopenai"

# Manually trigger the indexer to start indexing immediately
Write-Host "`n🔄 Triggering indexer to run immediately..."
az search indexer run `
  --name $indexerName `
  --service-name $searchServiceName `
  --resource-group $resourceGroup

Write-Host "✅ Indexer manually triggered"

