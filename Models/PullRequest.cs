namespace FGP.Server.Models;

public class PullRequest
{
    public int Id { get; set; } // Unique ID (e.g., PR #1)
    
    public required string RepoName { get; set; } // Which project is this for?
    
    public required string Title { get; set; }
    public required string Description { get; set; }
    
    // The "Git" parts
    public required string SourceBranch { get; set; } // Where the code comes from
    public required string TargetBranch { get; set; } // Where the code is going
    
    public required bool IsOpen { get; set; } = true; // Is it merged yet?
}

// The form users fill out to create a PR
public record CreatePrRequest(
    string RepoName,
    string Title,
    string Description,
    string SourceBranch,
    string TargetBranch
);