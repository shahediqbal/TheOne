# The One Project - Full Project Documentation v5

## 1. Project Overview

The One Project is a full-fledged digital ecosystem, not only a digital
archive.

The platform combines:

-   Public Website and CMS
-   Membership Management
-   Enterprise Authentication and Authorization
-   Digital Knowledge Archive
-   Content Conversion and Review Platform
-   Research Platform
-   AI and MCP Integration
-   Future Community Features

The objective is to preserve original knowledge, manage members, provide
modern access, and create an AI-ready research ecosystem.

------------------------------------------------------------------------

## 2. System Vision

The platform consists of:

        Public Website
                |
        Member Portal
                |
        Admin Platform
                |
        Digital Library
                |
        Research AI Platform

------------------------------------------------------------------------

## 3. Major Modules

### Public Website CMS

Features:

-   Dynamic pages
-   Menu management
-   Banner management
-   Articles
-   Publications
-   Events
-   Gallery
-   Contact pages
-   SEO metadata
-   Multi-language support

**CMS multilingual content and library Unicode content are separate
concerns.**

CMS translation manages website presentation. Library Unicode content
preserves original publication content with versioning and approval.

------------------------------------------------------------------------

## 4. Membership Management

Features:

-   Online registration
-   Membership application workflow
-   Profile management
-   Approval workflow
-   Member skills and contribution tracking
-   Membership history

### Payment Integration

**Initial payment:** - bKash MFS membership payment

**Future:** - Card payment - Other MFS providers - Additional payment
gateways

### Payment Flow

        Member
         |
        Membership Registration
         |
        Payment Request
         |
        bKash MFS
         |
        Webhook Verification
         |
        Transaction Update
         |
        Membership Activation

Payment data is stored separately from membership records.

### Payment Webhook Error Handling

  -----------------------------------------------------------------------
  Scenario                            Handling
  ----------------------------------- -----------------------------------
  **Webhook timeout**                 bKash retries delivery to your
                                      webhook endpoint with exponential
                                      backoff (max 5 attempts). Your
                                      system responds with appropriate
                                      HTTP status codes to signal
                                      success/failure.

  **Duplicate webhook**               Idempotency key
                                      (`bKash_transaction_id`) check;
                                      reject duplicate

  **Signature mismatch**              Reject webhook; log security alert

  **Payment expired**                 Cancel membership application;
                                      notify user

  **Payment refunded**                Deactivate membership; log audit

  **Network failure**                 Queue for manual reconciliation
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 5. Content Conversion and Digital Library

### Supported Sources

-   PDF
-   DOC
-   DOCX
-   Bijoy/SutonnyMJ documents
-   Scanned documents

### Content Ingestion Workflow

        Upload
         |
        Text Extraction
         |
        Encoding Detection
         |
        Bijoy to Unicode Conversion
         |
        Automatic Quality Check
         |
        Human Review (Side-by-Side Diff Viewer)
         |
        Admin Approval
         |
        Library Publication

### Quality Checks

-   Word count comparison
-   Paragraph comparison
-   Character loss detection
-   Encoding validation
-   Missing content detection

### Human Review Interface

A **side-by-side diff viewer** will be implemented in the Human Review
stage:

    ┌─────────────────────────────────────────────────────────────┐
    │  Content Review Dashboard                                  │
    │  Book: "Bangla Literature Vol 3"  │  Chapter: 5 of 12     │
    │  Status: ⏳ In Review             │  Reviewer: admin@org  │
    ├──────────────────────────┬──────────────────────────────────┤
    │  Original (Bijoy)        │  Converted (Unicode)           │
    │  ┌────────────────────┐  │  ┌────────────────────────────┐│
    │  │ Avwg wPšbwZ Kivi?  │  │  │ আমি কি ভাবছি?              ││
    │  │ ZvKvi e¨vcv‡i      │  │  │ ঢাকার ইতিহাসে             ││
    │  │ Ggb wKQz BwZg~jK   │  │  │ এমন কিছু আশ্চর্যজনক       ││
    │  └────────────────────┘  │  └────────────────────────────┘│
    ├──────────────────────────┴──────────────────────────────────┤
    │  Quality Score: 96%  │  Diff Report: 4 character changes   │
    │                    [Approve] [Reject] [Edit]                │
    └─────────────────────────────────────────────────────────────┘

