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
}