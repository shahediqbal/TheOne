using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Domain.Entities;
using Xunit;
namespace TheOne.IntegrationTests;
[Collection("PostgreSQL authentication")]
public sealed class BlogTests
{
 private static BlogDocument Document(string title="Test")=>new(title,"article-"+Guid.NewGuid().ToString("N"),"Summary","SEO title","Description",[new("Paragraph","Body")],[]);
 [Fact] public void Rules_reject_unapproved_publication_and_unsafe_blocks()
 {
  Assert.Throws<AdministrationException>(()=>BlogRules.Transition(BlogStatus.Draft,"publish","human"));
  Assert.Throws<ValidationException>(()=>BlogRules.Transition(BlogStatus.Draft,"review","ai_assisted_draft"));
  Assert.Throws<ValidationException>(()=>BlogRules.Validate(Document() with{Blocks=[new("HTML","<script>bad</script>")]}));
  Assert.Throws<ValidationException>(()=>BlogRules.Validate(Document() with{Blocks=[new("Image",Url:"javascript:alert(1)",Alt:"Example")]}));
  Assert.Throws<ValidationException>(()=>BlogRules.Validate(Document() with{Slug="preview"}));
 }
 [Fact] public async Task Edits_and_restores_do_not_change_live_content_until_approved_and_published()
 {
  using var f=new AuthenticationTests.AuthenticationFactory();using var scope=f.Services.CreateScope();var blog=scope.ServiceProvider.GetRequiredService<IBlogService>();var actor=Guid.NewGuid();
  var t=await blog.CreateAsync(actor,"bn",null,default);var first=Document("Original");t=await blog.SaveAsync(t.Id,actor,new(t.Version,first),default);
  await Assert.ThrowsAsync<AdministrationException>(()=>blog.PublicReadAsync("bn",first.Slug,default));
  t=await blog.ActAsync(t.Id,actor,"review",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"approve",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"publish",new(t.Version),default);
  var published=t.PublishedRevisionId;Assert.Equal("Original",(await blog.PublicReadAsync("bn",first.Slug,default)).Document.Title);
  var second=Document("Edited");t=await blog.SaveAsync(t.Id,actor,new(t.Version,second),default);
  Assert.Equal("Original",(await blog.PublicReadAsync("bn",first.Slug,default)).Document.Title);
  await Assert.ThrowsAsync<AdministrationException>(()=>blog.ActAsync(t.Id,actor,"publish",new(t.Version),default));
  t=await blog.ActAsync(t.Id,actor,"review",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"approve",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"publish",new(t.Version),default);
  Assert.Equal(second.Slug,(await blog.PublicReadAsync("bn",first.Slug,default)).Document.Slug);
  t=await blog.ActAsync(t.Id,actor,"restore",new(t.Version,published),default);
  Assert.Equal(BlogStatus.Draft,t.Status);Assert.Null(t.ApprovedBy);Assert.Equal("Edited",(await blog.PublicReadAsync("bn",first.Slug,default)).Document.Title);
  t=await blog.ActAsync(t.Id,actor,"archive",new(t.Version),default);
  await Assert.ThrowsAsync<AdministrationException>(()=>blog.PublicReadAsync("bn",first.Slug,default));
 }
 [Fact] public async Task Stale_versions_and_changed_previews_are_rejected()
 {
  using var f=new AuthenticationTests.AuthenticationFactory();using var scope=f.Services.CreateScope();var blog=scope.ServiceProvider.GetRequiredService<IBlogService>();var actor=Guid.NewGuid();
  var t=await blog.CreateAsync(actor,"en",null,default);var token=await blog.PreviewAsync(t.Id,default);Assert.NotNull(await blog.ReadPreviewAsync(token.Token,default));
  var version=t.Version;t=await blog.SaveAsync(t.Id,actor,new(version,Document()),default);
  await Assert.ThrowsAsync<AdministrationException>(()=>blog.SaveAsync(t.Id,actor,new(version,Document()),default));
  await Assert.ThrowsAsync<AdministrationException>(()=>blog.ReadPreviewAsync(token.Token,default));
 }
 [Fact] public async Task Ai_provenance_cannot_be_erased_by_saving_and_languages_review_independently()
 {
  using var f=new AuthenticationTests.AuthenticationFactory();using var scope=f.Services.CreateScope();var blog=scope.ServiceProvider.GetRequiredService<IBlogService>();var actor=Guid.NewGuid();
  var t=await blog.CreateAsync(actor,"bn",null,default);t=await blog.SaveAsync(t.Id,actor,new(t.Version,Document() with{Provenance="ai_assisted_draft"}),default);
  t=await blog.SaveAsync(t.Id,actor,new(t.Version,t.Document with{Provenance="human"}),default);Assert.Equal("ai_assisted_draft",t.Document.Provenance);
  await Assert.ThrowsAsync<ValidationException>(()=>blog.ActAsync(t.Id,actor,"review",new(t.Version),default));
  t=await blog.ActAsync(t.Id,actor,"review-ai",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"review",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"approve",new(t.Version),default);t=await blog.ActAsync(t.Id,actor,"publish",new(t.Version),default);
  var english=await blog.CreateAsync(actor,"en",t.PostId,default);Assert.Equal(BlogStatus.Draft,english.Status);var fallback=await blog.PublicByPostAsync("en",t.PostId,default);Assert.Equal("bn",fallback.Language);Assert.Null(fallback.OtherLanguageSlug);
 }
}
