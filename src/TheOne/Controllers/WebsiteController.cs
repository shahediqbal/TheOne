using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Application.Common.Models;
using TheOne.Application.Website;
using TheOne.Domain.Entities;
using TheOne.Persistence.Data;
using TheOne.Persistence.Identity;
using TheOne.Persistence.Membership;
namespace TheOne.API.Controllers;
[ApiController,Route("api/v1/admin/website"),Authorize(Policy=Permissions.WebsiteRead)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None),RequestSizeLimit(1024*1024)]
public sealed class WebsiteController(IWebsiteService cms,IPermissionReader permissions,UserManager<ApplicationUser> users,TheOneDbContext db):ControllerBase
{
 private Guid Actor=>Guid.Parse(User.FindFirstValue("sub")!);
 private async Task Require(bool admin,CancellationToken ct){var grants=await permissions.GetAsync(Actor,ct);if(!grants.Contains(admin?Permissions.WebsiteApprove:Permissions.WebsiteEdit))throw new AdministrationException("Permission denied.",403);if(admin){var user=await users.FindByIdAsync(Actor.ToString());if(user is null||!(await users.GetRolesAsync(user)).Any(r=>r is "Admin" or "SuperAdmin"))throw new AdministrationException("Administrator approval is required.",403);}}
 [HttpGet("schema")] public IActionResult Schema()=>Ok(ApiResponse<object>.SuccessResponse(WebsiteSchemas.All));
 [HttpGet("records")] public async Task<IActionResult> List(string kind="Page",int page=1,string? search=null,CancellationToken ct=default)=>Ok(ApiResponse<object>.SuccessResponse(await cms.ListAsync(kind,page,search,ct)));
 [HttpGet("records/{id:guid}")] public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await cms.GetAsync(id,ct)));
 [HttpPost("records")] public async Task<IActionResult> Create(WebsiteCreate request,CancellationToken ct){await Require(false,ct);return Ok(ApiResponse<object>.SuccessResponse(await cms.CreateAsync(Actor,request,ct)));}
 [HttpPut("records/{id:guid}")] public async Task<IActionResult> Save(Guid id,WebsiteSave request,CancellationToken ct){await Require(false,ct);return Ok(ApiResponse<object>.SuccessResponse(await cms.SaveAsync(id,Actor,request,ct)));}
 [HttpPost("records/{id:guid}/actions/{action}")] public async Task<IActionResult> Act(Guid id,string action,BlogAction request,CancellationToken ct){if(action is not("review" or "return" or "restore" or "review-ai" or "approve" or "publish" or "archive"))return NotFound();await Require(action is "review-ai" or "approve" or "publish" or "archive",ct);return Ok(ApiResponse<object>.SuccessResponse(await cms.ActAsync(id,Actor,action,request,ct)));}
 [HttpGet("records/{id:guid}/revisions")] public async Task<IActionResult> History(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await cms.HistoryAsync(id,ct)));
 [HttpPost("records/{id:guid}/preview")] public async Task<IActionResult> Preview(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await cms.PreviewAsync(id,ct)));
 [HttpGet("lookup")] public async Task<IActionResult> Lookup(string kind,int page=1,CancellationToken ct=default){if(page<1||page>10000)throw new ValidationException("Invalid page.");
  if(kind=="BlogPost"){var blogs=await db.BlogTranslations.AsNoTracking().OrderBy(t=>t.BlogPostId).ThenBy(t=>t.Language).Skip((page-1)*50).Take(50).ToListAsync(ct);return Ok(ApiResponse<object>.SuccessResponse(blogs.Select(b=>new{id=b.BlogPostId,title=BlogRules.Read(b.DraftJson).Title,language=b.Language,published=b.PublishedRevisionId!=null})));}
  WebsiteSchemas.Get(kind);return Ok(ApiResponse<object>.SuccessResponse(await db.WebsiteRecords.AsNoTracking().Where(r=>r.Kind==kind).OrderBy(r=>r.Title).ThenBy(r=>r.Id).Skip((page-1)*50).Take(50).Select(r=>new{id=r.CanonicalId,title=r.Title,language=r.Language,published=r.PublishedRevisionId!=null}).ToListAsync(ct)));
 }
 [HttpGet("staff")] public async Task<IActionResult> Staff(int page=1,CancellationToken ct=default){if(page<1||page>10000)throw new ValidationException("Invalid page.");var candidates=await db.Users.AsNoTracking().Where(u=>u.IsActive).OrderBy(u=>u.Id).Skip((page-1)*50).Take(50).Select(u=>new{u.Id,u.FullName}).ToListAsync(ct);var result=new List<object>();foreach(var user in candidates)if((await permissions.GetAsync(user.Id,ct)).Contains(Permissions.BlogRead))result.Add(new{id=user.Id,title=user.FullName});return Ok(ApiResponse<object>.SuccessResponse(result));}
 [HttpGet("assets")] public async Task<IActionResult> Assets(int page=1,CancellationToken ct=default){if(page<1||page>10000)throw new ValidationException("Invalid page.");return Ok(ApiResponse<object>.SuccessResponse(await db.WebsiteAssets.AsNoTracking().OrderByDescending(a=>a.CreatedAtUtc).ThenBy(a=>a.Id).Skip((page-1)*30).Take(30).Select(a=>new AssetView(a.Id,a.OriginalName,a.ContentType,a.Content.Length,a.CreatedAtUtc)).ToListAsync(ct)));}
 [HttpPost("assets"),RequestSizeLimit(11*1024*1024),RequestFormLimits(MultipartBodyLengthLimit=11*1024*1024)] public async Task<IActionResult> Upload([FromForm]IFormFile image,CancellationToken ct){await Require(false,ct);if(image.Length==0||image.Length>10*1024*1024)throw new ValidationException("Image must be at most 10 MB.");using var stream=new MemoryStream();await image.CopyToAsync(stream,ct);var bytes=stream.ToArray();var type=MembershipPhotos.ValidateImage(bytes);var name=Path.GetFileName(image.FileName);if(name.Length>200)name=name[..200];var asset=new WebsiteAsset{Content=bytes,ContentType=type,OriginalName=name,UploadedBy=Actor};db.WebsiteAssets.Add(asset);await db.SaveChangesAsync(ct);return Ok(ApiResponse<object>.SuccessResponse(new AssetView(asset.Id,name,type,bytes.Length,asset.CreatedAtUtc)));}
 [HttpGet("assets/{id:guid}")] public async Task<IActionResult> Asset(Guid id,CancellationToken ct){var asset=await db.WebsiteAssets.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id,ct)??throw new AdministrationException("Image not found.",404);Response.Headers["X-Content-Type-Options"]="nosniff";Response.Headers.ContentSecurityPolicy="default-src 'none'; sandbox";return File(asset.Content,asset.ContentType);}
 [HttpGet("publish-events")] public async Task<IActionResult> Events(CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await db.PublishingEvents.AsNoTracking().Where(e=>e.AcknowledgedAtUtc==null).OrderBy(e=>e.CreatedAtUtc).ThenBy(e=>e.Id).Take(100).ToListAsync(ct)));
 [HttpPost("publish-events/{id:guid}/acknowledge")] public async Task<IActionResult> Acknowledge(Guid id,CancellationToken ct){await Require(true,ct);var item=await db.PublishingEvents.SingleOrDefaultAsync(e=>e.Id==id,ct)??throw new AdministrationException("Event not found.",404);item.AcknowledgedAtUtc??=DateTime.UtcNow;await db.SaveChangesAsync(ct);return Ok(ApiResponse<object>.SuccessResponse(true));}
}
[ApiController,Route("api/v1/website"),AllowAnonymous,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PublicWebsiteController(IWebsiteService cms,TheOneDbContext db):ControllerBase
{
 [HttpGet("{kind}/{language}")]public async Task<IActionResult> List(string kind,string language,int page=1,CancellationToken ct=default)=>Ok(ApiResponse<object>.SuccessResponse(await cms.PublicListAsync(kind,language,page,ct)));
 [HttpGet("{kind}/{language}/by-id/{id:guid}")]public async Task<IActionResult> ById(string kind,string language,Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await cms.PublicByIdAsync(kind,language,id,ct)));
 [HttpGet("{kind}/{language}/{slug}")]public async Task<IActionResult> Read(string kind,string language,string slug,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await cms.PublicReadAsync(kind,language,slug,ct)));
 [HttpPost("preview"),RequestSizeLimit(8192)]public async Task<IActionResult> Preview(PreviewToken request,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await cms.ReadPreviewAsync(request.Token,ct)));
 [HttpGet("assets/{id:guid}")]public async Task<IActionResult> Asset(Guid id,CancellationToken ct){
  // An upload is public only while an approved live record explicitly references it.
  var records=await db.WebsiteRecords.AsNoTracking().Where(r=>r.PublishedRevisionId!=null).Select(r=>new{r.Kind,r.PublishedRevisionId}).ToListAsync(ct);bool visible=false;
  foreach(var record in records){var json=await db.WebsiteRevisions.Where(r=>r.Id==record.PublishedRevisionId).Select(r=>r.DocumentJson).SingleAsync(ct);var d=WebsiteSchemas.Read(json);if(WebsiteSchemas.Get(record.Kind).Fields.Where(f=>f.Type=="asset").Any(f=>WebsiteRules.Reference(d,f.Key)==id)){visible=true;break;}}
  if(!visible)return NotFound();var asset=await db.WebsiteAssets.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id,ct);if(asset is null)return NotFound();Response.Headers["X-Content-Type-Options"]="nosniff";Response.Headers.ContentSecurityPolicy="default-src 'none'; sandbox";return File(asset.Content,asset.ContentType);
 }
}
