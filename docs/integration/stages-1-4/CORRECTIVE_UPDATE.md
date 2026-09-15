# Corrective update to Stages 1–4

This cumulative ZIP supersedes the previous source ZIP. Extract into a new folder; do not layer older ZIPs on top. Read INTEGRATION_DEPLOYMENT_GUIDE.md for integration, and PENDING_WORK.md for the deferred work list.

## Code and documentation changes

- MembershipFormDefinition now declares all 28 required fields with nameof and typed entity accessors. Its existing public field-name list is derived from those definitions in the same order.
- MembershipSubmissionPolicy explicitly maps all 41 persisted form fields into the save validator. ResumeToken is only a validator placeholder after credential verification; FieldTranslationSources remains a request-only map with a separate stored provenance representation, matching previous behaviour.
- SaveMembershipSectionRequestValidator uses 34 typed maximum-length rules, preserving the previous limits. There is no runtime reflection in the submission policy or its validator. Field renames in accessors/mappings now require corresponding code changes to compile.
- MembershipSubmissionPolicyTests probes each required field and persisted text field, validates a complete application and checks bad stored numeric/enum values and stale configuration. Reflection is used only in these tests to discover future DTO drift, not to implement production validation.
- WebsiteRules.cs and WebsiteEntities.cs have consistent spacing/indentation. Whitespace-independent token comparison against the preceding source passed.
- The membership workflow SVG now reflects no payment requirement at submission, manual cash/bKash recording and the BDT 100 MembershipFee approval gate. Rejection branches and the separate member lifecycle are shown. The SVG was rendered and visually inspected.

## Verification and limits

Passed: static source/project checks for 50 selected C# files, mapping coverage for 41 stored fields/34 string-length rules/28 required accessors, formatting token equivalence, SVG render/inspection and ZIP integrity.

The .NET SDK is still unavailable here: the new C# tests, backend build, EF migrations and PostgreSQL behaviour have NOT been executed. Run Verify-Cms.ps1 locally. Frontend code was not changed; the preceding successful two builds and 19 DOM tests remain the last frontend results and were not rerun for this backend/documentation update. No claim of 30/30 backend tests is made.

No schema migration is introduced by this update. Existing migrations still require the local gate. Missing semantic-search generation/indexing, monthly-dues rules, bootstrap/recovery and Stage 5/6 work are listed explicitly in PENDING_WORK.md.