### Content Versioning

Content versioning preserves previous **approved** versions. The system
maintains: - Last 5 versions per content block (chosen as a balance
between storage cost and preservation completeness) - Version history
with timestamps and approver information - Ability to revert to any
previous approved version

Content has a **Draft → Review → Published** workflow.

### Original Scan Retention

**Decision required during Phase 3:** Are original scanned images
retained long-term at full resolution, or only retained
transiently/compressed post-conversion?

  -------------------------------------------------------------------------
  Approach            Storage Impact    Pros              Cons
                      (5,000 books)                       
  ------------------- ----------------- ----------------- -----------------
  **Retain            100-500 GB+       Complete          High storage cost
  full-resolution     (scans are        preservation; can 
  originals**         20-100MB/book)    re-process if     
                                        conversion        
                                        improves          

  **Retain compressed 10-50 GB          Lower cost;       Lossy; can't
  only**                                faster delivery   re-process at
                                                          higher quality

  **Retain            Minimal           Lowest cost       No original
  transiently, delete                                     backup; risk if
  post-conversion**                                       conversion errors
                                                          found later
  -------------------------------------------------------------------------

**Recommendation:** Retain full-resolution originals for at least 3
years post-conversion, then archive to cold storage.

------------------------------------------------------------------------

## 6. AI and MCP Server Scope

TheOne.McpServer provides controlled AI access to approved knowledge
sources.

### Initial Scope

Read-only research and knowledge discovery.

### Example Tools

-   `SearchKnowledge`
-   `GetBookReference`
-   `GetChapterContent`
-   `FindRelatedTopics`
-   `GetAuthorInformation`

### AI Authentication Model

TheOne.McpServer will use the **same Identity/JWT authentication
system** as the rest of the API. This ensures consistent authorization
and auditing.

    MCP Client → JWT Token → API Gateway → TheOne.McpServer → Knowledge Sources

AI will **not** directly modify or publish original content.

### Future

-   Research assistance
-   Draft generation
-   Curation support with approval workflow

------------------------------------------------------------------------

## 7. Architecture

Clean Architecture solution:

        TheOne.sln
        
        TheOne.API
        TheOne.Application
        TheOne.Domain
        TheOne.Infrastructure
        TheOne.Persistence
        TheOne.Identity
        TheOne.McpServer
        TheOne.Worker

### Architecture Flow

        Frontend
           |
        API Layer
           |
        Application Layer
           |
        Domain Layer
           |
        Infrastructure Layer
           |
        Database

------------------------------------------------------------------------

## 8. Technology Stack

### Backend

-   **ASP.NET Core 10 LTS** (released November 2025, supported through
    2028)
-   Clean Architecture
-   Entity Framework Core
-   Dapper where required

**.NET 10 is an LTS (Long-Term Support) release** with 3-year support,
making it the recommended choice for this project.

### Database

-   PostgreSQL
-   UTF-8 Unicode storage
-   Full text search

### Frontend

-   Next.js
-   TypeScript
-   Tailwind CSS
-   TanStack Query
-   Zustand

### Mobile

-   Flutter

### AI

-   Python workers
-   MCP Server integration

------------------------------------------------------------------------

## 9. Authentication and Authorization

### Authentication

-   ASP.NET Core Identity
-   JWT Access Token
-   Refresh Token
-   Role based authorization
-   Permission based authorization

### Security

-   Password hashing
-   Account lockout
-   Email/mobile verification
-   MFA for administrators
-   Audit logging

### Authorization Hierarchy

        User
         |
        Role
         |
        Role Menu Permission
         |
        User Specific Permission Override
         |
        Data Permission

User-specific permissions override role permissions.

### MFA Implementation

-   **Primary:** TOTP (Authenticator app - Google Authenticator,
    Microsoft Authenticator)
-   **Backup:** 10 recovery codes (store hashed)
-   **Recovery:** Admin override with audit log and 24-hour delay
-   **Policy:** Mandatory for all administrative users

------------------------------------------------------------------------

## 10. Dynamic Menu and Permission System

### Permissions

-   View
-   Create
-   Edit
-   Delete
-   Approve
-   Export

### Permission Priority

1.  User specific permission
2.  Role permission
3.  Default permission

### Menu Management

The system supports: - Parent menu - Child menu - Route - Icon - Module

