---
trigger: always_on
---

# Core Rules

- Project is authentication service
- Must be stateless
- Must support multi-tenant
- JWT must include companyId
- Role-based authorization is required

Never:
- Add session logic
- Store user state in memory
- Ignore tenant structure