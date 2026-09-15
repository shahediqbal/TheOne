using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TheOne.Application.Membership;
using TheOne.Application.Membership.Validators;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;

namespace TheOne.Persistence.Membership;

public sealed class MembershipManagement(TheOneDbContext db, IMembershipTransaction transaction,
    IMembershipApplicationStore applications, MembershipSubmissionPolicy policy) : IMembershipManagement
{
    public async Task<MembershipPage> ListAsync(MembershipQuery request, CancellationToken ct)
    {
        if (request.Page < 1 || request.Page > 100000 || request.PageSize < 1 || request.PageSize > 100 ||
            request.Search?.Length > 200 || request.Status is { } status && !Enum.IsDefined(status))
            throw new ValidationException("Invalid page, search or status filter.");
        var q = db.MembershipApplications.AsNoTracking();
        if (request.Status is { } state) q = q.Where(x => x.Status == state);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            q = q.Where(x => x.ReferenceCode.Contains(search) || (x.FullNameBn != null && x.FullNameBn.Contains(search)) ||
                (x.FullNameEn != null && x.FullNameEn.ToLower().Contains(search.ToLower())) ||
                (x.ContactNumber != null && x.ContactNumber.Contains(search)) ||
                db.Members.Any(m => m.ApplicationId == x.Id && m.MembershipNumber != null && m.MembershipNumber.Contains(search)));
        }
        var count = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new MembershipListItem(x.Id, x.ReferenceCode, x.FullNameBn, x.FullNameEn,
                x.ContactNumber, x.Status, x.CreatedAtUtc,
                db.Members.Where(m => m.ApplicationId == x.Id).Select(m => m.MembershipNumber).FirstOrDefault()))
            .ToListAsync(ct);
        return new MembershipPage(items, count, request.Page, request.PageSize);
    }
    public async Task<MembershipDetail> DetailAsync(string referenceCode, CancellationToken ct)
    {
        var a = await db.MembershipApplications.AsNoTracking().SingleOrDefaultAsync(x => x.ReferenceCode == referenceCode, ct)
            ?? throw new MembershipApplicationNotFoundException();
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationId == a.Id, ct);
        MemberResponse? response = null;
        if (member is not null)
        {
            var history = await db.Contributions.AsNoTracking().Where(x => x.MemberId == member.Id)
                .OrderByDescending(x => x.RecordedAtUtc).ThenBy(x => x.Id)
                .Select(x => new ContributionResponse(x.Id, x.Type, x.Amount, x.Method, x.TransactionReference, x.Note, x.RecordedAtUtc)).ToListAsync(ct);
            response = new(member.Id, member.ApplicationId, member.MembershipNumber, member.Status, history);
        }
        return new(MembershipApplicationService.ToResponse(a), response, a.VerifiedBy, a.VerifiedAtUtc,
            a.ApprovedBy, a.ApprovedAtUtc, a.RejectedBy, a.RejectedAtUtc, a.RejectionReason, member?.EntryChannel, member?.EnteredByStaffId);
    }
    public Task<MembershipDetail> EnterAsync(Guid staffId, OperatorMembershipRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync("operator:" + request.RequestId, async () =>
        {
            if (request.RequestId == Guid.Empty || request.Fields is null || !request.CodeOfConductAccepted ||
                !request.DeclarationAccepted || !request.OathAccepted || request.ConsentVersion != MembershipFormDefinition.Version)
                throw new ValidationException("A request ID, application details and recorded conduct acceptance are required.");
            byte[] photoContent;
            try {
                if (string.IsNullOrWhiteSpace(request.PhotoBase64)) throw new ValidationException("Photo is required.");
                if (request.PhotoBase64.Length > 14 * 1024 * 1024) throw new ValidationException("Photo is too large.");
                photoContent = Convert.FromBase64String(request.PhotoBase64);
            } catch (FormatException) { throw new ValidationException("Invalid photo encoding."); }
            var photoType = MembershipPhotos.ValidateImage(photoContent);
            // The client keeps this request ID across retries; identical retries return the existing entry; changed details conflict.
            var existing = await db.Members.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationId == request.RequestId, ct);
            if (existing is not null)
            {
                if (existing.EnteredByStaffId != staffId) throw new MembershipConflictException("This request ID is already in use.");
                var recorded = await db.MembershipApplications.AsNoTracking().SingleAsync(x => x.Id == request.RequestId, ct);
                foreach (var source in typeof(SaveMembershipSectionRequest).GetProperties())
                {
                    var target = typeof(MembershipApplication).GetProperty(source.Name);
                    if (target is null || source.Name == "PhotoUrl") continue;
                    var expected = source.GetValue(request.Fields);
                    if (expected is null && target.PropertyType.IsValueType && Nullable.GetUnderlyingType(target.PropertyType) is null)
                        expected = Activator.CreateInstance(target.PropertyType);
                    if (!Equals(target.GetValue(recorded), expected))
                        throw new MembershipConflictException("This request was already saved with different details. Open the recorded application before continuing.");
                }
                var savedPhoto = await db.MembershipPhotos.SingleAsync(x => x.ApplicationId == request.RequestId, ct);
                if (!savedPhoto.Content.AsSpan().SequenceEqual(photoContent)) throw new MembershipConflictException("This request was already saved with a different photo.");
                return await DetailAsync(recorded.ReferenceCode, ct);
            }
            var fields = request.Fields;
            fields.ResumeToken = MembershipResumeCredential.Create();
            await new StartMembershipApplicationRequestValidator().ValidateAndThrowAsync(new()
                { FullNameBn = fields.FullNameBn ?? "", FullNameEn = fields.FullNameEn ?? "", ContactNumber = fields.ContactNumber }, ct);
            await new SaveMembershipSectionRequestValidator().ValidateAndThrowAsync(fields, ct);
            var application = new MembershipApplication { Id = request.RequestId,
                ReferenceCode = await applications.NextReferenceCodeAsync(DateTime.UtcNow.Year, ct),
                ResumeTokenHash = MembershipResumeCredential.Hash(fields.ResumeToken) };
            // Only explicitly supported form properties are copied; lifecycle/identity/decision fields are absent from this DTO.
            foreach (var source in typeof(SaveMembershipSectionRequest).GetProperties())
            {
                var target = typeof(MembershipApplication).GetProperty(source.Name);
                if (target is not null && source.Name != "PhotoUrl" && source.GetValue(fields) is { } value &&
                    (target.PropertyType == source.PropertyType || Nullable.GetUnderlyingType(source.PropertyType) == target.PropertyType))
                    target.SetValue(application, value);
            }
            application.TranslationProvenance = System.Text.Json.JsonSerializer.Serialize(
                typeof(SaveMembershipSectionRequest).GetProperties()
                    .Where(x => x.Name.EndsWith("En") && x.GetValue(fields) is string value && !string.IsNullOrWhiteSpace(value))
                    .ToDictionary(x => x.Name[..^2], _ => TranslationSource.UserTyped));
            application.PhotoUrl = "uploaded";
            policy.Validate(application);
            application.DeclarationAccepted = true;
            application.OathAccepted = true;
            application.ConsentVersion = MembershipFormDefinition.Version;
            application.ConsentAcceptedAtUtc = DateTime.UtcNow;
            application.CodeOfConductAccepted = true;
            application.Status = MembershipApplicationStatus.Submitted;
            application.SubmittedAtUtc = DateTime.UtcNow;
            application.ModifiedAtUtc = application.SubmittedAtUtc;
            db.MembershipApplications.Add(application);
            db.MembershipPhotos.Add(new MembershipPhoto { ApplicationId = application.Id, Content = photoContent, ContentType = photoType });
            db.Members.Add(new Member { ApplicationId = application.Id, EntryChannel = MembershipEntryChannel.Operator,
                EnteredByStaffId = staffId, Status = MemberStatus.PendingApproval });
            await db.SaveChangesAsync(ct);
            return await DetailAsync(application.ReferenceCode, ct);
        }, ct);
}
