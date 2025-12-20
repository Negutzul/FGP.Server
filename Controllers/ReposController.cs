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
}