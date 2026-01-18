namespace SampleProject.Services;

/// <summary>
/// Application class that demonstrates usages - tests find-instantiations and find-callers
/// </summary>
public class Application
{
    private readonly ICalculator _calculator;
    private readonly UserService _userService;

    public Application()
    {
        // Tests find-instantiations
        var logger = new ConsoleLogger();
        _calculator = new Calculator(logger);
        _userService = new UserService(logger);
    }

    public Application(ICalculator calculator, UserService userService)
    {
        _calculator = calculator;
        _userService = userService;
    }

    public void Run()
    {
        // Tests find-callers for Calculator.Add
        var sum = _calculator.Add(5, 3);
        Console.WriteLine($"5 + 3 = {sum}");

        // Tests find-callers for Calculator.Subtract
        var diff = _calculator.Subtract(10, 4);
        Console.WriteLine($"10 - 4 = {diff}");

        // Tests find-callers for Calculator.Multiply
        var product = _calculator.Multiply(6, 7);
        Console.WriteLine($"6 * 7 = {product}");
    }

    public async Task RunAsync()
    {
        // Tests find-callers for UserService methods
        var user = await _userService.CreateUserAsync("John", "john@example.com");
        Console.WriteLine($"Created user: {user.Name}");

        var retrieved = await _userService.GetUserAsync(user.Id);
        Console.WriteLine($"Retrieved user: {retrieved?.Name}");

        var allUsers = _userService.GetAllActiveUsers();
        Console.WriteLine($"Active users: {allUsers.Count}");
    }
}

/// <summary>
/// Another class that uses Calculator - for testing find-instantiations
/// </summary>
public class MathHelper
{
    public int ComputeSum(int[] numbers)
    {
        var calc = new Calculator();
        var result = 0;
        foreach (var num in numbers)
        {
            result = calc.Add(result, num);
        }
        return result;
    }

    public static Calculator CreateCalculator()
    {
        return new Calculator(new ConsoleLogger());
    }

    public static ScientificCalculator CreateScientificCalculator()
    {
        return new ScientificCalculator(new ConsoleLogger());
    }
}
