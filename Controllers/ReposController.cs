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
}