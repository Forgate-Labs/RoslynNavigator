namespace SampleProject.Services;

public class DataProcessor
{
    private readonly string _connectionString;
    private static readonly int MaxRetries = 3;

    public DataProcessor(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProcessResult> ProcessDataAsync(string input)
    {
        for (int retry = 0; retry < MaxRetries; retry++)
        {
            try
            {
                await Task.Delay(100);
                var processed = TransformData(input);
                return new ProcessResult { Success = true, Data = processed };
            }
            catch (Exception ex)
            {
                if (retry == MaxRetries - 1)
                {
                    return new ProcessResult { Success = false, Error = ex.Message };
                }
            }
        }
        return new ProcessResult { Success = false, Error = "Unknown error" };
    }

    private string TransformData(string input)
    {
        return input.ToUpperInvariant();
    }

    public bool ValidateInput(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.Length <= 1000;
    }
}

public class ProcessResult
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
}
