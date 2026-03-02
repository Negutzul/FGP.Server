using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FGP.Server.Controllers;

[ApiController]
public class GitSmartHttpController : ControllerBase
{
    private readonly string _repoBasePath = @"C:\Users\klu\Desktop\Projects\FGP";

    [Route("api/repos/{repoName}.git/info/refs")]
    [HttpGet]
    public async Task GetInfoRefs(string repoName, [FromQuery] string service)
    {
        await RunGitHttpBackend(repoName, service);
    }

    [Route("api/repos/{repoName}.git/git-upload-pack")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task PostUploadPack(string repoName)
    {
        await RunGitHttpBackend(repoName, "git-upload-pack");
    }

    [Route("api/repos/{repoName}.git/git-receive-pack")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task PostReceivePack(string repoName)
    {
        await RunGitHttpBackend(repoName, "git-receive-pack");
    }

    private async Task RunGitHttpBackend(string repoName, string service)
    {
        string repoPath = Path.Combine(_repoBasePath, repoName);

        if (!Directory.Exists(repoPath))
        {
            Response.StatusCode = 404;
            return;
        }

        Response.ContentType = $"application/x-{service}-advertisement";

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "http-backend",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = repoPath
        };

        // Set the CGI Environment Variables required by git-http-backend
        psi.Environment["GIT_PROJECT_ROOT"] = _repoBasePath;
        psi.Environment["GIT_HTTP_EXPORT_ALL"] = "1";
        
        string pathAfterGit = Request.Path.Value!.Substring(Request.Path.Value.IndexOf(".git") + 4);
        psi.Environment["PATH_INFO"] = $"/{repoName}{pathAfterGit}";
        
        psi.Environment["QUERY_STRING"] = Request.QueryString.HasValue ? Request.QueryString.Value.Substring(1) : "";
        psi.Environment["REQUEST_METHOD"] = Request.Method;
        psi.Environment["CONTENT_TYPE"] = Request.ContentType ?? "";
        
        // This tells git that the user is authenticated, allowing git-receive-pack (pushes) to work
        psi.Environment["REMOTE_USER"] = "fgp_user";

        using var process = Process.Start(psi);
        if (process == null)
        {
            Response.StatusCode = 500;
            return;
        }

        // If it's a POST (like pushing code), we send the HTTP request body to Git's Standard Input
        if (Request.Method == "POST")
        {
            _ = Request.Body.CopyToAsync(process.StandardInput.BaseStream).ContinueWith(t => process.StandardInput.Close());
        }
        else
        {
            process.StandardInput.Close();
        }

        // Read the CGI headers from Git's output before copying the body
        using var reader = new StreamReader(process.StandardOutput.BaseStream, leaveOpen: true);
        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
            {
                var headerName = parts[0].Trim();
                var headerValue = parts[1].Trim();
                if (headerName.Equals("Status", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(headerValue.Split(' ')[0], out int status))
                    {
                        Response.StatusCode = status;
                    }
                }
                else
                {
                    Response.Headers[headerName] = headerValue;
                }
            }
        }

        // Send the rest of Git's output to the HTTP response body
        await process.StandardOutput.BaseStream.CopyToAsync(Response.Body);
        
        await process.WaitForExitAsync();
    }
}
