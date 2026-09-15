namespace TheOne.Domain.Entities;
public sealed class MembershipPhoto
{
    public Guid ApplicationId { get; set; }
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
