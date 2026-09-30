# Deployment preparation

Start with [Portable hosting guide](PORTABLE-HOSTING.md).

- Linux/Coolify: Dockerfiles in docker/, repository-root build context.
- Windows/IIS: Publish-Windows.ps1 and conditional provider templates in iis/.
- Environment examples: env/. Supply actual secrets through the host settings.
- Staging values and remaining resource setup: [Staging handover](STAGING-HANDOVER.md), with templates in env/staging/.
- Local validation: [2026-09-30 results](VALIDATION-2026-09-30.md).

No deployment or database migration is automatic. Hosting runtime checks and staging approval are still required.
