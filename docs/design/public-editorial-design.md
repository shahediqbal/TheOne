# Public editorial design — 28 September 2026

Branch: feature/public-editorial-design

## Implemented
- Black, grey and white public theme, locally hosted Baloo Da 2 (Latin and Bengali), responsive sticky navigation, skip link, keyboard menu controls.
- Editorial home layout and selected bilingual biography from the legacy site, with its actual portrait. Existing published CMS home/life content takes priority. The default biography is static starter text, not an unpublished CMS record.
- Public Staff login link and management View website link; no tokens passed between applications. Authentication/MFA/permissions are unchanged.
- Website editor: create/select a Draft Page, choose Home or Mawla’s life in the starter-text selector, edit, Save draft, review AI-assisted text, and use the existing approval/publishing workflow. Loading a starter does not save or publish; replacing draft text asks first.

## Content sources and exclusions
Biography and portrait: https://sadarmawla.org/en/sadar-uddin-ahmad-chisty/bio/
Portrait source: https://sadarmawla.org/wp-content/uploads/2022/10/204786782_322339432870218_2602062672854243825_n.jpg
Short English/Bangla biography is an editorial paraphrase for review. No quotations of teachings were invented.
Font: https://github.com/google/fonts/tree/main/ofl/balooda2 (OFL license included with font).

Only the selected biography/portrait and existing section labels were carried forward in this pass. This is not a complete legacy archive migration. Gambling, ghostwriting, advertising, shop/cart and other unrelated legacy links were not imported. No broad homepage scrape was used. Existing CMS records were not altered.

## Editing and configuration
Shared starter copy: packages/website/starter-content.ts.
Home sections: apps/website/app/[lang]/page.tsx.
CSS: apps/website/app/globals.css.
Public app: MANAGEMENT_PUBLIC_ORIGIN sets the staff URL (development default https://localhost:5173). Production fallback /staff requires a reverse-proxy route, or configure the actual staff HTTPS origin.
Staff app: VITE_WEBSITE_PUBLIC_ORIGIN sets the public URL (development default http://localhost:3000). Configure the actual HTTPS origin before building for production; fallback / requires a same-origin public site.
Environment examples updated; local secrets/environment files unchanged.

## Validation
Public tests: 42 passing. Staff unit tests: 12 passing.
Both production builds pass (website 24 generated pages).
Real headless Edge check using a separate in-memory HTTP content fixture: English/Bangla desktop (1440px), mobile (390px), local portrait and font load, sticky header, menu open/Escape/link navigation, no horizontal overflow, staff URL, CMS home precedence, no browser JS errors.
This is not a new live PostgreSQL/authentication end-to-end run. No database writes, migrations, deployment or push were performed.

## Local use
1. Keep the existing .NET API running with its usual configuration.
2. In apps/website run npm run dev; open http://localhost:3000/en or /bn.
3. In apps/management run npm run dev; Staff login opens https://localhost:5173.
4. Edit/publish CMS content through Website content as usual. Review the initial copy and contact details before deployment.

## Local HTTPS troubleshooting
The development launcher (npm run dev) exports the existing ASP.NET public development certificate into ignored .certs/aspnet-localhost.pem and starts Next with NODE_EXTRA_CA_CERTS. No private key is exported, no global trust settings are changed, and TLS validation remains enabled. An explicitly supplied NODE_EXTRA_CA_CERTS is respected. Restart npm run dev after certificate renewal. This applies only to development; production still requires a valid trusted API certificate.
