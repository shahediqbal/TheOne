# Software Architecture Document

## Architecture Style

Clean Architecture.

## Solution

- TheOne.API
- TheOne.Application
- TheOne.Domain
- TheOne.Infrastructure
- TheOne.Persistence
- TheOne.Identity
- TheOne.McpServer
- TheOne.Worker

## Authentication

- ASP.NET Core Identity
- JWT
- Refresh Token
- MFA

## Authorization

User
-> Role
-> Role Permission
-> User Override Permission
-> Data Permission

## MCP

Initial scope:
Read-only research and knowledge discovery.

## Hosting

Phase 1:
Shared hosting approach.

Phase 2:
Cloud scalable architecture.
