using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TheOne.Application.Administration;
using TheOne.Application.Blog;
using TheOne.Persistence.Data;
namespace TheOne.Persistence.Blog;
/// <summary>Revision-bound pgvector retrieval. Embedding generation is a provider integration, not an implicit external API call.</summary>
public sealed class BlogSearch(TheOneDbContext db,IConfiguration config,IBlogService blogs):IBlogSearch
{
 private string Vector(string model,float[] values){if(!config.GetValue<bool>("BlogSearch:Enabled")||string.IsNullOrWhiteSpace(config["BlogSearch:EmbeddingModel"]))throw new AdministrationException("Semantic search is not configured.",503);if(model!=config["BlogSearch:EmbeddingModel"]||values is null||values.Length!=384||values.Any(v=>!float.IsFinite(v))||values.All(v=>v==0))throw new ValidationException("Use the configured model and a finite, non-zero 384-dimensional vector.");return JsonSerializer.Serialize(values);}
 public async Task SaveAsync(Guid translationId,EmbeddingRequest request,CancellationToken ct){var vector=Vector(request.Model,request.Vector);await using var tx=await db.Database.BeginTransactionAsync(ct);
  await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtextextended('website-cms',0))",ct);
  var t=await db.BlogTranslations.AsNoTracking().SingleOrDefaultAsync(t=>t.Id==translationId,ct)??throw new AdministrationException("Post not found.",404);
  if(t.PublishedRevisionId is null||t.PublishedRevisionId!=request.PublishedRevisionId)throw new AdministrationException("The published revision changed. Re-index the current publication.",409);
  await using var cmd=new NpgsqlCommand("INSERT INTO \"BlogEmbeddings\" (\"TranslationId\",\"RevisionId\",\"Model\",\"EmbeddingData\",\"Dimensions\",\"Embedding\") VALUES (@id,@revision,@model,@vector,384,CAST(@vector AS vector)) ON CONFLICT (\"TranslationId\") DO UPDATE SET \"RevisionId\"=EXCLUDED.\"RevisionId\",\"Model\"=EXCLUDED.\"Model\",\"EmbeddingData\"=EXCLUDED.\"EmbeddingData\",\"Dimensions\"=384,\"Embedding\"=EXCLUDED.\"Embedding\"",(NpgsqlConnection)db.Database.GetDbConnection(),(NpgsqlTransaction)tx.GetDbTransaction());cmd.Parameters.AddWithValue("id",translationId);cmd.Parameters.AddWithValue("revision",request.PublishedRevisionId);cmd.Parameters.AddWithValue("model",request.Model);cmd.Parameters.AddWithValue("vector",vector);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
 }
 public async Task<IReadOnlyList<SemanticResult>> SearchAsync(SemanticQuery request,CancellationToken ct){var vector=Vector(request.Model,request.Vector);BlogRules.Language(request.Language);if(request.Limit<1||request.Limit>20)throw new ValidationException("Limit must be 1 to 20.");await db.Database.OpenConnectionAsync(ct);
  await using var cmd=new NpgsqlCommand("SELECT t.\"BlogPostId\", 1-(e.\"Embedding\" <=> CAST(@vector AS vector)) FROM \"BlogEmbeddings\" e JOIN \"BlogTranslations\" t ON t.\"Id\"=e.\"TranslationId\" AND t.\"PublishedRevisionId\"=e.\"RevisionId\" WHERE t.\"Language\"=@language AND e.\"Model\"=@model AND e.\"Embedding\" IS NOT NULL ORDER BY e.\"Embedding\" <=> CAST(@vector AS vector) LIMIT @limit",(NpgsqlConnection)db.Database.GetDbConnection());cmd.Parameters.AddWithValue("vector",vector);cmd.Parameters.AddWithValue("language",request.Language);cmd.Parameters.AddWithValue("model",request.Model);cmd.Parameters.AddWithValue("limit",request.Limit);var hits=new List<(Guid Id,double Score)>();await using(var reader=await cmd.ExecuteReaderAsync(ct)){while(await reader.ReadAsync(ct))hits.Add((reader.GetGuid(0),reader.GetDouble(1)));}
  var result=new List<SemanticResult>();foreach(var hit in hits){try{var article=await blogs.PublicByPostAsync(request.Language,hit.Id,ct);if(article.Language==request.Language)result.Add(new(article,hit.Score));}catch(AdministrationException ex)when(ex.StatusCode==404){}}
  return result;
 }
}
