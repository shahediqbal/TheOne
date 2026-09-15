namespace TheOne.Application.Membership;
public sealed record MembershipPhotoResponse(byte[] Content, string ContentType);
public interface IMembershipPhotos
{
    Task SaveAsync(string referenceCode, string contactNumber, string resumeToken, byte[] content, CancellationToken ct);
    Task<MembershipPhotoResponse> ReadAsync(string referenceCode, CancellationToken ct);
}
