# AuthApi - Stateless Multi-Tenant Authentication Service

A robust, production-ready authentication service built with **ASP.NET Core 8 Web API**. It provides stateless JWT-based authentication, multi-tenant (company) support, and robust role-based authorization.

---

## 🚀 Features

-   **Stateless JWT Authentication**: Secure token generation and verification using `HS256`.
-   **Multi-tenant Architecture**: Users are scoped to a specific `CompanyId` in the JWT claims.
-   **Role-Based Access Control (RBAC)**: Support for `Admin`, `ReportViewer`, and `ReportCreator` roles.
-   **Comprehensive Auth Flows**:
    -   Registration (User + Company creation)
    -   Login & Token Generation
    -   Password Management (Change/Forgot/Reset)
-   **Security First**:
    -   **Rate Limiting**: Applied to authentication endpoints to prevent brute-force attacks.
    -   **OWASP Compliance**: Generic error messages to prevent user existence leaks.
    -   **Input Validation**: Strict validation on all incoming DTOs.
-   **Automatic Seeding**: Seeds initial roles and a system admin user on first startup.
-   **Fully Containerized**: Ready for deployment with **Docker** and **Docker Compose**.
-   **Automated Testing**: 45+ Unit and Integration tests covering core functionality.

---

## 🛠️ Technology Stack

-   **Framework**: .NET 8.0 (ASP.NET Core Web API)
-   **Identity**: Microsoft ASP.NET Core Identity
-   **ORM**: Entity Framework Core
-   **Database**: Microsoft SQL Server
-   **Auth**: JWT (JSON Web Token)
-   **Testing**: XUnit, Moq, TestServer
-   **Containerization**: Docker, Docker Compose

---

## 📦 Getting Started

### Prerequisites

-   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
-   [Docker Desktop](https://www.docker.com/products/docker-desktop) (Optional, for containerized run)
-   SQL Server (If running locally)

### Option 1: Running with Docker Compose (Recommended)

1.  Clone the repository.
2.  Navigate to the root directory.
3.  Run the following command:
    ```bash
    docker-compose up --build -d
    ```
4.  Access the API at `http://localhost:8080/swagger`.

### Option 2: Running Locally

1.  Update the `DefaultConnection` in `appsettings.json` to point to your local SQL Server instance.
2.  Apply migrations:
    ```bash
    dotnet ef database update
    ```
3.  Run the application:
    ```bash
    dotnet run
    ```

---

## 📜 Documentation

Detailed documentation is available in the `docs/` directory:

-   [API Documentation](file:///c:/Users/pasaa/Desktop/backend/docs/api-documentation.md): Endpoints, JWT structure, and Rate Limiting details.
-   [Project Structure](file:///c:/Users/pasaa/Desktop/backend/docs/project-structure.md): Directory-level explanation of the architecture.

---

## 🧪 Testing

The project includes a comprehensive suite of unit and integration tests. To run them, use:

```bash
dotnet test
```

---

## 🔒 Security Note

In production, ensure you override the default `JwtSettings:Key` and `AdminSettings:Password` using environment variables. These settings are pre-configured in the `docker-compose.yml` for demonstration purposes.

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details (if applicable).
