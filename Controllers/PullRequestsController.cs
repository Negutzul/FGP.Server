using Microsoft.AspNetCore.Mvc;
using FGP.Server.Data;
using FGP.Server.Models;

namespace FGP.Server.Controllers;

[ApiController]
[Route("api/[controller]")] // URL: /api/PullRequests
public class PullRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    // Inject the Database Manager
    public PullRequestsController(AppDbContext db)
    {
        _db = db;
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

    // GET: api/PullRequests
    [HttpGet]
    public IActionResult GetAllPrs()
    {
        // Return the whole list
        return Ok(_db.PullRequests.ToList());
    }
}