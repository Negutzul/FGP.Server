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
    public IActionResult GetCommits(string repoName, string branchName)
    {
        try
        {
            var commits = _gitService.GetCommitsForBranch(repoName, branchName);
            return Ok(commits);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }
}