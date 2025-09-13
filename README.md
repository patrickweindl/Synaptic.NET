# Synaptic.NET
Synaptic.NET is a fully .NET-based solution to provide an ASP.NET server with MCP, RESTful API functionalities, a Blazor web interface,
a RAG base via Qdrant and OpenAI API communication — all secured via OAuth2.

This repository acts as a starting point to dive into setting up MCP + RESTful RAG systems through a hybrid approach with EF and Qdrant with a basic implementation for your own project.

<img width="1327" height="1026" alt="image" src="https://github.com/user-attachments/assets/e87ab863-6f6e-4c1e-82c0-57ba28ae5025" />


## Getting Started
To explore functionality with a default setup:
1) Have .NET SDK 10 installed (https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
2) Clone the repository
3) Make sure you have a reverse proxy with an externally available domain with a valid TLS certificate
4) Run `dotnet publish .././src/Synaptic.NET.AppHost/Synaptic.NET.AppHost.csproj -c Release /t:PublishContainer` or set chmod +X on the `BuildDockerImage.sh` in ./docker and run `./BuildDockerImage.sh` to build a docker image
5) Adjust the variables in `./docker-compose.yml` according to your keys and URLs
6) Make sure you have an application configured in either GitHub, Microsoft or Google for OAuth2 authentication and carry over your application credentials to the `appsettings.json` file or environment variables, make sure the redirect-URI is exactly `https://your-uri/oauth-callback`
7) Run `docker-compose up -d` in the folder where your `docker-compose.yml` is located

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
  - [x] ChatGPT Desktop Authenticated MCP (not tested)
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
- [x] Easy docker building
- [ ] CI/CD with package publishing
- [x] Extend readme and usage instructions
