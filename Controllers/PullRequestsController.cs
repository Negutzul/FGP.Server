using Microsoft.AspNetCore.Mvc;
using FGP.Server.Data;
using FGP.Server.Models;

namespace FGP.Server.Controllers;

[ApiController]
[Route("api/[controller]")] // URL: /api/PullRequests
public class PullRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GitService _gitService;

    // Inject the Database Manager and Git Service
    public PullRequestsController(AppDbContext db, GitService gitService)
    {
        _db = db;
        _gitService = gitService;
    }

    // POST: api/PullRequests
    [HttpPost]
    public IActionResult CreatePr([FromBody] CreatePrRequest request)
    {
        // 1. Convert the "Input Form" into a real Database Object
        var newPr = new PullRequest
        {
            RepoName = request.RepoName,
            Title = request.Title,
            Description = request.Description,
            SourceBranch = request.SourceBranch,
            TargetBranch = request.TargetBranch,
            IsOpen = true // Default to Open
        };

        // 2. Add it to the "Spreadsheet" (in memory)
        _db.PullRequests.Add(newPr);

        // 3. Save changes to the actual file (app.db)
        _db.SaveChanges();

        // 4. Return the result with the new ID
        return Ok(newPr);
    }

    [HttpPost("{id}/merge")]
    public IActionResult MergePr(int id)
    {
        // 1. Find the PR in the Database
        var pr = _db.PullRequests.Find(id);
        if (pr == null) return NotFound("PR not found");

        if (!pr.IsOpen) return BadRequest("This PR is already closed/merged.");

        try 
        {
            // 2. Perform the actual Git Merge
            _gitService.MergeBranches(pr.RepoName, pr.SourceBranch, pr.TargetBranch);

            // 3. Update the Database Status
            pr.IsOpen = false;
            _db.SaveChanges();

            return Ok(new { Message = "Merged successfully!", PrId = id });
        }
        catch (Exception ex)
        {
            return BadRequest($"Merge Failed: {ex.Message}");
        }
    }

    // GET: api/PullRequests
    [HttpGet]
    public IActionResult GetAllPrs()
    {
        // Return the whole list
        return Ok(_db.PullRequests.ToList());
    }
}