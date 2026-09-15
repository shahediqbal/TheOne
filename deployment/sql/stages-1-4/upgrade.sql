-- Review-only upgrade from TheOne authentication/administration baseline.
-- Stop application writers and back up before any future approved application.
-- Refuse partially installed legacy membership schemas; their photo cleanup needs separate review.
DO $$ BEGIN
 IF to_regclass('"MembershipApplications"') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId"='20260913120000_CompleteMembershipForm')
 THEN RAISE EXCEPTION 'Existing Stage 1 membership data detected. Stop and review a data-preserving reconciliation before applying this script.';
 END IF;
END $$;
START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE SEQUENCE "MembershipReferenceNumbers" START WITH 1 INCREMENT BY 1 NO CYCLE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE SEQUENCE "MembershipNumbers" START WITH 1 INCREMENT BY 1 NO CYCLE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE TABLE "MembershipApplications" (
        "Id" uuid NOT NULL,
        "ReferenceCode" character varying(40) NOT NULL,
        "ResumeTokenHash" character varying(64) NOT NULL,
        "VerifiedBy" uuid,
        "RejectedBy" uuid,
        "RejectedAtUtc" timestamp with time zone,
        "RejectionReason" character varying(1000),
        "Status" integer NOT NULL,
        "UserId" uuid,
        "FullNameBn" character varying(200),
        "FullNameEn" character varying(200),
        "FatherNameBn" character varying(200),
        "FatherNameEn" character varying(200),
        "MotherNameBn" character varying(200),
        "MotherNameEn" character varying(200),
        "ContactNumber" character varying(20),
        "NidNumber" character varying(30),
        "PermanentAddressBn" character varying(1000),
        "PermanentAddressEn" character varying(1000),
        "TemporaryAddressBn" character varying(1000),
        "TemporaryAddressEn" character varying(1000),
        "MaritalStatus" integer,
        "Age" integer,
        "OccupationBn" character varying(200),
        "OccupationEn" character varying(200),
        "BloodGroup" character varying(5),
        "Gender" integer,
        "EducationBn" character varying(200),
        "EducationEn" character varying(200),
        "EmergencyContactNumber" character varying(20),
        "PhotoUrl" character varying(500),
        "PurposeOfJoiningBn" character varying(2000),
        "PurposeOfJoiningEn" character varying(2000),
        "LifeGoalBn" character varying(2000),
        "LifeGoalEn" character varying(2000),
        "SpecialSkillsBn" character varying(2000),
        "SpecialSkillsEn" character varying(2000),
        "HelpCategories" integer NOT NULL,
        "TimeCommitment" integer NOT NULL,
        "ContributionIntent" integer NOT NULL,
        "CurrentChallengeBn" character varying(2000),
        "CurrentChallengeEn" character varying(2000),
        "CodeOfConductAccepted" boolean NOT NULL,
        "CommittedSinceYear" integer,
        "FormalMembershipDate" date,
        "TranslationProvenance" character varying(4000) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "ModifiedAtUtc" timestamp with time zone,
        "SubmittedAtUtc" timestamp with time zone,
        "VerifiedAtUtc" timestamp with time zone,
        "ApprovedAtUtc" timestamp with time zone,
        "ApprovedBy" uuid,
        CONSTRAINT "PK_MembershipApplications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE UNIQUE INDEX "IX_MembershipApplications_ReferenceCode" ON "MembershipApplications" ("ReferenceCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_MembershipApplications_ReferenceCode_ContactNumber" ON "MembershipApplications" ("ReferenceCode", "ContactNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_MembershipApplications_UserId" ON "MembershipApplications" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_MembershipApplications_Status" ON "MembershipApplications" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE TABLE "Members" (
        "Id" uuid NOT NULL,
        "ApplicationId" uuid NOT NULL,
        "EntryChannel" integer NOT NULL,
        "EnteredByStaffId" uuid,
        "MembershipNumber" character varying(40),
        "Status" integer NOT NULL,
        "LinkedUserId" uuid,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "ActivatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Members" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Members_MembershipApplications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "MembershipApplications" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE UNIQUE INDEX "IX_Members_ApplicationId" ON "Members" ("ApplicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE UNIQUE INDEX "IX_Members_MembershipNumber" ON "Members" ("MembershipNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_Members_LinkedUserId" ON "Members" ("LinkedUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_Members_Status" ON "Members" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE TABLE "Contributions" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Amount" numeric(10,2) NOT NULL,
        "Method" integer NOT NULL,
        "TransactionReference" character varying(100),
        "Note" character varying(500),
        "RecordedByStaffId" uuid NOT NULL,
        "RecordedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Contributions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Contributions_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_Contributions_MemberId" ON "Contributions" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE INDEX "IX_Contributions_MemberId_Type" ON "Contributions" ("MemberId", "Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    CREATE UNIQUE INDEX "IX_Contributions_TransactionReference" ON "Contributions" ("TransactionReference") WHERE "TransactionReference" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913090000_AddMembershipModule') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260913090000_AddMembershipModule', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    UPDATE "MembershipApplications" SET "PhotoUrl" = NULL WHERE "Status" = 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "Email" character varying(254);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "OtherHelpBn" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "OtherHelpEn" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "AboutSelfBn" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "AboutSelfEn" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "SignatureName" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "ApplicationDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "DeclarationAccepted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "OathAccepted" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "ConsentAcceptedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    ALTER TABLE "MembershipApplications" ADD "ConsentVersion" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    CREATE TABLE "MembershipPhotos" (
        "ApplicationId" uuid NOT NULL,
        "Content" bytea NOT NULL,
        "ContentType" character varying(30) NOT NULL,
        "UploadedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MembershipPhotos" PRIMARY KEY ("ApplicationId"),
        CONSTRAINT "FK_MembershipPhotos_MembershipApplications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "MembershipApplications" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913120000_CompleteMembershipForm') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260913120000_CompleteMembershipForm', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE TABLE "BlogPosts" (
        "Id" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "CreatedBy" uuid NOT NULL,
        CONSTRAINT "PK_BlogPosts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE TABLE "BlogTranslations" (
        "Id" uuid NOT NULL,
        "BlogPostId" uuid NOT NULL,
        "Language" character varying(2) NOT NULL,
        "Status" integer NOT NULL,
        "Version" integer NOT NULL,
        "DraftJson" jsonb NOT NULL,
        "PublishedRevisionId" uuid,
        "ApprovedBy" uuid,
        "ApprovedAtUtc" timestamp with time zone,
        "PublishedAtUtc" timestamp with time zone,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BlogTranslations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BlogTranslations_BlogPosts_BlogPostId" FOREIGN KEY ("BlogPostId") REFERENCES "BlogPosts" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE UNIQUE INDEX "IX_BlogTranslations_BlogPostId_Language" ON "BlogTranslations" ("BlogPostId", "Language");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE TABLE "BlogRevisions" (
        "Id" uuid NOT NULL,
        "TranslationId" uuid NOT NULL,
        "Version" integer NOT NULL,
        "DocumentJson" jsonb NOT NULL,
        "Action" character varying(30) NOT NULL,
        "ActorId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BlogRevisions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BlogRevisions_BlogTranslations_TranslationId" FOREIGN KEY ("TranslationId") REFERENCES "BlogTranslations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE UNIQUE INDEX "IX_BlogRevisions_TranslationId_Version" ON "BlogRevisions" ("TranslationId", "Version");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE TABLE "BlogSlugs" (
        "Language" character varying(2) NOT NULL,
        "Slug" character varying(180) NOT NULL,
        "TranslationId" uuid NOT NULL,
        CONSTRAINT "PK_BlogSlugs" PRIMARY KEY ("Language", "Slug"),
        CONSTRAINT "FK_BlogSlugs_BlogTranslations_TranslationId" FOREIGN KEY ("TranslationId") REFERENCES "BlogTranslations" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    CREATE INDEX "IX_BlogSlugs_TranslationId" ON "BlogSlugs" ("TranslationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914100000_BlogPublishing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260914100000_BlogPublishing', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "WebsiteRecords" (
        "Id" uuid NOT NULL,
        "CanonicalId" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Kind" character varying(30) NOT NULL,
        "Language" character varying(2) NOT NULL,
        "LinkedStaffId" uuid,
        "Status" integer NOT NULL,
        "Version" integer NOT NULL,
        "DraftJson" jsonb NOT NULL,
        "PublishedRevisionId" uuid,
        "ApprovedBy" uuid,
        "ApprovedAtUtc" timestamp with time zone,
        "PublishedAtUtc" timestamp with time zone,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WebsiteRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WebsiteRecords_AspNetUsers_LinkedStaffId" FOREIGN KEY ("LinkedStaffId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE UNIQUE INDEX "IX_WebsiteRecords_CanonicalId_Language" ON "WebsiteRecords" ("CanonicalId", "Language");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE UNIQUE INDEX "IX_WebsiteRecords_Kind_Language" ON "WebsiteRecords" ("Kind", "Language") WHERE "Kind" = 'SiteSettings';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE INDEX "IX_WebsiteRecords_LinkedStaffId" ON "WebsiteRecords" ("LinkedStaffId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "WebsiteRevisions" (
        "Id" uuid NOT NULL,
        "RecordId" uuid NOT NULL,
        "Version" integer NOT NULL,
        "DocumentJson" jsonb NOT NULL,
        "Action" character varying(30) NOT NULL,
        "ActorId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WebsiteRevisions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WebsiteRevisions_WebsiteRecords_RecordId" FOREIGN KEY ("RecordId") REFERENCES "WebsiteRecords" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE UNIQUE INDEX "IX_WebsiteRevisions_RecordId_Version" ON "WebsiteRevisions" ("RecordId", "Version");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "WebsiteSlugs" (
        "Kind" character varying(30) NOT NULL,
        "Language" character varying(2) NOT NULL,
        "Slug" character varying(180) NOT NULL,
        "RecordId" uuid NOT NULL,
        CONSTRAINT "PK_WebsiteSlugs" PRIMARY KEY ("Kind", "Language", "Slug"),
        CONSTRAINT "FK_WebsiteSlugs_WebsiteRecords_RecordId" FOREIGN KEY ("RecordId") REFERENCES "WebsiteRecords" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE INDEX "IX_WebsiteSlugs_RecordId" ON "WebsiteSlugs" ("RecordId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "WebsiteAssets" (
        "Id" uuid NOT NULL,
        "Content" bytea NOT NULL,
        "ContentType" character varying(30) NOT NULL,
        "OriginalName" character varying(200) NOT NULL,
        "UploadedBy" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WebsiteAssets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "PublishingEvents" (
        "Id" uuid NOT NULL,
        "PayloadJson" jsonb NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "AcknowledgedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_PublishingEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE INDEX "IX_PublishingEvents_AcknowledgedAtUtc_CreatedAtUtc" ON "PublishingEvents" ("AcknowledgedAtUtc", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "BlogRevisionAuthors" (
        "RevisionId" uuid NOT NULL,
        "AuthorId" uuid NOT NULL,
        "Position" integer NOT NULL,
        CONSTRAINT "PK_BlogRevisionAuthors" PRIMARY KEY ("RevisionId", "AuthorId"),
        CONSTRAINT "FK_BlogRevisionAuthors_BlogRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "BlogRevisions" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_BlogRevisionAuthors_WebsiteRecords_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "WebsiteRecords" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE INDEX "IX_BlogRevisionAuthors_AuthorId" ON "BlogRevisionAuthors" ("AuthorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE TABLE "BlogEmbeddings" (
        "TranslationId" uuid NOT NULL,
        "RevisionId" uuid NOT NULL,
        "Model" character varying(100) NOT NULL,
        "EmbeddingData" text NOT NULL,
        "Dimensions" integer NOT NULL,
        CONSTRAINT "PK_BlogEmbeddings" PRIMARY KEY ("TranslationId"),
        CONSTRAINT "FK_BlogEmbeddings_BlogTranslations_TranslationId" FOREIGN KEY ("TranslationId") REFERENCES "BlogTranslations" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_BlogEmbeddings_BlogRevisions_RevisionId" FOREIGN KEY ("RevisionId") REFERENCES "BlogRevisions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    CREATE INDEX "IX_BlogEmbeddings_RevisionId" ON "BlogEmbeddings" ("RevisionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260914150000_CompleteWebsiteCms') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260914150000_CompleteWebsiteCms', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915061634_AddStageStaffNavigation') THEN
    INSERT INTO "NavigationMenus" ("Id", "LabelEn", "LabelBn", "Icon", "Route", "ParentId", "SortOrder", "Enabled", "RequiredPermission")
    SELECT v.id::uuid, v.en, v.bn, v.icon, v.route, NULL, v.position, true, v.permission
    FROM (VALUES
        ('72000000-0000-4000-8000-000000000001','Membership','সদস্যপদ','users','/membership',20,'membership.read'),
        ('72000000-0000-4000-8000-000000000002','Blog publishing','ব্লগ প্রকাশনা','book','/blog',30,'blog.read'),
        ('72000000-0000-4000-8000-000000000003','Website content','ওয়েবসাইটের বিষয়বস্তু','globe','/website',40,'website.read')
    ) AS v(id,en,bn,icon,route,position,permission)
    WHERE NOT EXISTS (SELECT 1 FROM "NavigationMenus" m WHERE m."Route" = v.route OR m."Id" = v.id::uuid);
    INSERT INTO "NavigationMenuRoles" ("MenuId", "RoleId")
    SELECT m."Id", r."Id" FROM "NavigationMenus" m CROSS JOIN "AspNetRoles" r
    WHERE m."Id" IN ('72000000-0000-4000-8000-000000000001','72000000-0000-4000-8000-000000000002','72000000-0000-4000-8000-000000000003')
        AND r."NormalizedName" = 'SUPERADMIN'
    ON CONFLICT DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915061634_AddStageStaffNavigation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260915061634_AddStageStaffNavigation', '10.0.11');
    END IF;
END $EF$;
COMMIT;