------------------------------------------------------------------------

## 11. Database and Migration Strategy

### Database Approach

-   EF Core Code First
-   Migration scripts stored in Git
-   Environment-specific seed data

### Seed Data

-   Roles
-   Permissions
-   Menus
-   Super Admin
-   Default settings

Production migration requires approval before execution.

------------------------------------------------------------------------

## 12. Security and API Standards

### API Security

-   HTTPS enforcement
-   CORS policy
-   API versioning
-   Request validation
-   File upload restrictions
-   Rate limiting

### Rate Limiting Policy

  API Type                  Policy
  ------------------------- -----------------------------------
  **Public APIs**           Per IP based limits
  **Authentication APIs**   Strict limits against brute force
  **Admin APIs**            User based limits

### Payment Webhook

-   Signature validation
-   Idempotency protection (using `bKash_transaction_id` as the
    idempotency key)
-   Reliable callback handling with retry and dead-letter queue

------------------------------------------------------------------------

## 13. Audit Logging Policy

### Audit Logs Cover

**Authentication:** - Login - Failed login - Password changes - MFA
events

**Authorization:** - Role changes - Permission changes

**Content:** - Upload - Edit - Approval - Publishing

**Membership:** - Registration - Approval - Payment verification

### Retention Policy

  Log Type              Retention Period
  --------------------- ------------------
  Security logs         2 years
  Business audit logs   5 years

------------------------------------------------------------------------

## 14. Search, Cache and Real-Time

### Search

**Phase 1:** - PostgreSQL Full Text Search

**Future:** - Elasticsearch/OpenSearch

### Redis Caching

Used for: - Public pages - Menus - Popular content

### Cache Invalidation Triggers

The following events will trigger cache invalidation:

  Event                     Cache Keys Invalidated
  ------------------------- -----------------------------
  Role/Permission updates   User permission caches
  Menu structure changes    Navigation/menu caches
  Content publication       Book/article content caches
  User role change          User permission caches
  System settings change    Global configuration cache

### Cache Invalidation Pattern

``` csharp
// When data changes
await _redis.PublishAsync("cache:invalidate", "menus:public");

// In each instance
_redis.Subscribe("cache:invalidate", (key) => {
    _cache.Remove(key);
});
```

### SignalR

Used for: - Notifications - Membership updates - Payment status updates

------------------------------------------------------------------------

## 15. Testing Strategy

### Unit Testing

-   xUnit
-   Moq

### Integration Testing

-   WebApplicationFactory
-   TestContainers PostgreSQL

### Frontend Testing

-   Playwright
-   Vitest

### Performance Testing

-   k6

### Security Testing

-   OWASP ZAP

### CI Gates

-   Build success
-   Automated tests passed
-   Security checks passed before merge

------------------------------------------------------------------------

## 16. Deployment and DevOps

### Deployment Approach

-   Git workflow
-   CI/CD pipeline
-   Docker support
-   Environment-based configuration

### Hosting Model

The application will be deployed as a **self-hosted Docker container**
on a cloud VM or Kubernetes cluster, not on Vercel/Netlify serverless.
This ensures:

-   Consistent environment between staging and production
-   Predictable performance for Bangladeshi users
-   Full control over infrastructure
-   No cold-start latency for API endpoints

**Decision required during Phase 1:** Choose a cloud provider for
hosting: - **Option A:** Azure (preferred if Microsoft ecosystem
alignment matters) - **Option B:** AWS - **Option C:** Local/regional
hosting provider (lowest latency for Bangladesh)

### Infrastructure Requirements

-   HTTPS
-   Structured logging (Serilog)
-   Monitoring
-   Backup strategy
-   Disaster recovery plan

### Storage Strategy

**Object storage (to be decided during Phase 1) is recommended for:** -
Books - Images - Documents

**Options:** - Self-hosted MinIO - S3-compatible cloud storage - Azure
Blob Storage / AWS S3

Database stores metadata and references only.

### Content Delivery Network (CDN)

A CDN is recommended to meet \<2s page-load and \<3s book-load targets.
The object storage should be fronted by a CDN:

  Provider             Notes
  -------------------- -------------------------------------------
  **Cloudflare**       Free tier available; good global coverage
  **Azure CDN**        If using Azure Blob Storage
  **AWS CloudFront**   If using S3
  **Bunny.net**        Affordable; good Southeast Asia coverage

