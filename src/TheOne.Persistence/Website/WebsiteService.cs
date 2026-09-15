using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Application.Website;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
namespace TheOne.Persistence.Website;
public sealed class WebsiteService(TheOneDbContext db,IDataProtectionProvider protection,IPermissionReader permissions):IWebsiteService
{
 private readonly ITimeLimitedDataProtector previews=protection.CreateProtector("TheOne.Website.Preview.v1").ToTimeLimitedDataProtector();
 private static AdministrationException Missing()=>new("CMS record not found.",404);
 private static WebsiteView View(WebsiteRecord t)=>new(t.Id,t.CanonicalId,t.Kind,t.Language,t.Status,t.Version,WebsiteSchemas.Read(t.DraftJson),t.LinkedStaffId,t.PublishedRevisionId,t.ApprovedBy,t.ApprovedAtUtc,t.PublishedAtUtc);
 private async Task<WebsiteRecord> Record(Guid id,CancellationToken ct)=>await db.WebsiteRecords.SingleOrDefaultAsync(t=>t.Id==id,ct)??throw Missing();
 private async Task<T> Write<T>(Func<Task<T>> action,CancellationToken ct){await using var tx=await db.Database.BeginTransactionAsync(ct);try{
  // CMS writes serialize as a group so graph-cycle, singleton and reference checks are atomic.
  await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtextextended('website-cms',0))",ct);var result=await action();await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return result;
 }catch(DbUpdateConcurrencyException){await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw new AdministrationException("This content changed. Reload before retrying.",409);}
 catch(DbUpdateException ex)when(ex.InnerException is Npgsql.PostgresException{SqlState:"23505"}){await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw new AdministrationException("That translation, settings record or slug already exists.",409);}
 catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}}
 private static void Version(WebsiteRecord t,int expected){if(t.Version!=expected)throw new AdministrationException("This content changed. Reload before retrying.",409);}
 private static void Clear(WebsiteRecord t){t.ApprovedBy=null;t.ApprovedAtUtc=null;}
 private WebsiteRevision Snapshot(WebsiteRecord t,Guid actor,string action){t.Version++;t.UpdatedAtUtc=DateTime.UtcNow;var r=new WebsiteRevision{RecordId=t.Id,Version=t.Version,DocumentJson=t.DraftJson,ActorId=actor,Action=action};db.WebsiteRevisions.Add(r);return r;}
 public async Task<IReadOnlyList<WebsiteView>> ListAsync(string kind,int page,string? search,CancellationToken ct){WebsiteSchemas.Get(kind);if(page<1||page>10000||search?.Length>100)throw new ValidationException("Invalid page/search.");var q=db.WebsiteRecords.AsNoTracking().Where(t=>t.Kind==kind);if(!string.IsNullOrWhiteSpace(search))q=q.Where(t=>t.Title.Contains(search));return (await q.OrderByDescending(t=>t.UpdatedAtUtc).ThenBy(t=>t.Id).Skip((page-1)*30).Take(30).ToListAsync(ct)).Select(View).ToArray();}
 public Task<WebsiteView> CreateAsync(Guid actor,WebsiteCreate request,CancellationToken ct)=>Write(async()=>{
  WebsiteSchemas.Get(request.Kind);BlogRules.Language(request.Language);var id=Guid.NewGuid();Guid? staff=null;var canonical=id;
  if(request.CanonicalId is{} group){var root=await Record(group,ct);if(root.CanonicalId!=group||root.Kind!=request.Kind)throw new ValidationException("Invalid canonical record.");canonical=group;staff=root.LinkedStaffId;}
  else if(request.Kind=="AuthorProfile"){
   if(request.StaffId is not{} user||!(await permissions.GetAsync(user,ct)).Contains(Permissions.BlogRead))throw new ValidationException("Link an existing staff identity with blog access.");staff=user;
   if(await db.WebsiteRecords.AnyAsync(t=>t.Kind=="AuthorProfile"&&t.LinkedStaffId==user,ct))throw new ValidationException("This staff identity already has an author profile. Add a translation to it instead.");
  }
  if(request.Kind=="SiteSettings"&&await db.WebsiteRecords.AnyAsync(t=>t.Kind==request.Kind&&t.Language==request.Language,ct))throw new ValidationException("Site settings already exist for this language.");
  var t=new WebsiteRecord{Id=id,CanonicalId=canonical,Kind=request.Kind,Language=request.Language,LinkedStaffId=staff,DraftJson=WebsiteSchemas.Serialize(new(new("Untitled","draft-"+id.ToString("N"),"","","",[],[]),new()))};db.WebsiteRecords.Add(t);Snapshot(t,actor,"create");return View(t);
 },ct);
 public async Task<WebsiteView> GetAsync(Guid id,CancellationToken ct)=>View(await Record(id,ct));
 public Task<WebsiteView> SaveAsync(Guid id,Guid actor,WebsiteSave request,CancellationToken ct)=>Write(async()=>{
  var t=await Record(id,ct);Version(t,request.ExpectedVersion);if(t.Status is BlogStatus.InReview or BlogStatus.Approved)throw new AdministrationException("Return content to draft before editing.",409);
  WebsiteRules.Validate(t.Kind,request.Document,false);await Relations(t,request.Document,false,ct);var old=WebsiteSchemas.Read(t.DraftJson);var d=request.Document;
  if(old.Content.Provenance.StartsWith("ai_assisted_",StringComparison.Ordinal)||d.Content.Provenance=="ai_assisted_reviewed")d=d with{Content=d.Content with{Provenance="ai_assisted_draft"}};
  t.Title=d.Content.Title;t.DraftJson=WebsiteSchemas.Serialize(d);t.Status=BlogStatus.Draft;Clear(t);Snapshot(t,actor,"save");return View(t);
 },ct);
 private async Task Relations(WebsiteRecord t,WebsiteDocument d,bool publishing,CancellationToken ct){
  foreach(var field in WebsiteSchemas.Get(t.Kind).Fields){if(field.Type=="reference"&&WebsiteRules.Reference(d,field.Key) is{} id){if(id==t.CanonicalId)throw new ValidationException("A record cannot refer to itself.");if(!await db.WebsiteRecords.AnyAsync(r=>r.CanonicalId==id&&r.Kind==field.TargetKind&&(!publishing||(r.Language==t.Language&&r.PublishedRevisionId!=null)),ct))throw new ValidationException(field.Label+" is missing or not published in this language.");}
   if(field.Type=="asset"&&WebsiteRules.Reference(d,field.Key) is{} asset&&!await db.WebsiteAssets.AnyAsync(a=>a.Id==asset,ct))throw new ValidationException("Uploaded image not found.");
  }
  if(t.Kind=="Series"&&d.Fields.TryGetValue("postIds",out var posts)){foreach(var item in posts.EnumerateArray()){var id=item.GetGuid();if(!await db.BlogPosts.AnyAsync(p=>p.Id==id,ct)||(publishing&&!await db.BlogTranslations.AnyAsync(b=>b.BlogPostId==id&&b.Language==t.Language&&b.PublishedRevisionId!=null,ct)))throw new ValidationException("Series articles must exist and be published in this language before publishing the series.");}}
  if(t.Kind=="LineageNode"){
   var records=await db.WebsiteRecords.AsNoTracking().Where(r=>r.Kind==t.Kind&&r.Language==t.Language).ToListAsync(ct);var parents=new Dictionary<Guid,Guid?>();
   foreach(var r in records){if(r.Id==t.Id){parents[r.CanonicalId]=WebsiteRules.Reference(d,"parentId");continue;}if(publishing&&r.PublishedRevisionId is null)continue;var json=publishing?await db.WebsiteRevisions.Where(v=>v.Id==r.PublishedRevisionId).Select(v=>v.DocumentJson).SingleAsync(ct):r.DraftJson;parents[r.CanonicalId]=WebsiteRules.Reference(WebsiteSchemas.Read(json),"parentId");}
   var seen=new HashSet<Guid>();Guid? current=t.CanonicalId;while(current is{} node){if(!seen.Add(node)||seen.Count>200)throw new ValidationException("Lineage contains a cycle or is too deep.");current=parents.GetValueOrDefault(node);}
  }
 }
 public Task<WebsiteView> ActAsync(Guid id,Guid actor,string action,BlogAction request,CancellationToken ct)=>Write(async()=>{
  var t=await Record(id,ct);Version(t,request.ExpectedVersion);
  if(action=="restore"){var r=await db.WebsiteRevisions.SingleOrDefaultAsync(r=>r.Id==request.RevisionId&&r.RecordId==id,ct)??throw Missing();var doc=WebsiteSchemas.Read(r.DocumentJson);WebsiteRules.Validate(t.Kind,doc,false);await Relations(t,doc,false,ct);t.Title=doc.Content.Title;t.DraftJson=r.DocumentJson;t.Status=BlogStatus.Draft;Clear(t);Snapshot(t,actor,action);return View(t);}
  var d=WebsiteSchemas.Read(t.DraftJson);
  if(action=="review-ai"){if(t.Status!=BlogStatus.Draft||d.Content.Provenance!="ai_assisted_draft")throw new AdministrationException("Only an AI-assisted draft can be marked reviewed.",409);t.DraftJson=WebsiteSchemas.Serialize(d with{Content=d.Content with{Provenance="ai_assisted_reviewed"}});Snapshot(t,actor,action);return View(t);}
  if(action is "review" or "approve" or "publish"){WebsiteRules.Validate(t.Kind,d,true);await Relations(t,d,action=="publish",ct);}
  t.Status=BlogRules.Transition(t.Status,action,d.Content.Provenance);if(action=="approve"){t.ApprovedBy=actor;t.ApprovedAtUtc=DateTime.UtcNow;}if(action=="return")Clear(t);
  if(action=="archive"&&t.Kind is "Place" or "LineageNode" or "AuthorProfile")await CanArchive(t,ct);
  var revision=Snapshot(t,actor,action);
  if(action=="publish"){
   if(t.ApprovedBy is null)throw new AdministrationException("Administrator approval is required.",409);
   if(t.Kind=="PublicMenu"){var menus=await db.WebsiteRecords.AsNoTracking().Where(r=>r.Kind==t.Kind&&r.Language==t.Language&&r.Id!=id&&r.PublishedRevisionId!=null).ToListAsync(ct);foreach(var menu in menus){var json=await db.WebsiteRevisions.Where(r=>r.Id==menu.PublishedRevisionId).Select(r=>r.DocumentJson).SingleAsync(ct);if(WebsiteRules.Text(WebsiteSchemas.Read(json),"location")==WebsiteRules.Text(d,"location"))throw new ValidationException("Another menu is published in this location. Edit it or archive it first.");}}
   var slug=await db.WebsiteSlugs.SingleOrDefaultAsync(s=>s.Kind==t.Kind&&s.Language==t.Language&&s.Slug==d.Content.Slug,ct);if(slug is not null&&slug.RecordId!=id)throw new AdministrationException("This slug is already reserved.",409);if(slug is null)db.WebsiteSlugs.Add(new(){Kind=t.Kind,Language=t.Language,Slug=d.Content.Slug,RecordId=id});t.PublishedRevisionId=revision.Id;t.PublishedAtUtc=DateTime.UtcNow;
  }
  if(action=="archive")t.PublishedRevisionId=null;
  if(action is "publish" or "archive"){var aliases=await db.WebsiteSlugs.Where(s=>s.RecordId==id).Select(s=>s.Slug).ToListAsync(ct);db.PublishingEvents.Add(new(){PayloadJson=JsonSerializer.Serialize(new{entity=t.Kind,recordId=id,canonicalId=t.CanonicalId,language=t.Language,action,version=t.Version,slug=d.Content.Slug,aliases=aliases.Append(d.Content.Slug).Distinct()},BlogRules.Json)});}
  return View(t);
 },ct);
 private async Task CanArchive(WebsiteRecord t,CancellationToken ct){
  if(t.Kind=="AuthorProfile"&&await db.BlogRevisionAuthors.AnyAsync(a=>a.AuthorId==t.CanonicalId&&db.BlogTranslations.Any(b=>b.PublishedRevisionId==a.RevisionId),ct))throw new ValidationException("A published blog credits this author. Update those articles before archiving the profile.");
  var live=await db.WebsiteRecords.AsNoTracking().Where(r=>r.PublishedRevisionId!=null&&r.Id!=t.Id&&r.Language==t.Language).ToListAsync(ct);
  foreach(var record in live){var json=await db.WebsiteRevisions.Where(r=>r.Id==record.PublishedRevisionId).Select(r=>r.DocumentJson).SingleAsync(ct);var d=WebsiteSchemas.Read(json);foreach(var f in WebsiteSchemas.Get(record.Kind).Fields.Where(f=>f.Type=="reference"&&f.TargetKind==t.Kind))if(WebsiteRules.Reference(d,f.Key)==t.CanonicalId)throw new ValidationException("Published content refers to this record. Update it before archiving.");}
 }
 public async Task<IReadOnlyList<WebsiteRevision>> HistoryAsync(Guid id,CancellationToken ct)=>await db.WebsiteRevisions.AsNoTracking().Where(r=>r.RecordId==id).OrderByDescending(r=>r.Version).Take(100).ToListAsync(ct);
 public async Task<BlogPreview> PreviewAsync(Guid id,CancellationToken ct){var t=await Record(id,ct);var expiry=DateTimeOffset.UtcNow.AddMinutes(15);return new(previews.Protect(t.Id+":"+t.Version,expiry),expiry.UtcDateTime);}
 public async Task<WebsiteDocument> ReadPreviewAsync(string token,CancellationToken ct){try{var v=previews.Unprotect(token,out _).Split(':');var t=await Record(Guid.Parse(v[0]),ct);if(t.Version!=int.Parse(v[1]))throw Missing();return WebsiteSchemas.Read(t.DraftJson);}catch(Exception e)when(e is System.Security.Cryptography.CryptographicException or FormatException or IndexOutOfRangeException){throw Missing();}}
 private async Task<WebsitePublic> Public(WebsiteRecord t,CancellationToken ct){if(t.PublishedRevisionId is null)throw Missing();var json=await db.WebsiteRevisions.Where(r=>r.Id==t.PublishedRevisionId&&r.RecordId==t.Id).Select(r=>r.DocumentJson).SingleAsync(ct);var other=await db.WebsiteRecords.AsNoTracking().SingleOrDefaultAsync(r=>r.CanonicalId==t.CanonicalId&&r.Language!=t.Language&&r.PublishedRevisionId!=null,ct);string? slug=null;if(other is not null){var j=await db.WebsiteRevisions.Where(r=>r.Id==other.PublishedRevisionId).Select(r=>r.DocumentJson).SingleAsync(ct);slug=WebsiteSchemas.Read(j).Content.Slug;}return new(t.CanonicalId,t.Kind,t.Language,WebsiteSchemas.Read(json),t.PublishedAtUtc!.Value,slug);}
 public async Task<IReadOnlyList<WebsitePublic>> PublicListAsync(string kind,string language,int page,CancellationToken ct){WebsiteSchemas.Get(kind);BlogRules.Language(language);if(page<1||page>10000)throw new ValidationException("Invalid page.");var records=await db.WebsiteRecords.AsNoTracking().Where(r=>r.Kind==kind&&r.Language==language&&r.PublishedRevisionId!=null).OrderByDescending(r=>r.PublishedAtUtc).ThenBy(r=>r.Id).Skip((page-1)*30).Take(30).ToListAsync(ct);var result=new List<WebsitePublic>();foreach(var t in records)result.Add(await Public(t,ct));return result;}
 public async Task<WebsitePublic> PublicReadAsync(string kind,string language,string slug,CancellationToken ct){WebsiteSchemas.Get(kind);BlogRules.Language(language);var id=await db.WebsiteSlugs.Where(s=>s.Kind==kind&&s.Language==language&&s.Slug==slug).Select(s=>(Guid?)s.RecordId).SingleOrDefaultAsync(ct)??throw Missing();return await Public(await Record(id,ct),ct);}
 public async Task<WebsitePublic> PublicByIdAsync(string kind,string language,Guid id,CancellationToken ct){WebsiteSchemas.Get(kind);BlogRules.Language(language);var choices=await db.WebsiteRecords.AsNoTracking().Where(r=>r.Kind==kind&&r.CanonicalId==id&&r.PublishedRevisionId!=null).ToListAsync(ct);var t=choices.FirstOrDefault(r=>r.Language==language)??choices.FirstOrDefault()??throw Missing();return await Public(t,ct);}
}
