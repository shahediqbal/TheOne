namespace TheOne.Domain.Entities;

/// <summary>CMS content, deliberately separate from permission-bearing staff navigation.</summary>
public sealed class WebsiteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CanonicalId { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Kind { get; set; } = "Page";
    public string Language { get; set; } = "bn";
    public Guid? LinkedStaffId { get; set; }
    public BlogStatus Status { get; set; }
    public int Version { get; set; }
    public string DraftJson { get; set; } = "{}";
    public Guid? PublishedRevisionId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WebsiteRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordId { get; set; }
    public int Version { get; set; }
    public string DocumentJson { get; set; } = "{}";
    public string Action { get; set; } = "save";
    public Guid ActorId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WebsiteSlug
{
    public string Kind { get; set; } = "Page";
    public string Language { get; set; } = "bn";
    public string Slug { get; set; } = "";
    public Guid RecordId { get; set; }
}

/// <summary>Immutable association on a saved revision: pending author changes do not alter the public article.</summary>
public sealed class BlogRevisionAuthor
{
    public Guid RevisionId { get; set; }
    public Guid AuthorId { get; set; }
    public int Position { get; set; }
}

public sealed class PublishingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PayloadJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
}

public sealed class WebsiteAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
    public string OriginalName { get; set; } = "";
    public Guid UploadedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class BlogEmbedding
{
    public Guid TranslationId { get; set; }
    public Guid RevisionId { get; set; }
    public string Model { get; set; } = "";
    public string EmbeddingData { get; set; } = "[]";
    public int Dimensions { get; set; } = 384;
}
