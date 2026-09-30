# Interactive family tree

Location: Life & Teachings, /bn/life and /en/life.

The tree is now a compact two-tab HTML layout: Paternal / Maternal. All currently entered siblings and children are visible immediately. There is no search, zoom, collapse control, pop-up, internal scroll box or horizontal canvas. Responsive grids wrap names on small screens; normal page scrolling can still be necessary on phones. Keyboard arrows switch tabs. Both tabs share the same stable person records.

## Correcting information
Edit packages/website/family-data.ts. Each person has an id, bn/en names, optional dates, optional spouse ID and a list of child IDs. Keep IDs unique and stable, verify every relationship, and never create circular child relationships. Both tabs share records, so changing a person updates both views. Earlier unclear generations are explicitly grouped as pending, not fabricated as individuals. English names are provisional transliterations. Later descendants and unclear spouses/dates have deliberately not been guessed.

This initial version is file-backed. A dedicated staff form/database workflow for family relationships has NOT been added; the existing CMS lineage records remain separately rendered and unchanged. Family genealogy is not automatically classified as spiritual succession. No database migrations or data changes were made.

Checks: 45 website tests pass, including immediately visible descendants, switching parents and keyboard/Bangla tabs. TypeScript check passed. No application servers were started for this work.
