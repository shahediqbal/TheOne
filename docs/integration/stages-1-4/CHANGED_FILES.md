# Cumulative delivery — final Stage 4 additions

This ZIP contains all Stages 1–4 source; it is not a patch. The list below identifies the final completion pass relative to the earlier blog-only Stage 4 ZIP. All previous membership, staff, public form and blog changes are retained. Original uploaded project documentation is preserved as historical material.

## Added

- `CMS_API_GUIDE.md`
- `INTEGRATION_DEPLOYMENT_GUIDE.md`
- `TheOne/apps/management/src/content/BlocksEditor.tsx`
- `TheOne/apps/management/src/content/WebsiteTypes.ts`
- `TheOne/apps/management/src/pages/Website.test.tsx`
- `TheOne/apps/management/src/pages/Website.tsx`
- `TheOne/deployment/sql/enable-blog-pgvector.sql`
- `TheOne/scripts/Verify-Cms.ps1`
- `TheOne/src/TheOne.Application/Blog/BlogSearchContracts.cs`
- `TheOne/src/TheOne.Application/Website/WebsiteContracts.cs`
- `TheOne/src/TheOne.Application/Website/WebsiteRules.cs`
- `TheOne/src/TheOne.Domain/Entities/WebsiteEntities.cs`
- `TheOne/src/TheOne.IntegrationTests/WebsiteTests.cs`
- `TheOne/src/TheOne.Persistence/Blog/BlogSearch.cs`
- `TheOne/src/TheOne.Persistence/Configurations/WebsiteConfiguration.cs`
- `TheOne/src/TheOne.Persistence/Migrations/20260914150000_CompleteWebsiteCms.Designer.cs`
- `TheOne/src/TheOne.Persistence/Migrations/20260914150000_CompleteWebsiteCms.cs`
- `TheOne/src/TheOne.Persistence/Website/WebsiteService.cs`
- `TheOne/src/TheOne/Controllers/WebsiteController.cs`

## Updated

- `PROJECT_DECISIONS.md`
- `README.md`
- `TheOne/apps/management/README.md`
- `TheOne/apps/management/src/App.tsx`
- `TheOne/apps/management/src/api.ts`
- `TheOne/apps/management/src/pages/Blog.tsx`
- `TheOne/deployment/README.md`
- `TheOne/packages/blog/types.ts`
- `TheOne/src/TheOne.Application/Administration/AdministrationContracts.cs`
- `TheOne/src/TheOne.Application/Blog/BlogContracts.cs`
- `TheOne/src/TheOne.Persistence/Blog/BlogService.cs`
- `TheOne/src/TheOne.Persistence/Data/TheOneDbContext.cs`
- `TheOne/src/TheOne.Persistence/DependencyInjection.cs`
- `TheOne/src/TheOne.Persistence/Migrations/TheOneDbContextModelSnapshot.cs`
- `TheOne/src/TheOne/Controllers/BlogController.cs`
- `TheOne/src/TheOne/appsettings.json`
- `VERIFICATION.md`
- `verification/check_sources.py`

## Subsequent corrective update

- `TheOne/src/TheOne.Application/Membership/MembershipFormDefinition.cs`: typed required-field definitions.
- `TheOne/src/TheOne.Application/Membership/MembershipSafety.cs`: explicit stored-field mapping.
- `TheOne/src/TheOne.Application/Membership/Validators/MembershipRequestValidators.cs`: typed length rules.
- `TheOne/src/TheOne.IntegrationTests/MembershipSubmissionPolicyTests.cs`: regression cases.
- `TheOne/src/TheOne.Application/Website/WebsiteRules.cs` and `TheOne/src/TheOne.Domain/Entities/WebsiteEntities.cs`: formatting only.
- `TheOne/docs/02-Architecture/diagrams/06_membership_registration_workflow.svg`: corrected flow.
- `verification/check_membership_mapping.py`: static mapping coverage.
- `CORRECTIVE_UPDATE.md`, `PENDING_WORK.md`, README, verification and scope notes updated.
