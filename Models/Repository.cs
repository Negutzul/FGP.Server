using System;

namespace FGP.Server.Models;

public class Repository
{
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;

        // Foreign Key to User
        public int OwnerId { get; set; }
        public User? Owner { get; set; }
}
