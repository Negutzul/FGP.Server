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

    public string CreateRepository(string repoName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        if (Directory.Exists(repoPath))
            throw new Exception($"Repository '{repoName}' already exists.");

        Directory.CreateDirectory(repoPath);
        Repository.Init(repoPath);

        return repoPath;
    }

    public void DeleteRepository(string repoName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        if (!Directory.Exists(repoPath))
            throw new Exception($"Repository '{repoName}' not found.");

        ForceDeleteDirectory(repoPath);
    }

    private static void ForceDeleteDirectory(string path)
    {
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(path, recursive: true);
    }

    public List<TreeEntry> GetFileTree(string repoName, string branchName, string folderPath = "")
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        using var repo = new Repository(repoPath);

        var branch = repo.Branches[branchName]
            ?? throw new Exception($"Branch '{branchName}' not found.");

        Tree tree = branch.Tip.Tree;

        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            var entry = branch.Tip[folderPath];
            if (entry == null)
                throw new Exception($"Path '{folderPath}' not found.");
            if (entry.TargetType != TreeEntryTargetType.Tree)
                throw new Exception($"Path '{folderPath}' is a file, not a directory.");
            tree = (Tree)entry.Target;
        }

        return tree.ToList();
    }

    public BranchComparisonResult CompareBranches(string repoName, string sourceBranchName, string targetBranchName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        using var repo = new Repository(repoPath);

        var source = repo.Branches[sourceBranchName]
            ?? throw new Exception($"Branch '{sourceBranchName}' not found.");
        var target = repo.Branches[targetBranchName]
            ?? throw new Exception($"Branch '{targetBranchName}' not found.");

        // Get the diff between the two branch tips
        var patch = repo.Diff.Compare<Patch>(target.Tip.Tree, source.Tip.Tree);

        var files = patch.Select(change => new FileDiff(
            change.Path,
            change.Status.ToString(),
            change.Patch
        )).ToList();

        // Get commits in source that aren't in target
        var filter = new CommitFilter
        {
            IncludeReachableFrom = source,
            ExcludeReachableFrom = target,
            SortBy = CommitSortStrategies.Topological
        };

        var commits = repo.Commits.QueryBy(filter)
            .Select(c => new SimpleCommit(c.Sha, c.MessageShort, c.Author.Name, c.Author.When))
            .ToList();

        return new BranchComparisonResult(files, commits, files.Count, commits.Count);
    }

    public void CreateBranch(string repoName, string branchName, string sourceBranchName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        using var repo = new Repository(repoPath);

        var source = repo.Branches[sourceBranchName]
            ?? throw new Exception($"Source branch '{sourceBranchName}' not found.");

        if (repo.Branches[branchName] != null)
            throw new Exception($"Branch '{branchName}' already exists.");

        repo.CreateBranch(branchName, source.Tip);
    }

    public void DeleteBranch(string repoName, string branchName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        using var repo = new Repository(repoPath);

        if (branchName == "main" || branchName == "master")
            throw new Exception($"Cannot delete the '{branchName}' branch.");

        var branch = repo.Branches[branchName]
            ?? throw new Exception($"Branch '{branchName}' not found.");

        if (branch.IsCurrentRepositoryHead)
            throw new Exception("Cannot delete the currently checked-out branch.");

        repo.Branches.Remove(branch);
    }

    public string MergeBranches(string repoName, string sourceBranch, string targetBranch, string mergedBy = "FGP Server")
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);
        
        using (var repo = new Repository(repoPath))
        {
            // 1. Get the branches
            var source = repo.Branches[sourceBranch];
            var target = repo.Branches[targetBranch];

            if (source == null) 
                throw new Exception($"Source branch '{sourceBranch}' does not exist.");
            if (target == null) 
                throw new Exception($"Target branch '{targetBranch}' does not exist.");

            var merger = new Signature(mergedBy, $"{mergedBy.ToLower().Replace(" ", "")}@fgp.dev", DateTime.Now);

            // 2. Check if we can fast-forward
            var mergeBase = repo.ObjectDatabase.FindMergeBase(target.Tip, source.Tip);

            if (mergeBase != null && mergeBase.Sha == target.Tip.Sha)
            {
                // Fast-forward: target is an ancestor of source, just move the pointer
                repo.Refs.UpdateTarget(repo.Refs[target.CanonicalName], source.Tip.Id);
                return $"Fast-forward merge: {targetBranch} updated to {source.Tip.Sha.Substring(0, 7)}";
            }

            // 3. Non-fast-forward: create a merge commit
            // Checkout the target branch first
            Commands.Checkout(repo, target);

            var result = repo.Merge(source, merger, new MergeOptions 
            { 
                FastForwardStrategy = FastForwardStrategy.NoFastForward 
            });

            if (result.Status == MergeStatus.Conflicts)
            {
                repo.Reset(ResetMode.Hard);
                throw new Exception("Merge conflict detected! Automated merge failed.");
            }

            return $"Merge commit created on {targetBranch}: {result.Commit?.Sha?.Substring(0, 7) ?? "unknown"}";
        }
    }

    public async Task ReceiveBundleAsync(string repoName, Stream bundleStream)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        if (!Directory.Exists(repoPath))
            throw new Exception($"Repository not found: {repoPath}");

        string tempBundle = Path.Combine(Path.GetTempPath(), $"fgp-{Guid.NewGuid()}.bundle");

        try
        {
            using (var fs = File.Create(tempBundle))
                await bundleStream.CopyToAsync(fs);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"bundle unbundle \"{tempBundle}\"",
                WorkingDirectory = repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi)!;
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"git bundle unbundle failed: {stderr}");
        }
        finally
        {
            if (File.Exists(tempBundle))
                File.Delete(tempBundle);
        }
    }

    public async Task<string> CreateBundleAsync(string repoName, string branchName)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        if (!Directory.Exists(repoPath))
            throw new Exception($"Repository not found: {repoPath}");

        string tempBundle = Path.Combine(Path.GetTempPath(), $"fgp-{Guid.NewGuid()}.bundle");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"bundle create \"{tempBundle}\" {branchName}",
            WorkingDirectory = repoPath,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            if (File.Exists(tempBundle)) File.Delete(tempBundle);
            throw new Exception($"git bundle create failed: {stderr}");
        }

        return tempBundle;
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

public record CreateRepoRequest(string RepoName);
public record CreateBranchRequest(string BranchName, string SourceBranch);
public record TreeEntryDto(string Name, string Path, string Type);
public record BranchComparisonResult(List<FileDiff> Files, List<SimpleCommit> Commits, int FilesChanged, int CommitCount);