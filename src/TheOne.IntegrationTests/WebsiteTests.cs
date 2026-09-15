using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Application.Website;
using TheOne.Domain.Entities;
using Xunit;
namespace TheOne.IntegrationTests;
[Collection("PostgreSQL authentication")]
public sealed class WebsiteTests
{
 private static WebsiteDocument Document(string title="Page",object? fields=null)=>new(new(title,"page-"+Guid.NewGuid().ToString("N"),"Summary","SEO","Description",[],[]),JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(JsonSerializer.Serialize(fields??new{}))!);
 [Fact] public void Rejects_unsafe_links_unknown_fields_and_invalid_dates()
 {
  Assert.False(WebsiteRules.Link("//evil.example"));Assert.False(WebsiteRules.Link("javascript:alert(1)"));Assert.True(WebsiteRules.Link("/bn/books"));
  Assert.Throws<ValidationException>(()=>WebsiteRules.Validate("Page",Document(fields:new{rawHtml="<script/>"}),false));
  Assert.Throws<ValidationException>(()=>WebsiteRules.Validate("Event",Document(fields:new{startsAt="2026-10-01T12:00:00",endsAt="2026-10-01T13:00:00+06:00"}),true));
  Assert.Throws<ValidationException>(()=>WebsiteRules.Validate("Place",Document(fields:new{address="Dhaka",latitude="wrong",longitude=90}),true));
  Assert.Throws<ValidationException>(()=>WebsiteRules.Validate("Publication",Document(fields:new{author="Founder",availability="Available"}),true));
 }
 [Fact] public void Rejects_cyclic_public_menus_and_duplicate_series_articles()
 {
  Assert.Throws<ValidationException>(()=>WebsiteRules.Validate("PublicMenu",Document(fields:new{location="main",items=new[]{new{key="a",label="A",href="/a",parentKey="b"},new{key="b",label="B",href="/b",parentKey="a"}}}),true));
  var id=Guid.NewGuid();Assert.Throws<ValidationException>(()=>WebsiteRules.Validate("Series",Document(fields:new{postIds=new[]{id,id}}),true));
 }
 [Fact] public async Task Published_revision_survives_draft_edits_restore_and_version_conflicts()
 {
  using var f=new AuthenticationTests.AuthenticationFactory();using var scope=f.Services.CreateScope();var cms=scope.ServiceProvider.GetRequiredService<IWebsiteService>();var actor=Guid.NewGuid();
  var t=await cms.CreateAsync(actor,new("Page","bn"),default);var first=Document("Original");t=await cms.SaveAsync(t.Id,actor,new(t.Version,first),default);
  await Assert.ThrowsAsync<AdministrationException>(()=>cms.PublicReadAsync("Page","bn",first.Content.Slug,default));
  await Assert.ThrowsAsync<AdministrationException>(()=>cms.ActAsync(t.Id,actor,"publish",new(t.Version),default));
  t=await cms.ActAsync(t.Id,actor,"review",new(t.Version),default);t=await cms.ActAsync(t.Id,actor,"approve",new(t.Version),default);t=await cms.ActAsync(t.Id,actor,"publish",new(t.Version),default);var published=t.PublishedRevisionId;var stale=t.Version;
  t=await cms.SaveAsync(t.Id,actor,new(t.Version,Document("Edited")),default);Assert.Equal("Original",(await cms.PublicReadAsync("Page","bn",first.Content.Slug,default)).Document.Content.Title);
  await Assert.ThrowsAsync<AdministrationException>(()=>cms.SaveAsync(t.Id,actor,new(stale,first),default));
  t=await cms.ActAsync(t.Id,actor,"restore",new(t.Version,published),default);Assert.Equal(BlogStatus.Draft,t.Status);Assert.Null(t.ApprovedBy);
  t=await cms.ActAsync(t.Id,actor,"archive",new(t.Version),default);await Assert.ThrowsAsync<AdministrationException>(()=>cms.PublicReadAsync("Page","bn",first.Content.Slug,default));
 }
 [Fact] public async Task Ai_review_and_preview_are_bound_to_saved_content()
 {
  using var f=new AuthenticationTests.AuthenticationFactory();using var scope=f.Services.CreateScope();var cms=scope.ServiceProvider.GetRequiredService<IWebsiteService>();var actor=Guid.NewGuid();var t=await cms.CreateAsync(actor,new("Page","en"),default);var token=await cms.PreviewAsync(t.Id,default);
  var d=Document();t=await cms.SaveAsync(t.Id,actor,new(t.Version,d with{Content=d.Content with{Provenance="ai_assisted_draft"}}),default);
  await Assert.ThrowsAsync<AdministrationException>(()=>cms.ReadPreviewAsync(token.Token,default));
  t=await cms.SaveAsync(t.Id,actor,new(t.Version,t.Document with{Content=t.Document.Content with{Provenance="human"}}),default);Assert.Equal("ai_assisted_draft",t.Document.Content.Provenance);
  await Assert.ThrowsAsync<ValidationException>(()=>cms.ActAsync(t.Id,actor,"review",new(t.Version),default));
 }
}
