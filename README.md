# Synaptic.NET
Synaptic.NET is a fully .NET-based solution to provide an ASP.NET server with MCP, RESTful API functionalities, a Blazor web interface,
a RAG base via Qdrant and OpenAI API communication — all secured via OAuth2.

This repository acts as a starting point to dive into setting up MCP + RESTful RAG systems through a hybrid approach with EF and Qdrant with a basic implementation for your own project.

<img width="1327" height="1026" alt="image" src="https://github.com/user-attachments/assets/e87ab863-6f6e-4c1e-82c0-57ba28ae5025" />


## Getting Started
To explore functionality with a default setup:
1) Setup a Qdrant instance (see [Qdrant](https://github.com/qdrant/qdrant))
2) Setup a PostgreSQL instance (see [PostgreSQL](https://hub.docker.com/_/postgres))
3) Clone the repository
4) Adjust the `appsettings.json` file or your environment variables in `src/Synaptic.NET.AppHost` to match your local setup for ports and forwarding (check the exampleAppSettings.json in the same folder for reference)
5) Make sure you provide an OpenAI API key in either the `appsettings.json` file or environment variables
6) Make sure you have an application configured in either GitHub, Microsoft or Google for OAuth2 authentication and carry over your application credentials to the `appsettings.json` file or environment variables, make sure the redirect-URI is exactly `https://your-uri/oauth-callback`
7) Build the solution
8) Run the solution in HTTPS configuration (HTTP is available but should be used only when running the solution behind a reverse proxy as per certifications)

*If you are getting an error when opening the Blazor web UI that no secure connection can be established:*

*Option A: run the server in HTTP behind a reverse proxy with a certbot, preferably with an openly accessible domain name that is included in the app settings as a configuration entry either via the appsettings.json or environment variables.*

*Option B: run the server in HTTPS with a self-signed certificate (`dotnet dev-certs https --trust`) and make sure you are not connected to any SSH tunnels.*

### Remarks
It's possible to only use individual portions of this project (e.g. only the RESTful API or only the Blazor web UI). You can also use the backend with your own frontend or MCP implementation as long as interfaces are satisfied. This repository should only give you a head start on how to set up a MCP/REST based RAG system with a web UI and authentication.

Check out the samples in the `samples` folder for example partial usages or injections.

## Features

### Blazor Web UI
- [x] Login/Logout
- [x] User Management
- [x] Dashboard
- [x] Search
- [x] Observable file creation results
- [x] Observable search results
- [x] File Upload
- [x] Memory Management
- [x] Background Memory Upload
- [ ] User data exporter
- [ ] Exporter to another Synaptic.NET instance
### MCP
- [x] Memory Endpoints
- [x] Common Tools
### RESTful API
- [x] Memory Endpoints
- [x] Common Tools
### Backend
- [x] Authentication/Authorization (via OAuth2 flows with providers GitHub, MS or Google)
  - [x] VSCode Authenticated MCP
  - [x] Claude Authenticated MCP
  - [x] Claude Code Authenticated MCP
  - [ ] ChatGPT Desktop Authenticated MCP (not tested)
    - [ ] GitHub
    - [ ] MS
    - [ ] Google
  - [ ] EntraID
- [x] Observable results for memory creation and search for frontend
- [ ] Group permissions for users (permissions of users within groups)
- [x] Rest and MCP endpoints with filterable queries (e.g. only look in certain groups or in user memories)
- [x] Restricted access for guests
- [x] Sharing memory stores with groups
### Others
- [x] Qdrant
- [x] Entity Framework Core
- [x] OpenAI
- [ ] Swagger UI
- [ ] Docker support
- [ ] CI/CD with package publishing
- [x] Extend readme and usage instructions
