using RoslynNavigator.Models;
using RoslynNavigator.Services;

namespace RoslynNavigator.Commands;

public static class ListClassCommand
{
    public static async Task<ClassStructure> ExecuteAsync(string solutionPath, string filePath, string className)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path is required for list-class command");
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name is required for list-class command");

        var solution = await WorkspaceService.GetSolutionAsync(solutionPath);
        var document = await WorkspaceService.FindDocumentAsync(solution, filePath, solutionPath);

        if (document == null)
            throw new FileNotFoundException($"File not found: {filePath}");

        var result = await RoslynAnalyzer.AnalyzeClassAsync(document, className, solutionPath);

        if (result == null)
            throw new InvalidOperationException($"Class '{className}' not found in file {filePath}");

        return result;
    }
}
