using SampleProject.Models;

namespace SampleProject.Services;

public class UserService
{
    private readonly List<User> _users = new();
    private readonly ILogger _logger;

    public UserService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        await Task.Delay(10); // Simulate async operation
        _logger.Log($"Getting user with id {id}");
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public async Task<User> CreateUserAsync(string name, string email)
    {
        var user = new User
        {
            Id = _users.Count + 1,
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _users.Add(user);
        _logger.Log($"Created user: {user.Name}");
        return await Task.FromResult(user);
    }

    public void DeactivateUser(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            user.IsActive = false;
            _logger.Log($"Deactivated user: {user.Name}");
        }
    }

    public List<User> GetAllActiveUsers()
    {
        return _users.Where(u => u.IsActive).ToList();
    }

    public int UserCount => _users.Count;
}

public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }
}

public static class UserServiceExtensions
{
    public static bool HasUsers(this UserService service)
    {
        return service.UserCount > 0;
    }
}
