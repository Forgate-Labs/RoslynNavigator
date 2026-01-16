namespace SampleProject.Models;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class UserRole
{
    public int Id { get; set; }
    public required string RoleName { get; set; }
    public required User User { get; set; }
}
