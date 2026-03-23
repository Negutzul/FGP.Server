using Microsoft.AspNetCore.Mvc;
using FGP.Server;             

namespace FGP.Server.Controllers; 

[ApiController]
[Route("api/[controller]")] 
public class ReposController : ControllerBase
{
    private readonly GitService _gitService;

    public ReposController(GitService gitService)
    {
        _gitService = gitService;
    }

    [HttpGet("{repoName}/branches")]
    public IActionResult GetBranches(string repoName)
    {
        try
        {
            var branches = _gitService.GetBranches(repoName);
            
            return Ok(branches);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{repoName}/branches")]
    public IActionResult CreateBranch(string repoName, [FromBody] CreateBranchRequest request)
    {
        try
        {
            _gitService.CreateBranch(repoName, request.BranchName, request.SourceBranch);
            return Ok(new { Message = $"Branch '{request.BranchName}' created from '{request.SourceBranch}'." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{repoName}/branches/{branchName}")]
    public IActionResult DeleteBranch(string repoName, string branchName)
    {
        try
        {
            _gitService.DeleteBranch(repoName, branchName);
            return Ok(new { Message = $"Branch '{branchName}' deleted." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{repoName}/branches/{branchName}/commits")]
    public IActionResult GetCommits(string repoName, string branchName, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var commits = _gitService.GetCommitsForBranch(repoName, branchName, page, pageSize);
            return Ok(commits);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{repoName}/commits/{sha}/diff")]
    public IActionResult GetCommitDiff(string repoName, string sha)
    {
        try
        {
            var diffs = _gitService.GetDiffForCommit(repoName, sha);
            return Ok(diffs);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{repoName}/branches/{branchName}/files/{*path}")]
    public IActionResult GetFileContent(string repoName, string branchName, string path)
    {
        try
        {
            var content = _gitService.GetFileContent(repoName, branchName, path);
            
            return Ok(new { Content = content });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    public IActionResult GetAllRepos()
    {
        try
        {
            var repos = _gitService.GetRepositories();
            return Ok(repos);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{repoName}/compare")]
    public IActionResult CompareBranches(string repoName, [FromQuery] string source, [FromQuery] string target)
    {
        try
        {
            var result = _gitService.CompareBranches(repoName, source, target);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    public IActionResult CreateRepo([FromBody] CreateRepoRequest request)
    {
        try
        {
            _gitService.CreateRepository(request.RepoName);
            return Ok(new { Message = $"Repository '{request.RepoName}' created successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{repoName}")]
    public IActionResult DeleteRepo(string repoName)
    {
        try
        {
            _gitService.DeleteRepository(repoName);
            return Ok(new { Message = $"Repository '{repoName}' deleted successfully." });
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{repoName}/branches/{branchName}/tree")]
    public IActionResult GetFileTree(string repoName, string branchName, [FromQuery] string path = "")
    {
        try
        {
            var entries = _gitService.GetFileTree(repoName, branchName, path);
            var result = entries.Select(e => new TreeEntryDto(
                e.Name,
                e.Path,
                e.TargetType.ToString()
            ));
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{repoName}/upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> UploadBundle(string repoName, IFormFile bundle)
    {
        if (bundle == null || bundle.Length == 0)
            return BadRequest("No bundle file provided.");

        try
        {
            using var stream = bundle.OpenReadStream();
            await _gitService.ReceiveBundleAsync(repoName, stream);
            return Ok(new { Message = $"Bundle applied to '{repoName}' successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest($"Upload failed: {ex.Message}");
        }
    }

    [HttpGet("{repoName}/download")]
    public async Task<IActionResult> DownloadBundle(string repoName, [FromQuery] string branch = "main")
    {
        string? tempBundle = null;
        try
        {
            tempBundle = await _gitService.CreateBundleAsync(repoName, branch);

            byte[] bundleBytes = await System.IO.File.ReadAllBytesAsync(tempBundle);

            string fileName = $"{repoName}-{branch}.bundle";
            return File(bundleBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Download failed: {ex.Message}");
        }
        finally
        {
            if (tempBundle != null && System.IO.File.Exists(tempBundle))
                System.IO.File.Delete(tempBundle);
        }
    }
}