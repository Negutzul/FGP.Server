using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FGP.Server; 

public class GitService
{

    private readonly string _repoBasePath = @"C:\Users\klu\Desktop\Projects\FGP"; 

    public List<string> GetBranches(string repoName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        
        if (!Directory.Exists(repoPath)) 
        {
            throw new Exception($"Repository not found at: {repoPath}");
        }

        using (var repo = new Repository(repoPath))
        {
            return repo.Branches
                .Where(b => !b.IsRemote)  
                .Select(b => b.FriendlyName)
                .ToList();
        }
    }

    public List<SimpleCommit> GetCommitsForBranch(string repoName, string branchName, int page = 1, int pageSize = 10)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        using (var repo = new Repository(repoPath))
        {
            var branch = repo.Branches[branchName];
            if (branch == null)
            {
                throw new Exception($"Branch '{branchName}' not found.");
            }

            return branch.Commits
                .Skip((page - 1) * pageSize) // Skip the previous pages
                .Take(pageSize)              // Take only the chunk we need
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
            
            var commit = repo.Lookup<Commit>(commitSha);
            if (commit == null) throw new Exception("Commit not found");

            var parentCommit = commit.Parents.FirstOrDefault();
            var parentTree = parentCommit?.Tree; 

            var patch = repo.Diff.Compare<Patch>(parentTree, commit.Tree);

            return patch.Select(change => new FileDiff(
                change.Path,
                change.Status.ToString(),
                change.Patch 
            )).ToList();
        }
    }

    public string GetFileContent(string repoName, string branchName, string relativePath)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        using (var repo = new Repository(repoPath))
        {
            var branch = repo.Branches[branchName];
            if (branch == null) throw new Exception($"Branch {branchName} not found");

            var commit = branch.Tip;

            var treeEntry = commit[relativePath];
            
            if (treeEntry == null)
            {
                throw new FileNotFoundException($"File '{relativePath}' not found in branch '{branchName}'.");
            }

            var blob = treeEntry.Target as Blob;

            if (blob == null)
            {
                throw new Exception($"Path '{relativePath}' is not a file.");
            }

            return blob.GetContentText();
        }
    }
    public List<string> GetRepositories()
    {
        var directories = Directory.GetDirectories(_repoBasePath);

        var gitRepos = directories
            .Where(dir => Directory.Exists(Path.Combine(dir, ".git")))
            .Select(dir => Path.GetFileName(dir))
            .ToList();

        return gitRepos;
    }

    public MergeResult MergeBranches(string repoName, string sourceBranch, string targetBranch)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        
        using (var repo = new Repository(repoPath))
        {
            // 1. Get the branches
            var source = repo.Branches[sourceBranch];
            var target = repo.Branches[targetBranch];

            if (source == null || target == null) 
                throw new Exception("One of the branches does not exist.");

            // 2. We need a "Signature" (Who is merging this?)
            var merger = new Signature("FGP Server", "server@fgp.com", DateTime.Now);

            // 3. Checkout the target branch (e.g., 'main') so we can merge INTO it
            Commands.Checkout(repo, target);

            // 4. Perform the Merge
            var result = repo.Merge(source, merger, new MergeOptions 
            { 
                FastForwardStrategy = FastForwardStrategy.Default 
            });

            // 5. Check if it worked
            if (result.Status == MergeStatus.Conflicts)
            {
                // If conflict, abort (reset) to keep things clean for now
                repo.Reset(ResetMode.Hard); 
                throw new Exception("Merge conflict detected! Automated merge failed.");
            }

            return result; // Success
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