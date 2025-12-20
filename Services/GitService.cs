using LibGit2Sharp; 

namespace FGP.Server; 

public class GitService
{

    private readonly string _repoBasePath = @"C:\Users\klu\Desktop\Projects\FGP"; 

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

    public List<FileDiff> GetDiffForCommit(string repoName, string commitSha)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        using (var repo = new Repository(repoPath))
        {
            // 1. Find the commit
            var commit = repo.Lookup<Commit>(commitSha);
            if (commit == null) throw new Exception("Commit not found");

            // 2. Find the parent (The "Old" version)
            // If there is no parent (first commit), we compare against "Nothing" (null)
            var parentCommit = commit.Parents.FirstOrDefault();
            var parentTree = parentCommit?.Tree; 

            // 3. Calculate the Diff
            // We compare the Parent's Tree vs the Current Commit's Tree
            var patch = repo.Diff.Compare<Patch>(parentTree, commit.Tree);

            // 4. Convert to our simple list
            return patch.Select(change => new FileDiff(
                change.Path,
                change.Status.ToString(),
                change.Patch // This string contains the actual "+ code / - code"
            )).ToList();
        }
    }

    public string GetFileContent(string repoName, string branchName, string relativePath)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        using (var repo = new Repository(repoPath))
        {
            // 1. Find the branch
            var branch = repo.Branches[branchName];
            if (branch == null) throw new Exception($"Branch {branchName} not found");

            // 2. Get the latest commit on that branch
            var commit = branch.Tip;

            // 3. Find the file inside that commit
            // LibGit2Sharp lets us look up entries by path!
            var treeEntry = commit[relativePath];
            
            if (treeEntry == null)
            {
                throw new FileNotFoundException($"File '{relativePath}' not found in branch '{branchName}'.");
            }

            // 4. Get the "Blob" (The actual file data)
            var blob = treeEntry.Target as Blob;

            if (blob == null)
            {
                 // This happens if the user tries to read a Folder instead of a File
                throw new Exception($"Path '{relativePath}' is not a file.");
            }

            // 5. Read the content as text
            // Note: This works for code/text. It will look weird for Images.
            return blob.GetContentText();
        }
    }
}

public record SimpleCommit(
    string Sha, 
    string Message, 
    string Author, 
    DateTimeOffset Date
);

public record FileDiff(
    string Path, 
    string ChangeKind, 
    string Patch
);