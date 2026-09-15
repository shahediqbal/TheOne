using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using TheOne.Application.Administration;
using TheOne.Application.Common.Models;
using TheOne.Application.Membership;
namespace TheOne.API.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MembershipPhotoController(IMembershipPhotos photos, IMembershipApplicationService applications) : ControllerBase
{
    [AllowAnonymous, EnableRateLimiting("membership-public")]
    [HttpPost("api/v1/membership/applications/{referenceCode}/photo")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(string referenceCode, [FromForm] string contactNumber, [FromForm] string resumeToken, [FromForm] IFormFile photo, CancellationToken ct)
    {
        if (photo.Length == 0 || photo.Length > 10 * 1024 * 1024) throw new ValidationException("Photo must be 10 MB or smaller.");
        using var memory = new MemoryStream(); await photo.CopyToAsync(memory, ct);
        await photos.SaveAsync(referenceCode, contactNumber, resumeToken, memory.ToArray(), ct);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }
    [AllowAnonymous, EnableRateLimiting("membership-public")]
    [HttpPost("api/v1/membership/applications/{referenceCode}/photo/read")]
    public async Task<IActionResult> ApplicantRead(string referenceCode, ResumeMembershipApplicationRequest request, CancellationToken ct)
    {
        request.ReferenceCode = referenceCode; await applications.ResumeAsync(request, ct);
        return await Read(referenceCode, ct);
    }
    [Authorize(Policy = Permissions.MembershipRead)]
    [HttpGet("api/v1/admin/membership/applications/{referenceCode}/photo")]
    public Task<IActionResult> StaffRead(string referenceCode, CancellationToken ct) => Read(referenceCode, ct);
    private async Task<IActionResult> Read(string referenceCode, CancellationToken ct)
    {
        var photo = await photos.ReadAsync(referenceCode, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        return File(photo.Content, photo.ContentType);
    }
}