### CI/CD Pipeline Flow

``` yaml
Pull Request:
  - Build .NET
  - Run Unit Tests
  - Run Integration Tests
  - Run Lint/StyleCop

Merge to main:
  - Run all tests
  - Build Docker images
  - Deploy to Staging
  - Run E2E tests (Playwright)
  - Manual approval required

Release (tag vX.Y.Z):
  - Deploy to Production
  - Blue-green deployment (zero downtime)
  - Health checks
  - Automated rollback on failure
```

------------------------------------------------------------------------

## 17. Documentation Strategy

### Repository Structure

    /docs
        Architecture Design Document
        Database Design Document
        API Documentation
        Authentication Document
        Security Document
        Content Ingestion Document
        Payment Integration Document
        Deployment Guide
        Testing Strategy
        Development Plan
        User Manual
        Change Log

### API Documentation

-   Swagger/OpenAPI
-   XML Comments

------------------------------------------------------------------------

## 18. Development Roadmap

### Phase 1: Foundation and Identity

**Duration: 8 weeks \| Team: 2 BE + 1 FE**

-   Clean Architecture
-   Authentication
-   Permission system
-   CMS foundation
-   Documentation setup

### Phase 2: CMS and Membership

**Duration: 6 weeks \| Team: 2 BE + 1 FE**

-   Registration
-   Approval workflow
-   Member profile
-   bKash integration

### Phase 3: Content Ingestion and Conversion

**Duration: 10 weeks \| Team: 2 BE + 2 FE**

-   Book processing
-   Unicode conversion
-   QC system
-   Review workflow

### Phase 4: Digital Library Reader

**Duration: 6 weeks \| Team: 1 BE + 2 FE**

-   Search
-   Reading experience
-   Bookmark
-   History

### Phase 5: AI Research Platform

**Duration: 8 weeks \| Team: 1 BE + 1 AI + 1 FE**

-   MCP integration
-   Knowledge assistant
-   Research tools

### MVP Definition

**Minimum Viable Product = Phase 1 + Phase 2**

-   Working authentication
-   CMS with dynamic pages
-   Membership registration with bKash
-   Admin approval workflow

------------------------------------------------------------------------

## 19. Project Timeline and Resource Plan

### Team Structure

  Role                 Count   Responsibilities
  -------------------- ------- ------------------------------------------
  Backend Developer    2       API, Identity, Persistence, Integrations
  Frontend Developer   1-2     Next.js, CMS, Reader
  QA Engineer          1       Testing automation
  DevOps Engineer      1       CI/CD, Infrastructure
  Content Specialist   1-2     Content ingestion, QC
  Project Manager      1       Coordination

### Phase Timeline Summary

  Phase                  Duration   Key Deliverables
  ---------------------- ---------- ------------------------------------
  1: Foundation          8 weeks    Auth, Permissions, CMS Base
  2: CMS + Membership    6 weeks    Registration, bKash Integration
  3: Content Ingestion   10 weeks   Conversion Pipeline, Review Portal
  4: Digital Library     6 weeks    Reader, Search, Bookmarks
  5: AI Platform         8 weeks    MCP, Research Tools

**Total Estimated Timeline: 38 weeks (\~9-10 months)**

------------------------------------------------------------------------

## 20. Mobile Application (Flutter)

### Phase 1: Reader App

-   **Native Flutter** (not WebView)
-   Authentication (JWT + biometric optional)
-   Book reading with pagination
-   Full-text search
-   Bookmarks and reading history
-   Offline support: cache read books
-   Platforms: iOS 14+ and Android 7+

### Phase 2: Full Member App (Future)

-   All web features
-   Push notifications
-   Membership management
-   Payment integration

------------------------------------------------------------------------

## 21. Multi-Language Strategy

### Supported Languages

-   Bengali (bn) - Primary
-   English (en) - Secondary

### Implementation

-   URL strategy: `/{lang}/page-slug`
-   Translation: Per-page, per-field stored in separate table
-   CMS: Translators can edit translations in admin panel
-   Default: Bengali (site primary language)

------------------------------------------------------------------------

## 22. Scale and Performance Targets

