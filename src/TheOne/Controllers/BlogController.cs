using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Application.Common.Models;
using TheOne.Persistence.Identity;
namespace TheOne.API.Controllers;
[ApiController,Route("api/v1/admin/blog"),Authorize(Policy=Permissions.BlogRead)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[RequestSizeLimit(1024*1024)]
public sealed class BlogController(IBlogService blog,IBlogSearch search,IPermissionReader permissions,UserManager<ApplicationUser> users):ControllerBase
{
 private Guid Actor=>Guid.Parse(User.FindFirstValue("sub")!);
 private async Task Require(string permission,bool administrator,CancellationToken ct){var grants=await permissions.GetAsync(Actor,ct);if(!grants.Contains(permission))throw new AdministrationException("Permission denied.",403);if(administrator){var user=await users.FindByIdAsync(Actor.ToString());if(user is null)throw new AdministrationException("Permission denied.",403);var roles=await users.GetRolesAsync(user);if(!roles.Any(r=>r is "Admin" or "SuperAdmin"))throw new AdministrationException("Administrator approval is required.",403);}}
 [HttpGet] public async Task<IActionResult> List(int page=1,CancellationToken ct=default)=>Ok(ApiResponse<object>.SuccessResponse(await blog.ListAsync(page,ct)));
 [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await blog.GetAsync(id,ct)));
 [HttpPost] public async Task<IActionResult> Create(CreateBlog request,CancellationToken ct){await Require(Permissions.BlogEdit,false,ct);return Ok(ApiResponse<object>.SuccessResponse(await blog.CreateAsync(Actor,request.Language,request.PostId,ct)));}
 [HttpPut("{id:guid}")] public async Task<IActionResult> Save(Guid id,SaveBlog request,CancellationToken ct){await Require(Permissions.BlogEdit,false,ct);return Ok(ApiResponse<object>.SuccessResponse(await blog.SaveAsync(id,Actor,request,ct)));}
 [HttpPost("{id:guid}/actions/{action}")] public async Task<IActionResult> Act(Guid id,string action,BlogAction request,CancellationToken ct){bool admin=action is "approve" or "publish" or "archive" or "review-ai";if(action is not("approve" or "publish" or "archive" or "review-ai" or "return" or "review" or "restore"))return NotFound();await Require(admin?Permissions.BlogApprove:Permissions.BlogEdit,admin,ct);return Ok(ApiResponse<object>.SuccessResponse(await blog.ActAsync(id,Actor,action,request,ct)));}
 [HttpPut("{id:guid}/embedding"),RequestSizeLimit(32768)] public async Task<IActionResult> Embedding(Guid id,EmbeddingRequest request,CancellationToken ct){await Require(Permissions.BlogApprove,true,ct);await search.SaveAsync(id,request,ct);return Ok(ApiResponse<object>.SuccessResponse(true));}
 [HttpGet("{id:guid}/revisions")] public async Task<IActionResult> History(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await blog.HistoryAsync(id,ct)));
 [HttpPost("{id:guid}/preview")] public async Task<IActionResult> Preview(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await blog.PreviewAsync(id,ct)));
}
public sealed record CreateBlog(string Language,Guid? PostId=null);
[ApiController,Route("api/v1/blog"),AllowAnonymous]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PublicBlogController(IBlogService blog,IBlogSearch search):ControllerBase
{
 [HttpPost("search/semantic"),RequestSizeLimit(32768)] public async Task<IActionResult> Search(SemanticQuery request,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await search.SearchAsync(request,ct)));
 [HttpGet("{language}")] public async Task<IActionResult> List(string language,int page=1,CancellationToken ct=default)=>Ok(ApiResponse<object>.SuccessResponse(await blog.PublicListAsync(language,page,ct)));
 [HttpGet("{language}/post/{id:guid}")] public async Task<IActionResult> ByPost(string language,Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await blog.PublicByPostAsync(language,id,ct)));
 [HttpGet("{language}/{slug}")] public async Task<IActionResult> Read(string language,string slug,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await blog.PublicReadAsync(language,slug,ct)));
 [HttpPost("preview"),RequestSizeLimit(8192)] public async Task<IActionResult> Preview(PreviewToken request,CancellationToken ct)=>Ok(ApiResponse<object>.SuccessResponse(await blog.ReadPreviewAsync(request.Token,ct)));
}
public sealed record PreviewToken(string Token);
