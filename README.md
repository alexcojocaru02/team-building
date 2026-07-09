# TeamConnect

TeamConnect is a full-stack dissertation/demo project for supporting remote team cohesion.

The repository contains two main applications:

- [UI/](UI/) - Angular 20 frontend using standalone components
- [TeamConnect.Api/](TeamConnect.Api/) - ASP.NET Core 8 Web API with MongoDB and JWT authentication

## What the project includes

- Authentication with register/login flows
- Protected user, feed, feedback, dashboard, and team features
- Angular route guards and HTTP auth handling
- REST API backed by MongoDB
- Automated end-to-end tests in [e2e/](e2e/)

## Repository layout

- [UI/](UI/) - Angular frontend
- [TeamConnect.Api/](TeamConnect.Api/) - backend API and solution
- [TeamConnect.Api.Tests/](TeamConnect.Api.Tests/) - backend tests
- [e2e/](e2e/) - Playwright end-to-end tests
- [docs/](docs/) - architecture documentation
- [start-dev.ps1](start-dev.ps1) / [start-dev.cmd](start-dev.cmd) - helper scripts to start frontend and backend locally

## Requirements

- Node.js and npm
- .NET 8 SDK
- MongoDB running locally for development

## Local development

### Frontend

From [UI/](UI/):

```bash
npm install
npm start
```

The frontend runs at http://localhost:4200.

### Backend

From [TeamConnect.Api/](TeamConnect.Api/):

```bash
dotnet run
```

The API is exposed locally at http://localhost:5217/api, with HTTPS also available at https://localhost:7241/api.

### Start both apps

From the repository root, use one of the helper scripts:

- [start-dev.ps1](start-dev.ps1)
- [start-dev.cmd](start-dev.cmd)

## Testing

- Backend tests: [TeamConnect.Api.Tests/](TeamConnect.Api.Tests/)
- End-to-end tests: [e2e/](e2e/)
- Angular unit tests can be run from [UI/](UI/) with `npm test`

## Deployment

The deployment process builds the Angular app first and then publishes the ASP.NET Core project. The backend project includes a publish target that copies the Angular production build into the API output during publish.

See [DEPLOYMENT.md](DEPLOYMENT.md) for the full deployment workflow.

## Documentation

- [copilot-context.md](copilot-context.md) - repo-wide working notes and architecture context
- [DEPLOYMENT.md](DEPLOYMENT.md) - deployment guide
- [IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md) - implementation summary
- [raport-tehnic-backend.md](raport-tehnic-backend.md) - backend technical report
- [raport-tehnic-frontend.md](raport-tehnic-frontend.md) - frontend technical report
- [docs/architecture-diagram.md](docs/architecture-diagram.md) - architecture overview

## Notes

- The backend uses JWT bearer authentication.
- The frontend expects the API to be available through the configured development URL.
- Keep frontend changes in [UI/](UI/) and API changes in [TeamConnect.Api/](TeamConnect.Api/).