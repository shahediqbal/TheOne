namespace TheOne.Application.Blog;
public sealed record EmbeddingRequest(Guid PublishedRevisionId,string Model,float[] Vector);
public sealed record SemanticQuery(string Language,string Model,float[] Vector,int Limit=10);
public sealed record SemanticResult(PublicBlog Article,double Similarity);
public interface IBlogSearch
{
 Task SaveAsync(Guid translationId,EmbeddingRequest request,CancellationToken ct);
 Task<IReadOnlyList<SemanticResult>> SearchAsync(SemanticQuery request,CancellationToken ct);
}
