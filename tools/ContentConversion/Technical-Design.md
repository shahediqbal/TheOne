# Content Conversion — Technical Design

Expands the tool `README.md`. Covers the Bijoy-to-Unicode conversion pipeline implementation, backing `TheOne.Worker`.

## Pipeline Stages (implementation detail)

```
Upload -> Text Extraction -> Encoding Detection -> Bijoy-to-Unicode Conversion
       -> Automatic QC -> Human Review (Diff Viewer) -> Admin Approval -> Publication
```

### 1. Upload

- Accepted formats: PDF, DOC, DOCX, Bijoy/SutonnyMJ documents, scanned images (JPEG/PNG/TIFF within a PDF or standalone).
- File saved to object storage first (raw, untouched); a `ContentIngestionJob` row is created referencing the storage key. The Worker never processes an in-request upload synchronously — it's queued.

### 2. Text Extraction

| Source Type | Extraction Approach |
|---|---|
| PDF (text layer present) | Direct text extraction (e.g., PDF text-layer parser) |
| PDF (scanned, no text layer) | OCR pass required before extraction |
| DOC/DOCX | Direct document-object text extraction |
| Scanned image | OCR (Bangla-capable OCR engine) |

Output at this stage is **raw extracted text**, not yet Unicode-normalized — it may still be in legacy Bijoy/ANSI encoding.

### 3. Encoding Detection

- Heuristic + byte-pattern detection distinguishes: already-Unicode text, Bijoy/ANSI legacy encoding, SutonnyMJ legacy encoding, mixed/garbled input.
- Detection confidence below a threshold routes the job to a **manual encoding-confirmation queue** rather than guessing — an incorrect encoding guess silently corrupts the entire document, which is worse than a short human-confirmation delay.

### 4. Bijoy to Unicode Conversion

- Character-mapping table converts legacy glyph sequences to their Unicode Bengali codepoint equivalents (this is a well-understood mapping problem — legacy Bangla fonts encode glyphs, not Unicode graphemes, so the mapping table is the core asset here and should be maintained/versioned independently of the pipeline code).
- Conversion runs as a Python worker task (per the stack's "Python workers" choice), keeping the mapping-table logic separate from the .NET orchestration layer — the .NET `TheOne.Worker` project queues/orchestrates; the Python service does the actual character transformation.
- Output: Unicode Bengali text, stored as a new `ContentVersion` in `Draft` status.

### 5. Automatic Quality Check

Each check produces a score component; combined into the `QualityScore` stored on the version.

| Check | Method |
|---|---|
| Word count comparison | Extracted-text word count vs. converted-text word count; flags >X% deviation |
| Paragraph comparison | Paragraph/line-break count comparison; flags structural loss |
| Character loss detection | Byte-level diff against expected mapping-table coverage; flags unmapped/dropped characters |
| Encoding validation | Confirms output is valid UTF-8 Bengali codepoints, not leftover legacy bytes |
| Missing content detection | Compares extracted page/section count against converted output's structure |

A version scoring below a configured threshold is routed straight to Human Review with QC findings attached, rather than silently queued behind higher-confidence conversions.

### 6. Human Review (Diff Viewer)

- Reviewer sees the side-by-side original-vs-converted view (per the Frontend Architecture's review-UI module) plus the QC score and per-check breakdown.
- Actions: **Approve** (promotes version to `Approved`, becomes `ContentBlock.CurrentVersionId`), **Reject** (returns to conversion queue with reviewer notes — may trigger a re-run with adjusted encoding/mapping assumptions), **Edit** (reviewer corrects text inline before approving — the edit itself is captured as part of that version's data, not a silent overwrite).

### 7. Admin Approval & Publication

- A second-tier Approval gate (per the Role & Permission Matrix — Content Reviewer approves the conversion; publication to the public Library may require a separate Admin/Approve action depending on final workflow configuration) moves the `ContentBlock` to `Published`.
- Publication triggers the cache-invalidation event for that book's content cache (per the cache-invalidation triggers already defined).

## Error Handling & Retry

- Extraction/OCR failures retry up to 3 times with backoff before landing in a `Failed` state visible in the Content Specialist's queue — not silently dropped.
- A conversion job carries the original file reference throughout, so any stage can be re-run from scratch without re-upload.

## Versioning & Storage Tie-In

- Every approved edit produces a new `ContentVersion` row (per `Database-Detailed-Design.md`), capped at 5 retained per `ContentBlock`.
- Original scans are retained per the resolution/retention policy already defined (full-resolution retained ≥3 years, then cold storage) — the conversion pipeline never deletes the source scan on its own; retention is a separate scheduled job.

## Technology Choices

- **Orchestration:** `TheOne.Worker` (.NET) — queues jobs, tracks state, calls the Python conversion service, applies QC results, writes to PostgreSQL.
- **Conversion engine:** Python service with the Bijoy/SutonnyMJ-to-Unicode mapping table, invoked as an internal service call (not exposed publicly).
- **OCR:** a Bangla-capable OCR engine for scanned sources — selection and licensing to be confirmed during Phase 3 implementation; this document assumes OCR as a pluggable stage, not tied to a specific vendor.
- **Queue:** background job queue (e.g., a .NET hosted service with a database-backed or Redis-backed queue) — matches the Worker project's role in the solution architecture.
