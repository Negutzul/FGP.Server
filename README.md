# FGP Server

**Federated Git Protocol** — A custom, self-hosted alternative to GitHub. FGP allows you to host Git repositories, browse code, manage Pull Requests, and push/pull using standard Git commands, all through your own server.

## Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Server | ASP.NET Core (.NET 10) | Web API exposing all endpoints |
| Git Engine | LibGit2Sharp | C# wrapper for libgit2 — reads `.git` folders directly |
| Database | SQLite + Entity Framework Core | Stores metadata Git doesn't support (PRs, comments) |
| Git Protocol | `git http-backend` (CGI) | Enables native `git clone` and `git push` over HTTP |
| Docs/Test | Swagger UI | Interactive API testing at `/swagger` |

## Running the Server

```bash
cd FGP.Server
dotnet run
```

The server starts at `http://localhost:5298`. Swagger UI is available at `http://localhost:5298/swagger`.

---

## Project Structure

```
FGP.Server/
├── Controllers/
│   ├── ReposController.cs          # REST API for repository operations
│   ├── PullRequestsController.cs   # REST API for Pull Request management
│   └── GitSmartHttpController.cs   # Git Smart HTTP protocol (clone/push)
├── Services/
│   └── GitService.cs               # Core Git logic using LibGit2Sharp
├── Models/
│   ├── PullRequest.cs              # Database model for PRs
│   └── PrComment.cs                # Database model for PR comments
├── Data/
│   └── AppDbContext.cs             # Entity Framework database context
├── Program.cs                      # App startup and dependency injection
└── app.db                          # SQLite database file
```

---

## API Endpoints

### 1. Repository Browsing (`ReposController.cs`)

#### `GET /api/repos`
List all Git repositories in the server's base path.

**Response:**
```json
["FGP.Server", "FGP.CLI"]
```

---

#### `POST /api/repos`
Create a new empty Git repository (runs `git init`).

**Request Body:**
```json
{ "repoName": "MyNewProject" }
```

**Response:**
```json
{ "message": "Repository 'MyNewProject' created successfully." }
```

---

#### `DELETE /api/repos/{repoName}`
Delete a repository from disk.

**Example:** `DELETE /api/repos/MyNewProject`

**Response:**
```json
{ "message": "Repository 'MyNewProject' deleted successfully." }
```

---

#### `GET /api/repos/{repoName}/branches`
List all local branches in a repository.

**Example:** `GET /api/repos/FGP.Server/branches`

**Response:**
```json
["main", "feature-test"]
```

---

#### `GET /api/repos/{repoName}/branches/{branchName}/commits`
List commits on a branch with pagination.

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 10)

**Example:** `GET /api/repos/FGP.Server/branches/main/commits?page=1&pageSize=5`

**Response:**
```json
[
  {
    "sha": "abc123...",
    "message": "Initial commit",
    "author": "klu",
    "date": "2025-12-05T07:58:11+00:00"
  }
]
```

---

