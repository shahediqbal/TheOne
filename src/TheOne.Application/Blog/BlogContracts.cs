using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using TheOne.Domain.Entities;
namespace TheOne.Application.Blog;
public sealed record BlogImage(string Url, string Alt, string? Caption = null);
public sealed record BlogBlock(string Type, string? Text = null, string? Url = null, string? Alt = null,
    int? Level = null, string? Citation = null, BlogImage[]? Images = null, Guid[]? PostIds = null);
public sealed record BlogDocument(string Title, string Slug, string Summary, string SeoTitle, string SeoDescription,
    BlogBlock[] Blocks, string[] Tags, string Provenance = "human", Guid[]? AuthorIds = null);
public sealed record SaveBlog(int ExpectedVersion, BlogDocument Document);
public sealed record BlogAction(int ExpectedVersion, Guid? RevisionId = null);
public sealed record BlogView(Guid Id, Guid PostId, string Language, BlogStatus Status, int Version, BlogDocument Document,
    Guid? PublishedRevisionId, Guid? ApprovedBy, DateTime? ApprovedAtUtc, DateTime? PublishedAtUtc);
public sealed record PublicBlog(Guid PostId, string Language, BlogDocument Document, DateTime PublishedAtUtc, string? OtherLanguageSlug, IReadOnlyList<PublicAuthor>? Authors = null, IReadOnlyList<PublicSeries>? Series = null);
public sealed record PublicAuthor(Guid Id,string Name,string Biography,Guid? PortraitAssetId);
public sealed record PublicSeries(Guid Id,string Title,string Slug,Guid[] PostIds);
public sealed record BlogPreview(string Token, DateTime ExpiresAtUtc);
public interface IBlogService
{
    Task<IReadOnlyList<BlogView>> ListAsync(int page, CancellationToken ct);
    Task<BlogView> CreateAsync(Guid actor, string language, Guid? postId, CancellationToken ct);
    Task<BlogView> GetAsync(Guid id, CancellationToken ct);
    Task<BlogView> SaveAsync(Guid id, Guid actor, SaveBlog request, CancellationToken ct);
    Task<BlogView> ActAsync(Guid id, Guid actor, string action, BlogAction request, CancellationToken ct);
    Task<IReadOnlyList<BlogRevision>> HistoryAsync(Guid id, CancellationToken ct);
    Task<BlogPreview> PreviewAsync(Guid id, CancellationToken ct);
    Task<BlogDocument> ReadPreviewAsync(string token, CancellationToken ct);
    Task<IReadOnlyList<PublicBlog>> PublicListAsync(string language, int page, CancellationToken ct);
    Task<PublicBlog> PublicByPostAsync(string language, Guid postId, CancellationToken ct);
    Task<PublicBlog> PublicReadAsync(string language, string slug, CancellationToken ct);
}
public static class BlogRules
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string Serialize(BlogDocument document) => JsonSerializer.Serialize(document, Json);
    public static BlogDocument Read(string json) => JsonSerializer.Deserialize<BlogDocument>(json, Json)!;
    public static void Language(string value) { if (value is not ("bn" or "en")) throw new ValidationException("Language must be bn or en."); }
    public static bool SafeUrl(string? value) => value is not null && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo);
    public static void Validate(BlogDocument d)
    {
        if (d is null || string.IsNullOrWhiteSpace(d.Title) || d.Title.Length>200 || d.Summary is null || d.Summary.Length>1000 ||
            d.SeoTitle is null || d.SeoTitle.Length>200 || d.SeoDescription is null || d.SeoDescription.Length>500 ||
            d.Slug is null || d.Slug is "preview" or "post" || d.Slug.Length>180 || !Regex.IsMatch(d.Slug,@"^[\p{L}\p{M}\p{N}]+(?:-[\p{L}\p{M}\p{N}]+)*$") ||
            d.Blocks is null || d.Blocks.Length is <1 or >100 || d.Tags is null || d.Tags.Length>20 ||
            d.Tags.Any(t=>string.IsNullOrWhiteSpace(t)||t.Length>80) ||
            d.Provenance is not ("human" or "ai_assisted_draft" or "ai_assisted_reviewed"))
            throw new ValidationException("Check title, slug, summary, SEO, blocks, tags and provenance.");
        if(d.AuthorIds is {} authors && (authors.Length>12||authors.Any(id=>id==Guid.Empty)||authors.Distinct().Count()!=authors.Length))throw new ValidationException("Select up to twelve distinct authors.");
        if (Serialize(d).Length>200000) throw new ValidationException("Post content is too large.");
        foreach(var b in d.Blocks)
        {
            if(b is null || b.Type is not ("Paragraph" or "Heading" or "Image" or "Gallery" or "Quote" or "AudioEmbed" or "PullQuote" or "RelatedPosts"))
                throw new ValidationException("Unsupported content block.");
            if(b.Text?.Length>10000 || b.Alt?.Length>500 || b.Citation?.Length>1000 || b.Url?.Length>2000)
                throw new ValidationException("Block text is too long.");
            if(b.Type is "Paragraph" or "Heading" or "Quote" or "PullQuote" && string.IsNullOrWhiteSpace(b.Text))throw new ValidationException("Text block is empty.");
            if(b.Type=="Heading" && b.Level is not (2 or 3 or 4))throw new ValidationException("Heading level must be 2, 3 or 4.");
            if(b.Type is "Image" or "AudioEmbed" && !SafeUrl(b.Url))throw new ValidationException("Media requires an HTTPS URL.");
            if(b.Type=="Image" && string.IsNullOrWhiteSpace(b.Alt))throw new ValidationException("Image alt text is required.");
            if(b.Type=="Gallery" && (b.Images is null || b.Images.Length is <1 or >12 || b.Images.Any(i=>i is null||!SafeUrl(i.Url)||i.Url.Length>2000||string.IsNullOrWhiteSpace(i.Alt)||i.Alt.Length>500||i.Caption?.Length>1000)))throw new ValidationException("Gallery images require HTTPS URLs and alt text.");
            if(b.Type=="RelatedPosts" && (b.PostIds is null || b.PostIds.Length is <1 or >8 || b.PostIds.Any(i=>i==Guid.Empty)))throw new ValidationException("Select one to eight related posts.");
        }
    }
    public static BlogStatus Transition(BlogStatus status, string action, string provenance)
    {
        if(action=="review"&&status==BlogStatus.Draft){if(provenance=="ai_assisted_draft")throw new ValidationException("A human must review AI-assisted content first.");return BlogStatus.InReview;}
        if(action=="approve"&&status==BlogStatus.InReview)return BlogStatus.Approved;
        if(action=="publish"&&status==BlogStatus.Approved)return BlogStatus.Published;
        if(action=="return"&&status is BlogStatus.InReview or BlogStatus.Approved)return BlogStatus.Draft;
        if(action=="archive"&&status!=BlogStatus.Archived)return BlogStatus.Archived;
        throw new TheOne.Application.Administration.AdministrationException("This transition is not allowed from the current state.",409);
    }
}
