using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Membership;
using TheOne.Persistence.Data;
using TheOne.Persistence.Membership;
using Xunit;
namespace TheOne.IntegrationTests;
[Collection("PostgreSQL authentication")]
public sealed class MembershipFormTests
{
    [Fact]
    public async Task Complete_form_requires_private_photo_and_all_versioned_consents()
    {
        using var f=new AuthenticationTests.AuthenticationFactory();using var scope=f.Services.CreateScope();
        var service=scope.ServiceProvider.GetRequiredService<IMembershipApplicationService>();
        var photos=scope.ServiceProvider.GetRequiredService<IMembershipPhotos>();
        var draft=await service.StartAsync(new(){FullNameBn="পরীক্ষা",FullNameEn="Test Applicant",ContactNumber="+8801700000000"},default);
        var fields=MembershipFormFixture.Fields(draft.ResumeToken);fields.PhotoUrl="uploaded";
        var saved=await service.SaveSectionAsync(draft.ReferenceCode,fields,default);
        Assert.Null(saved.PhotoUrl);
        var submit=new SubmitMembershipApplicationRequest {ContactNumber=fields.ContactNumber,ResumeToken=draft.ResumeToken,
            CodeOfConductAccepted=true,DeclarationAccepted=true,OathAccepted=true,ConsentVersion=MembershipFormDefinition.Version};
        await Assert.ThrowsAsync<ValidationException>(()=>service.SubmitAsync(draft.ReferenceCode,submit,default));
        var bytes=Convert.FromBase64String(MembershipFormFixture.Photo);
        await Assert.ThrowsAsync<MembershipApplicationNotFoundException>(()=>photos.SaveAsync(draft.ReferenceCode,fields.ContactNumber,new string('0',64),bytes,default));
        await photos.SaveAsync(draft.ReferenceCode,fields.ContactNumber,draft.ResumeToken,bytes,default);
        submit.OathAccepted=false;
        await Assert.ThrowsAsync<ValidationException>(()=>service.SubmitAsync(draft.ReferenceCode,submit,default));
        submit.OathAccepted=true;submit.ConsentVersion="old-version";
        await Assert.ThrowsAsync<ValidationException>(()=>service.SubmitAsync(draft.ReferenceCode,submit,default));
        submit.ConsentVersion=MembershipFormDefinition.Version;
        var submitted=await service.SubmitAsync(draft.ReferenceCode,submit,default);
        Assert.True(submitted.DeclarationAccepted);Assert.True(submitted.OathAccepted);Assert.NotNull(submitted.ConsentAcceptedAtUtc);
        Assert.Equal(fields.Email,submitted.Email);Assert.Equal(fields.SignatureName,submitted.SignatureName);
        Assert.Equal(MembershipFormDefinition.Version,submitted.ConsentVersion);
        await Assert.ThrowsAsync<MembershipApplicationNotEditableException>(()=>photos.SaveAsync(draft.ReferenceCode,fields.ContactNumber,draft.ResumeToken,bytes,default));
        var db=scope.ServiceProvider.GetRequiredService<TheOneDbContext>();
        Assert.Equal(1,await db.MembershipPhotos.CountAsync(x=>x.ApplicationId==db.MembershipApplications.Where(a=>a.ReferenceCode==draft.ReferenceCode).Select(a=>a.Id).Single()));
    }
    [Fact]
    public void Non_image_and_oversize_photos_are_rejected()
    {
        Assert.Throws<ValidationException>(()=>MembershipPhotos.ValidateImage("<svg>fake</svg>"u8.ToArray()));
        Assert.Throws<ValidationException>(()=>MembershipPhotos.ValidateImage(new byte[10*1024*1024+1]));
        Assert.Equal("image/png",MembershipPhotos.ValidateImage(Convert.FromBase64String(MembershipFormFixture.Photo)));
    }
}
