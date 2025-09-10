# Synaptic.NET
Synaptic.NET is a fully .NET-based solution to provide an ASP.NET server with MCP, RESTful API functionalities, a Blazor web interface,
a RAG base via Qdrant and OpenAI API communication — all secured via OAuth2.

This repository acts as a starting point to dive into setting up MCP + RESTful RAG systems through a hybrid approach with EF and Qdrant with a basic implementation for your own project.

## Getting Started
To explore functionality with a default setup:
1) Setup a local Qdrant instance (see [Qdrant](https://github.com/qdrant/qdrant))
2) Clone the repository
3) Adjust the `appsettings.json` file in src/Synaptic.NET.AppHost to match your local setup for ports and forwarding
4) Make sure you provide an OpenAI API key in either the `appsettings.json` file or environment variables
5) Make sure you have an application configured in either GitHub, Microsoft or Google for OAuth2 authentication and carry over your application credentials to the `appsettings.json` file or environment variables, make sure the redirect-URI is exactly `https://your-uri/oauth-callback`
6) Build the solution
7) Run the solution in HTTPS configuration (HTTP is available but should be used only when running the solution behind a reverse proxy as per certifications)

*If you are getting an error when opening the Blazor web UI that no secure connection can be established:*

*Option A: run the server in HTTP behind a reverse proxy with a certbot, preferably with an openly accessible domain name that is included in the appsettings.json.*

*Option B: run the server in HTTPS with a self-signed certificate (`dotnet dev-certs https --trust`) and make sure you are not connected to any SSH tunnels.*

## Features

### Blazor Web UI
- [x] Login/Logout
- [x] User Management
- [x] Dashboard
- [x] Search
- [x] Observable file creation results
- [ ] Observable search results
- [x] File Upload
- [x] Memory Management
- [x] Background Memory Upload
### MCP
- [x] Memory Endpoints
- [x] Common Tools
### RESTful API
- [x] Memory Endpoints
- [x] Common Tools
### Backend
- [x] Authentication/Authorization
  - [x] VSCode Authenticated MCP
    - [x] GitHub
    - [x] MS
    - [x] Google
  - [x] Claude Authenticated MCP
    - [x] GitHub
    - [x] MS
    - [x] Google
  - [x] Claude Code Authenticated MCP
    - [x] GitHub
    - [x] MS
    - [x] Google
  - [ ] CustomGPT Authenticated RestAPI Access
    - [ ] GitHub
    - [ ] MS
    - [ ] Google
  - [ ] EntraID
- [ ] Observable results for memory creation and search for frontend
- [ ] Group permissions for users
- [x] Restricted access for guests
- [x] Sharing memory stores with groups
### Others
- [x] Qdrant
- [x] Entity Framework Core
- [x] OpenAI
- [ ] Builder pattern for AppHost
- [ ] Docker support
- [ ] CI/CD with package publishing
- [ ] Extend readme and usage instructions
