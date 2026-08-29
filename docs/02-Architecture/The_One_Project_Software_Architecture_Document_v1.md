# The One Project - Software Architecture Document

## Architecture

The system follows Clean Architecture.

Solution:

- TheOne.API
- TheOne.Application
- TheOne.Domain
- TheOne.Infrastructure
- TheOne.Persistence
- TheOne.Identity
- TheOne.McpServer
- TheOne.Worker

## Authentication

ASP.NET Core Identity
JWT
Refresh Token
MFA

## Authorization

User
|
Role
|
Role Permission
|
User Specific Permission Override
|
Data Permission
