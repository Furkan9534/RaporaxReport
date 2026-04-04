# Project Structure & Directory Guide

This guide provides a clear overview of the **AuthApi** project's architecture and organization, designed to be understood easily by developers of all levels.

---

## Root Directory Files

- **`Program.cs`**: The heart of the application. It handles startup, dependency injection, database configuration, security policies, and middleware orchestration.
- **`appsettings.json`**: Contains configuration settings such as database connection strings, JWT settings, and logging levels.
- **`AuthApi.csproj`**: The project file defining dependencies, target framework (.NET 8), and NuGet packages.
- **`Dockerfile` & `docker-compose.yml`**: Configuration for containerization and service orchestration.

---

## Core Directories

### 1. `Controllers` (The Receptionists)
Controllers handle incoming HTTP requests and route them to the appropriate services. They act as the entry point for the API.
- `AuthController.cs`: Manages login, registration, and password flows.
- `AdminController.cs`: Handles administrative tasks like role assignment.

### 2. `Services` (The Kitchen)
This is where the business logic lives. Services process data and enforce rules.
- `AuthService.cs`: Coordinates authentication logic.
- `TokenService.cs`: Generates secure JWT (JSON Web Tokens).
- `AdminService.cs`: Manages administrative operations.
- `FakeEmailSender.cs`: A mock implementation for sending emails (for development).

### 3. `DTOs` (Data Transfer Objects)
Contract classes used for data exchange between the client and the server.
- `...Request.cs`: Models for incoming data.
- `...Response.cs`: Models for outgoing data.

### 4. `Entities` (The Building Blocks)
C# classes that represent the database schema (Models).
- `ApplicationUser.cs`: Extends the default Identity user with custom properties like `CompanyId`.
- `Company.cs`: Represents the tenant/company in our multi-tenant architecture.

### 5. `Data` (The Bridge)
Handles the connection between the application and the SQL Server database.
- `AppDbContext.cs`: The Entity Framework Core context for database operations.

### 6. `Migrations` (Database History)
Auto-generated files that track changes to the database schema over time.

### 7. `AuthApi.Tests` (The Auditors)
A separate project dedicated to verifying the correctness of the application.
- **Unit Tests**: Test individual components in isolation using Mocks.
- **Integration Tests**: Verify the full system flow from HTTP request to Database.
