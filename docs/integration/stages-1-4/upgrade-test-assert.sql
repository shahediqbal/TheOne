DO $$
BEGIN
 IF EXISTS (
  SELECT * FROM "IntegrationBaseline"
  EXCEPT (
   SELECT 'users',row_to_json(t)::text FROM "AspNetUsers" t
   UNION ALL SELECT 'roles',row_to_json(t)::text FROM "AspNetRoles" t
   UNION ALL SELECT 'claims',row_to_json(t)::text FROM "AspNetRoleClaims" t
   UNION ALL SELECT 'assignments',row_to_json(t)::text FROM "AspNetUserRoles" t
   UNION ALL SELECT 'menus',row_to_json(t)::text FROM "NavigationMenus" t
   UNION ALL SELECT 'menuRoles',row_to_json(t)::text FROM "NavigationMenuRoles" t
   UNION ALL SELECT 'audit',row_to_json(t)::text FROM "SecurityAuditEvents" t
  )
 ) THEN RAISE EXCEPTION 'Existing auth/menu/audit rows changed during upgrade'; END IF;
 IF (SELECT count(*) FROM "NavigationMenus" WHERE "Route" IN ('/membership','/blog','/website')) <> 3 THEN RAISE EXCEPTION 'New staff menus missing or duplicated'; END IF;
 IF (SELECT count(*) FROM "NavigationMenuRoles" WHERE "MenuId"::text LIKE '72000000-%') <> 3 THEN RAISE EXCEPTION 'SuperAdmin staff-menu links missing'; END IF;
 IF (SELECT count(*) FROM "AspNetRoleClaims") <> (SELECT count(*) FROM "IntegrationBaseline" WHERE kind='claims') THEN RAISE EXCEPTION 'Unexpected permission grants'; END IF;
END $$;
SELECT 'Upgrade preserved existing account/MFA fields, grants, assignments, menus and audit; new staff menus linked.' AS result;
