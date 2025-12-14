using System;
using System.ComponentModel.DataAnnotations;

namespace FGP.Server.Models;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = "main";

    // This is the magic field for the Race Condition check
    [ConcurrencyCheck]
    public string HeadHash { get; set; } = string.Empty;

    // Foreign Key to Repository
    public int RepositoryId { get; set; }
    public Repository? Repository { get; set; }
}
