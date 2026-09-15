namespace TheOne.Domain.Entities;
public enum BlogStatus { Draft, InReview, Approved, Published, Archived }
public sealed class BlogPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
}
public sealed class BlogPostTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BlogPostId { get; set; }
    public string Language { get; set; } = "bn";
    public BlogStatus Status { get; set; }
    public int Version { get; set; }
    public string DraftJson { get; set; } = "{}";
    public Guid? PublishedRevisionId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class BlogRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TranslationId { get; set; }
    public int Version { get; set; }
    public string DocumentJson { get; set; } = "{}";
    public string Action { get; set; } = "save";
    public Guid ActorId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
// Published slugs remain reserved, so renames can redirect without collisions or ownership changes.
public sealed class BlogSlug
{
    public string Language { get; set; } = "bn";
    public string Slug { get; set; } = "";
    public Guid TranslationId { get; set; }
}
