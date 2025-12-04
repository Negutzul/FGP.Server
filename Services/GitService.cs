using LibGit2Sharp; 

namespace FGP.Server; 

public class GitService
{
    // !!! UPDATE THIS PATH !!!
    // It must point to the folder containing 'my-test-project'
    // Example: @"C:\Users\John\Desktop\Projects\my-git-server\";
    private readonly string _repoBasePath = @"C:\Users\klu\Desktop\Projects"; 

    public List<string> GetBranches(string repoName)
    {
        // Combine the library path with the book name
        string repoPath = Path.Combine(_repoBasePath, repoName);
        
        // Safety check: Does the book exist?
        if (!Directory.Exists(repoPath)) 
        {
            throw new Exception($"Repository not found at: {repoPath}");
        }

        // Open the book and read the Table of Contents (Branches)
        using (var repo = new Repository(repoPath))
        {
            return repo.Branches
                .Where(b => !b.IsRemote)      // Only look at local chapters
                .Select(b => b.FriendlyName)  // Write down their names
                .ToList();
        }
    }

    // Add this inside the GitService class
    public List<SimpleCommit> GetCommitsForBranch(string repoName, string branchName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        using (var repo = new Repository(repoPath))
        {
            // 1. Find the specific branch (e.g., "main")
            var branch = repo.Branches[branchName];
            
            if (branch == null)
            {
                throw new Exception($"Branch '{branchName}' not found.");
            }

            // 2. Read the commits from that branch
            // We convert the complex Git commit into our "SimpleCommit"
            return branch.Commits
                .Select(c => new SimpleCommit(
                    c.Sha,
                    c.MessageShort,
                    c.Author.Name,
                    c.Author.When
                ))
                .ToList();
        }
    }
}

public record SimpleCommit(
    string Sha, 
    string Message, 
    string Author, 
    DateTimeOffset Date
);