#### `GET /api/repos/{repoName}/branches/{branchName}/tree`
List files and folders at a path in a branch (like GitHub's file browser).

**Query Parameters:**
- `path` (default: `""` = root)

**Example:** `GET /api/repos/FGP.Server/branches/main/tree?path=Controllers`

**Response:**
```json
[
  { "name": "ReposController.cs", "path": "Controllers/ReposController.cs", "type": "Blob" },
  { "name": "PullRequestsController.cs", "path": "Controllers/PullRequestsController.cs", "type": "Blob" }
]
```

> `"Blob"` = file, `"Tree"` = folder.

---

#### `GET /api/repos/{repoName}/branches/{branchName}/files/{path}`
Read the content of a single file.

**Example:** `GET /api/repos/FGP.Server/branches/main/files/Program.cs`

**Response:**
```json
{ "content": "using FGP.Server;\nusing FGP.Server.Data;\n..." }
```

---

#### `GET /api/repos/{repoName}/commits/{sha}/diff`
View the diff (changed files) for a specific commit.

**Example:** `GET /api/repos/FGP.Server/commits/abc123/diff`

**Response:**
```json
[
  {
    "path": "Program.cs",
    "changeKind": "Modified",
    "patch": "@@ -1,3 +1,4 @@\n+using FGP.Server;\n..."
  }
]
```

---

### 2. Bundle Transfer (`ReposController.cs`)

#### `POST /api/repos/{repoName}/upload`
Upload a Git bundle file to apply commits/branches to a repository.

**Content-Type:** `multipart/form-data`  
**Form field:** `bundle` (the `.bundle` file)

**Example:**
```bash
# Create a bundle
git bundle create test.bundle feature-test

# Upload it
curl -X POST "http://localhost:5298/api/Repos/FGP.Server/upload" \
  -F "bundle=@test.bundle"
```

**Response:**
```json
{ "message": "Bundle applied to 'FGP.Server' successfully." }
```

---

#### `GET /api/repos/{repoName}/download`
Download a branch as a Git bundle file.

**Query Parameters:**
- `branch` (default: `"main"`)

**Example:**
```bash
curl "http://localhost:5298/api/Repos/FGP.Server/download?branch=main" -o repo.bundle
```

**Response:** Binary `.bundle` file download.

---

### 3. Pull Requests (`PullRequestsController.cs`)

#### `POST /api/PullRequests`
Create a new Pull Request. This stores metadata in the database — it does not merge anything yet.

**Request Body:**
```json
{
  "repoName": "FGP.Server",
  "title": "Add login feature",
  "description": "Implements user authentication",
  "sourceBranch": "feature-login",
  "targetBranch": "main"
}
```

**Response:**
```json
{
  "id": 1,
  "repoName": "FGP.Server",
  "title": "Add login feature",
  "description": "Implements user authentication",
  "sourceBranch": "feature-login",
  "targetBranch": "main",
  "isOpen": true
}
```

---

#### `GET /api/PullRequests`
List all Pull Requests across all repositories.

**Response:**
```json
[
  { "id": 1, "repoName": "FGP.Server", "title": "Add login feature", "isOpen": true },
  { "id": 2, "repoName": "FGP.Server", "title": "Fix bug", "isOpen": false }
]
```

---

#### `POST /api/PullRequests/{id}/merge`
Merge a Pull Request. This performs an actual Git merge using LibGit2Sharp and marks the PR as closed in the database.

**Example:** `POST /api/PullRequests/1/merge`

**Response (success):**
```json
{ "message": "Merged successfully!", "prId": 1 }
```

**Response (conflict):**
```json
"Merge Failed: Merge conflict detected! Automated merge failed."
```

---

#### `POST /api/PullRequests/{id}/comments`
Add a comment to a Pull Request.

**Request Body:**
```json
{ "author": "klu", "body": "Looks good to me! Ship it." }
```

**Response:**
```json
{
  "id": 1,
  "pullRequestId": 3,
  "author": "klu",
  "body": "Looks good to me! Ship it.",
  "createdAt": "2026-03-03T20:11:58Z"
}
```

---

#### `GET /api/PullRequests/{id}/comments`
Get all comments on a Pull Request, sorted by date (oldest first).

**Example:** `GET /api/PullRequests/3/comments`

**Response:**
```json
[
  { "id": 1, "pullRequestId": 3, "author": "klu", "body": "Looks good!", "createdAt": "..." },
  { "id": 2, "pullRequestId": 3, "author": "dev2", "body": "Add tests first", "createdAt": "..." }
]
```

---

### 4. Git Smart HTTP Protocol (`GitSmartHttpController.cs`)

These endpoints implement the standard Git Smart HTTP protocol, allowing native `git clone` and `git push` commands to work against the FGP server.

#### `GET /api/repos/{repoName}.git/info/refs?service=...`
Git's discovery endpoint. Returns the list of branches and their commit SHAs.

#### `POST /api/repos/{repoName}.git/git-upload-pack`
Handles `git clone` and `git fetch`. Streams packed Git objects to the client.

#### `POST /api/repos/{repoName}.git/git-receive-pack`
Handles `git push`. Receives packed Git objects from the client and applies them.

**Usage:**
```bash
# Clone a repo from FGP
git clone http://localhost:5298/api/repos/FGP.Server.git

# Push to FGP
git push origin main
```

> **Note:** Target repositories must have `receive.denyCurrentBranch` set to `ignore` to accept pushes to checked-out branches:
> ```bash
> git -C /path/to/repo config receive.denyCurrentBranch ignore
> ```

---

## Code Architecture

### `GitService.cs` — The Git Engine

All Git operations go through this service. It is registered as a **Singleton** in `Program.cs`.

| Method | What it does |
|--------|-------------|
| `GetRepositories()` | Scans the base path for folders containing `.git` |
| `CreateRepository(name)` | Runs `git init` to create a new repo |
| `DeleteRepository(name)` | Force-deletes a repo folder (handles read-only `.git` files) |
| `GetBranches(repo)` | Lists local branches via LibGit2Sharp |
| `GetCommitsForBranch(repo, branch, page, pageSize)` | Paginated commit history |
| `GetDiffForCommit(repo, sha)` | Compares a commit to its parent to produce a diff |
| `GetFileContent(repo, branch, path)` | Reads a file's content from a specific branch |
| `GetFileTree(repo, branch, folder)` | Lists files/folders at a path in a branch's tree |
| `MergeBranches(repo, source, target)` | Performs a Git merge via LibGit2Sharp |
| `ReceiveBundleAsync(repo, stream)` | Applies an uploaded `.bundle` file via `git bundle unbundle` |
| `CreateBundleAsync(repo, branch)` | Creates a `.bundle` file via `git bundle create` |

### `GitSmartHttpController.cs` — The Protocol Bridge

Acts as a proxy between HTTP requests and Git's `http-backend` CGI program. It translates incoming HTTP requests into CGI environment variables, starts a `git http-backend` process, pipes data in/out, and streams the response back to the client.

### `PullRequest.cs` — The Database Model

```
PullRequest
├── Id              (int, auto-generated)
├── RepoName        (string)
├── Title           (string)
├── Description     (string)
├── SourceBranch    (string)
├── TargetBranch    (string)
└── IsOpen          (bool, default: true)
```

### `PrComment.cs` — The Comment Model

```
PrComment
├── Id              (int, auto-generated)
├── PullRequestId   (int, links to PullRequest)
├── Author          (string)
├── Body            (string)
└── CreatedAt       (DateTime, UTC)
```

### `AppDbContext.cs` — Entity Framework Context

Manages the SQLite database (`app.db`) with tables for:
- `Users`
- `Repositories`
- `Branches` (with concurrency token on `HeadHash`)
- `PullRequests`
- `PrComments`

### `Program.cs` — Startup

Registers services and middleware:
- `GitService` as Singleton
- `AppDbContext` with SQLite connection
- Swagger UI (development only)
- HTTPS redirection and authorization
