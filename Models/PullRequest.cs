namespace FGP.Server.Models;

public class PullRequest
{
    public int Id { get; set; } // Unique ID (e.g., PR #1)
    
    public string RepoName { get; set; } // Which project is this for?
    
    public string Title { get; set; }
    public string Description { get; set; }
    
    // The "Git" parts
    public string SourceBranch { get; set; } // Where the code comes from
    public string TargetBranch { get; set; } // Where the code is going
    
    public bool IsOpen { get; set; } = true; // Is it merged yet?
}

// The form users fill out to create a PR
public record CreatePrRequest(
    string RepoName,
    string Title,
    string Description,
    string SourceBranch,
    string TargetBranch
);