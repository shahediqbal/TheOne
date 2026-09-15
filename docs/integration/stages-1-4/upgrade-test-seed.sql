INSERT INTO "AspNetUsers" ("Id","FullName","IsActive","CreatedAtUtc","Email","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount","PasswordHash","SecurityStamp")
VALUES ('99000000-0000-4000-8000-000000000002','Upgrade fixture only',true,now(),'upgrade@example.test',true,true,true,true,0,'non-login-fixture-hash','fixture-stamp');
INSERT INTO "AspNetUserRoles" ("UserId","RoleId") VALUES ('99000000-0000-4000-8000-000000000002',(SELECT "Id" FROM "AspNetRoles" WHERE "NormalizedName"='SUPERADMIN'));
INSERT INTO "AspNetRoleClaims" ("RoleId","ClaimType","ClaimValue") VALUES ((SELECT "Id" FROM "AspNetRoles" WHERE "NormalizedName"='SUPERADMIN'),'permission','audit.read');
UPDATE "NavigationMenus" SET "LabelEn"='Existing custom users',"Enabled"=false WHERE "Route"='/administration/users';
INSERT INTO "NavigationMenuRoles" ("MenuId","RoleId") VALUES ('71000000-0000-4000-8000-000000000001',(SELECT "Id" FROM "AspNetRoles" WHERE "NormalizedName"='SUPERADMIN')) ON CONFLICT DO NOTHING;
INSERT INTO "SecurityAuditEvents" ("Id","CreatedAtUtc","Action","Target","Details") VALUES ('99000000-0000-4000-8000-000000000003',now(),'Upgrade.Fixture','isolated','{}');
CREATE TABLE "IntegrationBaseline" AS
SELECT 'users' AS kind, row_to_json(t)::text AS payload FROM "AspNetUsers" t
UNION ALL SELECT 'roles',row_to_json(t)::text FROM "AspNetRoles" t
UNION ALL SELECT 'claims',row_to_json(t)::text FROM "AspNetRoleClaims" t
UNION ALL SELECT 'assignments',row_to_json(t)::text FROM "AspNetUserRoles" t
UNION ALL SELECT 'menus',row_to_json(t)::text FROM "NavigationMenus" t
UNION ALL SELECT 'menuRoles',row_to_json(t)::text FROM "NavigationMenuRoles" t
UNION ALL SELECT 'audit',row_to_json(t)::text FROM "SecurityAuditEvents" t;
