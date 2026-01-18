namespace SampleProject.Services;

/// <summary>
/// Interface for calculator operations - tests find-implementations
/// </summary>
public interface ICalculator
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    int Multiply(int a, int b);
    double Divide(int a, int b);
}

/// <summary>
/// Basic calculator implementation - tests multiple commands
/// </summary>
public class Calculator : ICalculator
{
    private readonly ILogger _logger;

    public Calculator(ILogger logger)
    {
        _logger = logger;
    }

    public Calculator() : this(new ConsoleLogger())
    {
    }

    public virtual int Add(int a, int b)
    {
        _logger.Log($"Adding {a} + {b}");
        return a + b;
    }

    public virtual int Subtract(int a, int b)
    {
        _logger.Log($"Subtracting {a} - {b}");
        return a - b;
    }

    public int Multiply(int a, int b)
    {
        _logger.Log($"Multiplying {a} * {b}");
        return a * b;
    }

    public double Divide(int a, int b)
    {
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        _logger.Log($"Dividing {a} / {b}");
        return (double)a / b;
    }

    [Obsolete("Use Add method instead")]
    public int Sum(int a, int b) => Add(a, b);
}

/// <summary>
/// Scientific calculator - tests get-hierarchy (derived class)
/// </summary>
public class ScientificCalculator : Calculator
{
    public ScientificCalculator(ILogger logger) : base(logger)
    {
    }

    public override int Add(int a, int b)
    {
        // Scientific implementation with logging
        var result = base.Add(a, b);
        return result;
    }

    public double Power(double baseNum, double exponent)
    {
        return Math.Pow(baseNum, exponent);
    }

    public double SquareRoot(double number)
    {
        if (number < 0)
            throw new ArgumentException("Cannot calculate square root of negative number");
        return Math.Sqrt(number);
    }
}

/// <summary>
/// Alternative calculator implementation - tests find-implementations
/// </summary>
public struct FastCalculator : ICalculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public double Divide(int a, int b) => b == 0 ? 0 : (double)a / b;
}

/// <summary>
/// Demonstrates abstract classes - tests check-overridable
/// </summary>
public abstract class BaseProcessor
{
    public abstract void Process(string input);

    public virtual void Initialize()
    {
        Console.WriteLine("Initializing processor...");
    }

    public void Start()
    {
        Initialize();
        Console.WriteLine("Processor started");
    }
}

public class TextProcessor : BaseProcessor
{
    public override void Process(string input)
    {
        Console.WriteLine($"Processing text: {input}");
    }

    public sealed override void Initialize()
    {
        Console.WriteLine("Text processor initialized");
    }
}

/// <summary>
/// Test class for find-by-attribute with various attributes
/// </summary>
[Serializable]
public class AttributeTestClass
{
    [Obsolete("Use NewProperty instead")]
    public string? OldProperty { get; set; }

    public string? NewProperty { get; set; }

    [Obsolete("This method is deprecated")]
    public void DeprecatedMethod()
    {
        Console.WriteLine("Deprecated!");
    }

    public void CurrentMethod()
    {
        Console.WriteLine("Current method");
    }
}
