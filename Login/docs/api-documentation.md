# API Documentation (AuthApi)

This document outlines the API endpoints, data structures, and JWT (JSON Web Token) standards for the AuthApi project.

## 1. Authentication (JWT) Structure

The system is stateless, and all authorization is handled via **Bearer Token** sent in the HTTP `Authorization` header.

### JWT Claims (Payload)
Each token contains the following standard and custom claims:

| Claim | Description | Example Value |
| :--- | :--- | :--- |
| `sub` | Unique User ID (Guid) | `550e8400-e29b-41d4-a716-446655440000` |
| `email` | User's Email Address | `user@example.com` |
| `companyId`| User's Company/Tenant ID | `a1b2c3d4-e5f6...` |
| `role` | User's Roles (Multiple roles supported) | `Admin`, `ReportViewer` |
| `jti` | Unique Token ID (to prevent replay attacks) | `uuid-v4-string` |
| `exp` | Token Expiration Time (Unix Epoch) | `1712232000` |

---

## 2. API Endpoints

### A. Identity & Account Management (`AuthController`)

#### [POST] `/api/auth/register`
Registers a new user and a new company (tenant).
- **Access:** Anonymous
- **Rate Limit:** Moderate
- **Request Body:**
```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "companyName": "TechCorp"
}
```

#### [POST] `/api/auth/login`
Authenticates a user and returns a JWT token.
- **Access:** Anonymous
- **Rate Limit:** Strict
- **Request Body:**
```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

#### [POST] `/api/auth/forgot-password`
Generates a password reset token and "sends" an email.
- **Access:** Anonymous
- **Rate Limit:** Strict

#### [POST] `/api/auth/reset-password`
Resets the user's password using the generated token.
- **Access:** Anonymous
- **Rate Limit:** Moderate

#### [POST] `/api/auth/change-password`
Allows an authenticated user to change their own password.
- **Access:** Authenticated (`Authorize`)
- **Rate Limit:** Moderate

#### [GET] `/api/auth/me`
Returns current user details (ID, Email, CompanyId, Roles) from the token.
- **Access:** Authenticated (`Authorize`)

---

### B. Administrative Operations (`AdminController`)

#### [POST] `/api/admin/roles/assign`
Assigns a specific role to a user.
- **Access:** **Admin** role only (`Authorize[Roles="Admin"]`)
- **Request Body:**
```json
{
  "userId": "guid-user-id",
  "roleName": "ReportCreator"
}
```

---

## 3. Security & Error Handling

### HTTP Status Codes
- `200 OK`: Request successful.
- `400 BadRequest`: Validation failed or business logic error.
- `401 Unauthorized`: Token invalid, missing, or expired.
- `403 Forbidden`: User lacks required roles/permissions.
- `429 TooManyRequests`: Rate limit exceeded.

### Rate Limiting Policies
- **Strict:** Max 3 requests per 30 seconds (Login, Forgot Password).
- **Moderate:** Max 10 requests per 60 seconds (Register, Reset Password, Change Password).
