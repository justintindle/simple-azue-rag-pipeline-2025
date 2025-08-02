# Variables
$resourceGroup = "rag-workshop-rg"
$location = "eastus"
$storageAccount = "ragstorageacct$((Get-Random -Minimum 1000 -Maximum 9999))"
$containerName = "documents"
$localFolder = "../sample-data"  # Adjust if needed

# 1. Create resource group
az group create --name $resourceGroup --location $location

# 2. Create storage account
az storage account create `
  --name $storageAccount `
  --resource-group $resourceGroup `
  --sku Standard_LRS `
  --location $location

# 3. Get storage account key
$storageKey = az storage account keys list `
  --resource-group $resourceGroup `
  --account-name $storageAccount `
  --query "[0].value" `
  --output tsv

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
