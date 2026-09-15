using System.Text.Json;
using TheOne.Application.Blog;
using TheOne.Domain.Entities;
namespace TheOne.Application.Website;
public sealed record WebsiteField(string Key,string Label,string Type,bool Required=false,string? TargetKind=null,string[]? Choices=null);
public sealed record WebsiteSchema(string Kind,string Label,WebsiteField[] Fields);
public sealed record WebsiteDocument(BlogDocument Content,Dictionary<string,JsonElement> Fields);
public sealed record WebsiteCreate(string Kind,string Language,Guid? CanonicalId=null,Guid? StaffId=null);
public sealed record WebsiteSave(int ExpectedVersion,WebsiteDocument Document);
public sealed record WebsiteView(Guid Id,Guid CanonicalId,string Kind,string Language,BlogStatus Status,int Version,WebsiteDocument Document,Guid? LinkedStaffId,Guid? PublishedRevisionId,Guid? ApprovedBy,DateTime? ApprovedAtUtc,DateTime? PublishedAtUtc);
public sealed record WebsitePublic(Guid CanonicalId,string Kind,string Language,WebsiteDocument Document,DateTime PublishedAtUtc,string? OtherLanguageSlug);
public sealed record AssetView(Guid Id,string OriginalName,string ContentType,long Bytes,DateTime CreatedAtUtc);
public interface IWebsiteService
{
 Task<IReadOnlyList<WebsiteView>> ListAsync(string kind,int page,string? search,CancellationToken ct);
 Task<WebsiteView> CreateAsync(Guid actor,WebsiteCreate request,CancellationToken ct);
 Task<WebsiteView> GetAsync(Guid id,CancellationToken ct);
 Task<WebsiteView> SaveAsync(Guid id,Guid actor,WebsiteSave request,CancellationToken ct);
 Task<WebsiteView> ActAsync(Guid id,Guid actor,string action,BlogAction request,CancellationToken ct);
 Task<IReadOnlyList<WebsiteRevision>> HistoryAsync(Guid id,CancellationToken ct);
 Task<BlogPreview> PreviewAsync(Guid id,CancellationToken ct);
 Task<WebsiteDocument> ReadPreviewAsync(string token,CancellationToken ct);
 Task<IReadOnlyList<WebsitePublic>> PublicListAsync(string kind,string language,int page,CancellationToken ct);
 Task<WebsitePublic> PublicReadAsync(string kind,string language,string slug,CancellationToken ct);
 Task<WebsitePublic> PublicByIdAsync(string kind,string language,Guid id,CancellationToken ct);
}
public static class WebsiteSchemas
{
 public static readonly WebsiteSchema[] All=[
  new("Page","Pages",[]),
  new("SiteSettings","Site settings",[new("organizationName","Organization name","text",true),new("tagline","Tagline","text"),new("contactEmail","Contact email","email",true),new("phone","Phone / hotline","text",true),new("address","Postal address","textarea",true),new("footerText","Footer text","textarea"),new("socialLinks","Social links","links"),new("heroAssetId","Homepage image","asset")]),
  new("PublicMenu","Public menus",[new("location","Menu location","select",true,null,["main","footer"]),new("items","Menu entries","menu",true)]),
  new("Place","Places",[new("address","Address","textarea",true),new("latitude","Latitude","latitude",true),new("longitude","Longitude","longitude",true),new("visitingHours","Visiting hours","text"),new("contact","Contact","text"),new("imageAssetId","Photograph","asset")]),
  new("LineageNode","Lineage",[new("parentId","Parent in lineage","reference",false,"LineageNode"),new("order","Display order","integer",true),new("lifeDates","Life dates (as documented)","text"),new("portraitAssetId","Portrait","asset")]),
  new("Event","Programmes and events",[new("startsAt","Starts (with timezone)","datetime",true),new("endsAt","Ends (with timezone)","datetime",true),new("placeId","Place","reference",false,"Place"),new("venue","Venue details","textarea"),new("contact","Contact","text"),new("cancelled","Cancelled","boolean"),new("hijriLabel","Hijri date (reviewed display text)","text")]),
  new("MediaItem","Media library entries",[new("mediaType","Media type","select",true,null,["Photo","Video","Audio"]),new("assetId","Uploaded image","asset"),new("url","Hosted media URL","url"),new("alt","Description / alt text","text",true),new("category","Category","text"),new("credit","Credit / source","text")]),
  new("Publication","Books and publications",[new("author","Author / founder","text",true),new("publicationYear","Publication year","integer"),new("coverAssetId","Book cover","asset"),new("readingUrl","Reading archive link","url"),new("availability","Reading availability","select",true,null,["Preparing","Available"]),new("publisher","Publisher","text")]),
  new("AuthorProfile","Author profiles",[new("portraitAssetId","Portrait","asset"),new("bio","Biography","textarea",true)]),
  new("Series","Blog series",[new("postIds","Ordered articles","posts",true)])
 ];
 public static WebsiteSchema Get(string kind)=>All.SingleOrDefault(x=>x.Kind==kind)??throw new FluentValidation.ValidationException("Unknown CMS content type.");
 public static WebsiteDocument Read(string json)=>JsonSerializer.Deserialize<WebsiteDocument>(json,BlogRules.Json)!;
 public static string Serialize(WebsiteDocument document)=>JsonSerializer.Serialize(document,BlogRules.Json);
}
