# Project Context: AuthApi

This project is an ASP.NET Core Web API based authentication service.

## Core Requirements

- JWT based authentication
- Stateless architecture (no session, no server memory storage)
- Multi-tenant support (each user belongs to a company/tenant)
- JWT must include companyId claim
- Role-based authorization

## Roles

- ReportViewer
- ReportCreator
- Admin

## Features

- Login
- Change password
- Forgot password (email-based)
- Reset password
- Role-based access control
- Rate limiting on login endpoints

## Technical Constraints

- Language: C#
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core only
- No raw SQL unless absolutely necessary
- Use ASP.NET Identity where applicable

## Security Rules

- Never expose detailed error messages
- Do not reveal if user exists or not
- No secrets in code (JWT key, DB password, SMTP, etc.)
- Always validate input
- Use rate limiting for authentication endpoints

## JWT Requirements

Token must include:

- sub (user id)
- email
- roles
- companyId
- jti
- exp

## Development Rules

- Work step by step (do NOT implement everything at once)
- Only implement requested feature
- Do not refactor unrelated code
- Prefer minimal changes (diff-based updates)
- Write clean and simple code

## Testing

- Unit tests for services
- Integration tests for endpoints
- Focus on authentication flows

## Output Expectations

Always provide:

1. Plan (short)
2. Changed files
3. Explanation
4. What should be tested
