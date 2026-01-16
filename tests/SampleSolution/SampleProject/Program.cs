using SampleProject.Services;

namespace SampleProject;

public class Program
{
    public static async Task Main(string[] args)
    {
        var logger = new ConsoleLogger();
        var userService = new UserService(logger);

        // Test CreateUserAsync
        var user = await userService.CreateUserAsync("John", "john@example.com");
        Console.WriteLine($"Created user: {user.Name}");

        // Test GetUserAsync
        var fetchedUser = await userService.GetUserAsync(user.Id);
        Console.WriteLine($"Fetched user: {fetchedUser?.Name}");

        // Test DataProcessor
        var processor = new DataProcessor("connection-string");
        var result = await processor.ProcessDataAsync("hello");
        Console.WriteLine($"Processed: {result.Data}");
    }
}
