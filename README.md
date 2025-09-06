# Synaptic.NET
Synaptic.NET is a fully .NET-based solution to provide an ASP.NET server with MCP, RESTful API functionalities, a Blazor web interface,
a RAG base via Qdrant and OpenAI API communication — all secured via OAuth2.

This repository acts as a starting point to dive into setting up MCP+RESTful RAG systems with a basic implementation through Qdrant.

## Getting Started
To explore functionality with a default setup:
1) Setup a local Qdrant instance (see [Qdrant](https://github.com/qdrant/qdrant))
2) Clone the repository
3) Adjust the `appsettings.json` file in src/Synaptic.NET.AppHost to match your local setup for ports and forwarding
4) Build the solution
5) Run the solution in HTTPS configuration (HTTP is available but should be used only when running the solution behind a reverse proxy)

*If you are getting an error when opening the Blazor web UI that no secure connection can be established:*

*Option A: run the server in HTTP behind a reverse proxy with a certbot, preferably with an openly accessible domain name that is included in the appsettings.json.*

*Option B: run the server in HTTPS with a self-signed certificate and make sure you are not connected to any SSH tunnels.*

## Features

- [x] Blazor Web UI
    - [x] Login/Logout
    - [x] User Management
    - [ ] Dashboard
    - [ ] Search
    - [ ] Observable results
    - [ ] File Upload
    - [ ] Memory Management
- [x] MCP
  - [x] Memory Endpoints
  - [x] Common Tools
- [ ] RESTful API
    - [ ] Memory Endpoints
    - [ ] Common Tools
- [x] Backend
  - [x] OAuth2
  - [ ] Observable results for memory creation and search for frontend
  - [ ] Group permissions for users
- [x] Qdrant
- [x] OpenAI
- [ ] Builder pattern for AppHost
- [ ] Docker support
- [ ] CI/CD with package publishing
- [ ] Extend readme and usage instructions
