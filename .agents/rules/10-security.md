---
trigger: always_on
---

# Security Rules

- No secret in source code
- No detailed error messages
- No user existence leak
- Rate limiting required on auth endpoints
- Input validation is mandatory

JWT:
- Must be signed securely
- Must have expiration