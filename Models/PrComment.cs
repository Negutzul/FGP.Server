namespace FGP.Server.Models;

public class PrComment
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public required string Author { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record CreateCommentRequest(string Author, string Body);
