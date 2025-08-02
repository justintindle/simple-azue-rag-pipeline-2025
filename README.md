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

## 📦 Requirements

- Azure subscription with Cognitive Search and OpenAI access
- .NET 6+ SDK
- Visual Studio Code or Visual Studio
- PowerShell (for provisioning resources)

---

## 🚀 Quickstart

```bash
# Provision Azure resources
cd azure-setup
./create-resources.ps1

# Run console app
cd src/RagConsole
dotnet run

# Or run API
cd src/RagApi
dotnet run
