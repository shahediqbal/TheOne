using System.Buffers.Binary;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
namespace TheOne.Persistence.Membership;

public sealed class MembershipPhotos(TheOneDbContext db, IMembershipTransaction transaction) : IMembershipPhotos
{
    public static string ValidateImage(byte[] data)
    {
        if (data.Length == 0 || data.Length > 10 * 1024 * 1024) throw new ValidationException("Photo must be a JPEG or PNG up to 10 MB.");
        int width = 0, height = 0; string? type = null;
        if (data.Length >= 45 && data.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) &&
            data.AsSpan(12,4).SequenceEqual("IHDR"u8) && data.AsSpan(data.Length - 8,4).SequenceEqual("IEND"u8))
        {
            width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16,4));
            height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20,4)); type = "image/png";
        }
        else if (data.Length > 10 && data[0] == 255 && data[1] == 216 && data[^2] == 255 && data[^1] == 217)
        {
            var i = 2;
            while (i + 4 < data.Length && data[i] == 255)
            {
                while (i < data.Length && data[i] == 255) i++;
                if (i >= data.Length) break;
                int marker = data[i++];
                if (marker == 218 || marker == 217) break;
                if (marker == 1 || marker is >= 208 and <= 215) continue;
                if (i + 2 > data.Length) break;
                int length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i,2));
                if (length < 2 || i + length > data.Length) break;
                if (marker is 192 or 193 or 194 && length >= 8)
                { height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i+3,2)); width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i+5,2)); type = "image/jpeg"; break; }
                i += length;
            }
        }
        if (type is null || width <= 0 || height <= 0 || width > 6000 || height > 6000 || (long)width * height > 25000000)
            throw new ValidationException("Use a JPEG or PNG photo no larger than 6000 pixels per side or 25 megapixels.");
        return type;
    }
    public async Task SaveAsync(string referenceCode, string contactNumber, string resumeToken, byte[] content, CancellationToken ct)
    {
        await transaction.ExecuteAsync(referenceCode, async () =>
        {
            var a = await db.MembershipApplications.SingleOrDefaultAsync(x => x.ReferenceCode == referenceCode, ct)
                ?? throw new MembershipApplicationNotFoundException();
            if (!MembershipResumeCredential.Matches(resumeToken, a.ResumeTokenHash) || a.ContactNumber != contactNumber)
                throw new MembershipApplicationNotFoundException();
            if (a.Status != MembershipApplicationStatus.Draft) throw new MembershipApplicationNotEditableException();
            var contentType = ValidateImage(content);
            var photo = await db.MembershipPhotos.SingleOrDefaultAsync(x => x.ApplicationId == a.Id, ct);
            if (photo is null) { photo = new MembershipPhoto { ApplicationId = a.Id }; db.MembershipPhotos.Add(photo); }
            photo.Content = content; photo.ContentType = contentType; photo.UploadedAtUtc = DateTime.UtcNow;
            a.PhotoUrl = "uploaded"; a.ModifiedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct); return true;
        }, ct);
    }
    public async Task<MembershipPhotoResponse> ReadAsync(string referenceCode, CancellationToken ct)
    {
        var photo = await db.MembershipPhotos.AsNoTracking().Where(p => db.MembershipApplications.Any(a => a.Id == p.ApplicationId && a.ReferenceCode == referenceCode))
            .Select(p => new MembershipPhotoResponse(p.Content, p.ContentType)).SingleOrDefaultAsync(ct);
        return photo ?? throw new MembershipApplicationNotFoundException();
    }
}
