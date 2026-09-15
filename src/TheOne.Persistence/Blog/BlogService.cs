using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using TheOne.Application.Website;
using System.Text.Json;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
namespace TheOne.Persistence.Blog;
public sealed class BlogService(TheOneDbContext db, IDataProtectionProvider protection) : IBlogService
{
    private readonly ITimeLimitedDataProtector previews=protection.CreateProtector("TheOne.Blog.Preview.v1").ToTimeLimitedDataProtector();
    private static AdministrationException Missing()=>new("Post not found.",404);
    private static BlogView View(BlogPostTranslation t)=>new(t.Id,t.BlogPostId,t.Language,t.Status,t.Version,BlogRules.Read(t.DraftJson),t.PublishedRevisionId,t.ApprovedBy,t.ApprovedAtUtc,t.PublishedAtUtc);
    private async Task<BlogPostTranslation> Translation(Guid id,CancellationToken ct)=>await db.BlogTranslations.SingleOrDefaultAsync(t=>t.Id==id,ct)??throw Missing();
    private async Task<T> Write<T>(Guid id,Func<Task<T>> action,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        try{await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtextextended('website-cms',0))",ct);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"blog:"+id}, 0))",ct);
            var result=await action();await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return result;
        }catch(DbUpdateConcurrencyException){await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw new AdministrationException("This post changed. Reload before retrying.",409);}
        catch(DbUpdateException ex) when(ex.InnerException is Npgsql.PostgresException {SqlState:"23505"}) {await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw new AdministrationException("That translation or published slug already exists.",409);}
        catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
    }
    private static void Version(BlogPostTranslation t,int expected){if(t.Version!=expected)throw new AdministrationException("This post changed. Reload before editing or retrying.",409);}
    private BlogRevision Snapshot(BlogPostTranslation t,Guid actor,string action)
    {
        t.Version++;t.UpdatedAtUtc=DateTime.UtcNow;
        var r=new BlogRevision{TranslationId=t.Id,Version=t.Version,DocumentJson=t.DraftJson,ActorId=actor,Action=action};db.BlogRevisions.Add(r);
        int position=0;foreach(var id in BlogRules.Read(t.DraftJson).AuthorIds??[])db.BlogRevisionAuthors.Add(new(){RevisionId=r.Id,AuthorId=id,Position=position++});return r;
    }
    private static void ClearApproval(BlogPostTranslation t){t.ApprovedBy=null;t.ApprovedAtUtc=null;}
    public async Task<IReadOnlyList<BlogView>> ListAsync(int page,CancellationToken ct)
    {if(page<1||page>10000)throw new ValidationException("Invalid page.");var rows=await db.BlogTranslations.AsNoTracking().OrderByDescending(t=>t.UpdatedAtUtc).ThenBy(t=>t.Id).Skip((page-1)*20).Take(20).ToListAsync(ct);return rows.Select(View).ToArray();}
    public Task<BlogView> CreateAsync(Guid actor,string language,Guid? postId,CancellationToken ct)
    {
        BlogRules.Language(language);var id=postId??Guid.NewGuid();
        return Write(id,async()=>{
            if(postId is null)db.BlogPosts.Add(new(){Id=id,CreatedBy=actor});
            else if(!await db.BlogPosts.AnyAsync(p=>p.Id==id,ct))throw Missing();
            var t=new BlogPostTranslation{BlogPostId=id,Language=language,DraftJson=BlogRules.Serialize(new("Untitled","untitled-"+Guid.NewGuid().ToString("N"),"","","",[new("Paragraph","Write your article here.")],[]))};
            db.BlogTranslations.Add(t);Snapshot(t,actor,"create");return View(t);
        },ct);
    }
    private async Task Authors(BlogDocument document,bool publishing,CancellationToken ct){foreach(var id in document.AuthorIds??[])if(!await db.WebsiteRecords.AnyAsync(r=>r.Kind=="AuthorProfile"&&r.CanonicalId==id&&(!publishing||r.PublishedRevisionId!=null),ct))throw new ValidationException("An author profile is missing or unpublished.");}
    public async Task<BlogView> GetAsync(Guid id,CancellationToken ct)=>View(await Translation(id,ct));
    public Task<BlogView> SaveAsync(Guid id,Guid actor,SaveBlog request,CancellationToken ct)=>Write(id,async()=>{
        var t=await Translation(id,ct);Version(t,request.ExpectedVersion);BlogRules.Validate(request.Document);await Authors(request.Document,false,ct);
        if(t.Status is BlogStatus.InReview or BlogStatus.Approved)throw new AdministrationException("Return this translation to draft before editing.",409);
        var old=BlogRules.Read(t.DraftJson);
        // Any change to reviewed AI content requires renewed human review, including tags and SEO.
        var doc=old.Provenance.StartsWith("ai_assisted_",StringComparison.Ordinal)?request.Document with{Provenance="ai_assisted_draft"}:request.Document;
        // Editors cannot self-assert the reviewed provenance state through a save.
        if(doc.Provenance=="ai_assisted_reviewed")doc=doc with{Provenance="ai_assisted_draft"};
        t.DraftJson=BlogRules.Serialize(doc);t.Status=BlogStatus.Draft;ClearApproval(t);Snapshot(t,actor,"save");return View(t);
    },ct);
    public Task<BlogView> ActAsync(Guid id,Guid actor,string action,BlogAction request,CancellationToken ct)=>Write(id,async()=>{
        var t=await Translation(id,ct);Version(t,request.ExpectedVersion);
        if(action=="restore"){
            var r=await db.BlogRevisions.SingleOrDefaultAsync(r=>r.Id==request.RevisionId&&r.TranslationId==id,ct)??throw Missing();
            t.DraftJson=r.DocumentJson;t.Status=BlogStatus.Draft;ClearApproval(t);Snapshot(t,actor,"restore");return View(t);
        }
        var document=BlogRules.Read(t.DraftJson);BlogRules.Validate(document);await Authors(document,action=="publish",ct);
        if(action=="review-ai"){
            if(t.Status!=BlogStatus.Draft||document.Provenance!="ai_assisted_draft")throw new AdministrationException("Only AI-assisted drafts can be marked reviewed.",409);
            t.DraftJson=BlogRules.Serialize(document with{Provenance="ai_assisted_reviewed"});Snapshot(t,actor,action);return View(t);
        }
        if(action=="archive"){
            var series=await db.WebsiteRecords.AsNoTracking().Where(r=>r.Kind=="Series"&&r.Language==t.Language&&r.PublishedRevisionId!=null).ToListAsync(ct);
            foreach(var item in series){var json=await db.WebsiteRevisions.Where(r=>r.Id==item.PublishedRevisionId).Select(r=>r.DocumentJson).SingleAsync(ct);var payload=WebsiteSchemas.Read(json);if(payload.Fields.TryGetValue("postIds",out var posts)&&posts.EnumerateArray().Any(p=>p.GetGuid()==t.BlogPostId))throw new ValidationException("Update published series before archiving this article.");}
        }
        t.Status=BlogRules.Transition(t.Status,action,document.Provenance);
        if(action=="approve"){t.ApprovedBy=actor;t.ApprovedAtUtc=DateTime.UtcNow;}
        if(action=="return")ClearApproval(t);
        var revision=Snapshot(t,actor,action);
        if(action=="publish"){
            if(t.ApprovedBy is null)throw new AdministrationException("Administrator approval is required.",409);
            var slug=await db.BlogSlugs.SingleOrDefaultAsync(s=>s.Language==t.Language&&s.Slug==document.Slug,ct);
            if(slug is not null&&slug.TranslationId!=id)throw new AdministrationException("This published slug belongs to another post.",409);
            if(slug is null)db.BlogSlugs.Add(new(){Language=t.Language,Slug=document.Slug,TranslationId=id});
            t.PublishedRevisionId=revision.Id;t.PublishedAtUtc=DateTime.UtcNow;
        }
        if(action=="archive")t.PublishedRevisionId=null;
        if(action is "publish" or "archive"){
            await db.BlogEmbeddings.Where(e=>e.TranslationId==id).ExecuteDeleteAsync(ct);
            var aliases=await db.BlogSlugs.Where(s=>s.TranslationId==id).Select(s=>s.Slug).ToListAsync(ct);
            db.PublishingEvents.Add(new(){PayloadJson=JsonSerializer.Serialize(new{entity="Blog",recordId=id,canonicalId=t.BlogPostId,language=t.Language,action,version=t.Version,slug=document.Slug,aliases=aliases.Append(document.Slug).Distinct()},BlogRules.Json)});
        }
        return View(t);
    },ct);
    public async Task<IReadOnlyList<BlogRevision>> HistoryAsync(Guid id,CancellationToken ct)=>await db.BlogRevisions.AsNoTracking().Where(r=>r.TranslationId==id).OrderByDescending(r=>r.Version).Take(100).ToListAsync(ct);
    public async Task<BlogPreview> PreviewAsync(Guid id,CancellationToken ct)
    {var t=await Translation(id,ct);var expiry=DateTimeOffset.UtcNow.AddMinutes(15);return new(previews.Protect(t.Id+":"+t.Version,expiry),expiry.UtcDateTime);}
    public async Task<BlogDocument> ReadPreviewAsync(string token,CancellationToken ct)
    {
        try{var value=previews.Unprotect(token,out _).Split(':');var id=Guid.Parse(value[0]);var version=int.Parse(value[1]);
            var t=await Translation(id,ct);if(t.Version!=version)throw Missing();return BlogRules.Read(t.DraftJson);
        }catch(Exception ex) when(ex is System.Security.Cryptography.CryptographicException or FormatException or IndexOutOfRangeException){throw Missing();}
    }
    private async Task<PublicBlog> Public(BlogPostTranslation t,CancellationToken ct)
    {
        var r=await db.BlogRevisions.AsNoTracking().SingleOrDefaultAsync(r=>r.Id==t.PublishedRevisionId&&r.TranslationId==t.Id,ct)??throw Missing();
        var other=await db.BlogTranslations.AsNoTracking().SingleOrDefaultAsync(o=>o.BlogPostId==t.BlogPostId&&o.Language!=t.Language&&o.PublishedRevisionId!=null,ct);
        string? otherSlug=null;
        if(other is not null){var otherJson=await db.BlogRevisions.Where(r=>r.Id==other.PublishedRevisionId).Select(r=>r.DocumentJson).SingleAsync(ct);otherSlug=BlogRules.Read(otherJson).Slug;}
        var document=BlogRules.Read(r.DocumentJson);var authors=new List<PublicAuthor>();
        foreach(var id in document.AuthorIds??[]){var choices=await db.WebsiteRecords.AsNoTracking().Where(a=>a.Kind=="AuthorProfile"&&a.CanonicalId==id&&a.PublishedRevisionId!=null).ToListAsync(ct);var profile=choices.FirstOrDefault(a=>a.Language==t.Language)??choices.FirstOrDefault();if(profile is null)continue;var json=await db.WebsiteRevisions.Where(v=>v.Id==profile.PublishedRevisionId).Select(v=>v.DocumentJson).SingleAsync(ct);var p=WebsiteSchemas.Read(json);authors.Add(new(id,p.Content.Title,WebsiteRules.Text(p,"bio")??"",WebsiteRules.Reference(p,"portraitAssetId")));}
        var relatedSeries=new List<PublicSeries>();var series=await db.WebsiteRecords.AsNoTracking().Where(s=>s.Kind=="Series"&&s.Language==t.Language&&s.PublishedRevisionId!=null).ToListAsync(ct);
        foreach(var item in series){var json=await db.WebsiteRevisions.Where(v=>v.Id==item.PublishedRevisionId).Select(v=>v.DocumentJson).SingleAsync(ct);var p=WebsiteSchemas.Read(json);if(p.Fields.TryGetValue("postIds",out var ids)){var postIds=ids.EnumerateArray().Select(v=>v.GetGuid()).ToArray();if(postIds.Contains(t.BlogPostId))relatedSeries.Add(new(item.CanonicalId,p.Content.Title,p.Content.Slug,postIds));}}
        return new(t.BlogPostId,t.Language,document,t.PublishedAtUtc!.Value,otherSlug,authors,relatedSeries);
    }
    public async Task<IReadOnlyList<PublicBlog>> PublicListAsync(string language,int page,CancellationToken ct)
    {
        BlogRules.Language(language);if(page<1||page>10000)throw new ValidationException("Invalid page.");
        var rows=await db.BlogTranslations.AsNoTracking().Where(t=>t.Language==language&&t.PublishedRevisionId!=null).OrderByDescending(t=>t.PublishedAtUtc).ThenBy(t=>t.Id).Skip((page-1)*12).Take(12).ToListAsync(ct);
        var result=new List<PublicBlog>();foreach(var row in rows)result.Add(await Public(row,ct));return result;
    }
    public async Task<PublicBlog> PublicByPostAsync(string language,Guid postId,CancellationToken ct)
    {BlogRules.Language(language);var choices=await db.BlogTranslations.AsNoTracking().Where(t=>t.BlogPostId==postId&&t.PublishedRevisionId!=null).ToListAsync(ct);var t=choices.FirstOrDefault(t=>t.Language==language)??choices.FirstOrDefault()??throw Missing();return await Public(t,ct);}
    public async Task<PublicBlog> PublicReadAsync(string language,string slug,CancellationToken ct)
    {BlogRules.Language(language);var id=await db.BlogSlugs.Where(s=>s.Language==language&&s.Slug==slug).Select(s=>(Guid?)s.TranslationId).SingleOrDefaultAsync(ct)??throw Missing();var t=await Translation(id,ct);if(t.PublishedRevisionId is null)throw Missing();return await Public(t,ct);}
}
