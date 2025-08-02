# Basic scaffolding — you can customize it more later
az group create --name rag-workshop-rg --location eastus

az storage account create --name ragstorageacct --resource-group rag-workshop-rg --sku Standard_LRS

az cognitiveservices account create `
  --name rag-search `
  --resource-group rag-workshop-rg `
  --kind Search `
  --sku Standard `
  --location eastus `
  --yes

az cognitiveservices account create `
  --name rag-openai `
  --resource-group rag-workshop-rg `
  --kind OpenAI `
  --sku S0 `
  --location eastus `
  --yes