### Expected Volumes

  Metric                       Year 1      Year 3
  ---------------------------- ----------- ------------
  Books                        500         5,000
  Total Pages                  50,000      500,000
  Active Members               1,000       10,000
  Concurrent Users             100         500
  Converted Content Storage    10 GB       100 GB
  Scanned Original Storage\*   20 GB       200 GB
  **Total Storage**            **30 GB**   **300 GB**

\*Scanned originals are retained full-resolution for at least 3 years,
then archived to cold storage.

### Performance Targets

  Operation                        Target
  -------------------------------- --------------
  Page load (public, cached)       \< 2 seconds
  Page load (public, uncached)     \< 3 seconds
  Search response                  \< 500 ms
  Login                            \< 1 second
  Book load (with CDN)             \< 3 seconds
  API response (95th percentile)   \< 200 ms

------------------------------------------------------------------------

## 23. Risks and Mitigation

  -----------------------------------------------------------------------
  Risk              Impact            Probability       Mitigation
  ----------------- ----------------- ----------------- -----------------
  bKash API changes High              Medium            Abstract payment
                                                        layer; multiple
                                                        gateway support

  Bijoy conversion  High              Medium            Human review
  errors                                                step; automated
                                                        QC; side-by-side
                                                        diff viewer

  Staff training    Medium            High              Early training;
  gap                                                   detailed user
                                                        manual

  Content copyright High              Low               Legal review;
  issues                                                permission
                                                        documentation

  **Data protection **High**          **Medium**        **Legal review
  / PII                                                 during Phase 2;
  compliance**                                          data retention
                                                        policy;
                                                        encryption at
                                                        rest;
                                                        GDPR/Bangladesh
                                                        Data Protection
                                                        Act assessment**

  Technology        Medium            Low               Clean
  changes                                               Architecture;
                                                        abstraction
                                                        layers

  Cache             Medium            Medium            Redis Pub/Sub;
  inconsistency                                         SignalR
                                                        invalidation

  Webhook delivery  High              Medium            Dead-letter
  failure                                               queue; manual
                                                        reconciliation
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 24. Final Goal

The One Project will become:

-   A secure digital archive
-   A membership platform
-   A knowledge management system
-   A research platform
-   An AI-ready knowledge ecosystem

**The principle:**

*Preserve original knowledge while providing modern access, management,
and research capabilities.*

------------------------------------------------------------------------

## 📋 Document Checklist

  Document                       Status       Location
  ------------------------------ ------------ ----------------------------
  Architecture Design Document   ✅ Planned   /docs/architecture.md
  Database Design Document       ✅ Planned   /docs/database.md
  API Documentation              ✅ Planned   Swagger + XML
  Authentication Document        ✅ Planned   /docs/authentication.md
  Security Document              ✅ Planned   /docs/security.md
  Content Ingestion Document     ✅ Planned   /docs/content-ingestion.md
  Payment Integration Document   ✅ Planned   /docs/payment.md
  Deployment Guide               ✅ Planned   /docs/deployment.md
  Testing Strategy               ✅ Planned   /docs/testing.md
  Development Plan               ✅ Planned   /docs/development-plan.md
  User Manual                    ✅ Planned   /docs/user-manual.md
  Change Log                     ✅ Planned   /docs/changelog.md

------------------------------------------------------------------------

## 🚀 Immediate Next Steps

1.  **Create Database ERD** - Draw.io or similar (2-3 days)
2.  **Define OpenAPI Specification** - All Phase 1 endpoints (2-3 days)
3.  **Setup GitHub Repository** - CI/CD pipeline (1-2 days)
4.  **Create .env.example** - All required environment variables (1 day)
5.  **Setup Docker Compose** - Local development environment (1-2 days)
6.  **Select Cloud Provider** - Azure/AWS/Regional hosting (1 day)

------------------------------------------------------------------------

## Final Architecture Decisions

### Implementation Baseline

This document is the approved baseline for implementation. Detailed
technical decisions will be maintained in separate module documents
under `/docs`.

### Architecture Governance

Any major change to: - Technology stack - Security model - Data model -
External integrations - Deployment approach

must be recorded through an Architecture Decision Record (ADR).

------------------------------------------------------------------------

**Document Version:** Final v1.0\
**Status:** ✅ Ready for Implementation\
**Last Updated:** \[Current Date\]

*This document is the master architecture plan. Detailed implementation
specifications belong in the per-module documents listed in Section 17.*
