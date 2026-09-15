using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheOne.Application.Abstractions.Translation;
using TheOne.Application.Common.Models;
using TheOne.Application.Membership;
using TheOne.Domain.Entities;

namespace TheOne.API.Controllers;

public sealed class SuggestTranslationRequest
{
    public string ReferenceCode { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string ResumeToken { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
}

/// <summary>Optional translation of saved, allowlisted draft fields. Disabled by default.</summary>
[ApiController]
[Route("api/v1/membership/translate")]
[AllowAnonymous]
[EnableRateLimiting("membership-public")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TranslationController(IMembershipApplicationService applications, IConfiguration configuration) : ControllerBase
{
    private static readonly string[] StandardFields = ["PurposeOfJoiningBn", "LifeGoalBn", "SpecialSkillsBn", "CurrentChallengeBn"];
    private static readonly string[] AccurateFields = ["PermanentAddressBn", "TemporaryAddressBn", "OccupationBn", "EducationBn"];
    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Suggest(SuggestTranslationRequest request, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Membership:TranslationEnabled"))
            return StatusCode(503, ApiResponse<string>.FailureResponse("Translation suggestions are not enabled. Enter the English text manually."));
        if (!StandardFields.Contains(request.FieldName) && !AccurateFields.Contains(request.FieldName))
            return BadRequest(ApiResponse<string>.FailureResponse("This field is not eligible for translation."));
        var application = await applications.ResumeAsync(new ResumeMembershipApplicationRequest
        {
            ReferenceCode = request.ReferenceCode, ContactNumber = request.ContactNumber, ResumeToken = request.ResumeToken
        }, ct);
        if (application.Status != MembershipApplicationStatus.Draft) throw new MembershipApplicationNotEditableException();
        var source = typeof(MembershipApplicationResponse).GetProperty(request.FieldName)!.GetValue(application) as string;
        if (string.IsNullOrWhiteSpace(source)) return BadRequest(ApiResponse<string>.FailureResponse("Save this field before requesting a translation."));
        try
        {
            var translation = HttpContext.RequestServices.GetRequiredService<ITranslationService>();
            var suggestion = await translation.TranslateAsync(source, "bn", "en",
                AccurateFields.Contains(request.FieldName) ? TranslationTier.HighAccuracy : TranslationTier.Standard, ct);
            return Ok(ApiResponse<string>.SuccessResponse(suggestion));
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            return StatusCode(503, ApiResponse<string>.FailureResponse("Translation is unavailable. Your saved draft is unchanged."));
        }
    }
}
