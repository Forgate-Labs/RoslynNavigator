using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoslynNavigator.Commands;
using RoslynNavigator.Models;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var rootCommand = new RootCommand("Roslyn Navigator - Semantic C# code navigation tool");

// Shared options
var solutionOption = new Option<string>("--solution", "Path to .sln file") { IsRequired = true };
var fileOption = new Option<string>("--file", "Path to .cs file");
var classOption = new Option<string>("--class", "Class name");

// list-class command
var listClassCommand = new Command("list-class", "Get class structure overview");
listClassCommand.AddOption(solutionOption);
listClassCommand.AddOption(fileOption);
listClassCommand.AddOption(classOption);
listClassCommand.SetHandler(async (string solution, string file, string className) =>
{
    try
    {
        var result = await ListClassCommand.ExecuteAsync(solution, file, className);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("list_class_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, fileOption, classOption);

// find-symbol command
var findSymbolCommand = new Command("find-symbol", "Locate any symbol in the solution");
var nameOption = new Option<string>("--name", "Symbol name to find") { IsRequired = true };
var kindOption = new Option<string>("--kind", "Symbol kind (class/method/property)");
findSymbolCommand.AddOption(solutionOption);
findSymbolCommand.AddOption(nameOption);
findSymbolCommand.AddOption(kindOption);
findSymbolCommand.SetHandler(async (string solution, string name, string? kind) =>
{
    try
    {
        var result = await FindSymbolCommand.ExecuteAsync(solution, name, kind);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_symbol_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, nameOption, kindOption);

// get-method command
var getMethodCommand = new Command("get-method", "Read a specific method's code");
var methodOption = new Option<string>("--method", "Method name") { IsRequired = true };
getMethodCommand.AddOption(solutionOption);
getMethodCommand.AddOption(fileOption);
getMethodCommand.AddOption(classOption);
getMethodCommand.AddOption(methodOption);
getMethodCommand.SetHandler(async (string solution, string? file, string? className, string method) =>
{
    try
    {
        var result = await GetMethodCommand.ExecuteAsync(solution, file, className, method);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("get_method_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, fileOption, classOption, methodOption);

// find-usages command
var findUsagesCommand = new Command("find-usages", "Find where a symbol is used");
var symbolOption = new Option<string>("--symbol", "Symbol name (e.g., ClassName.MethodName)") { IsRequired = true };
findUsagesCommand.AddOption(solutionOption);
findUsagesCommand.AddOption(symbolOption);
findUsagesCommand.SetHandler(async (string solution, string symbol) =>
{
    try
    {
        var result = await FindUsagesCommand.ExecuteAsync(solution, symbol);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_usages_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, symbolOption);

// list-classes command
var listClassesCommand = new Command("list-classes", "Get all classes in a namespace");
var namespaceOption = new Option<string>("--namespace", "Namespace to filter") { IsRequired = true };
listClassesCommand.AddOption(solutionOption);
listClassesCommand.AddOption(namespaceOption);
listClassesCommand.SetHandler(async (string solution, string ns) =>
{
    try
    {
        var result = await ListClassesCommand.ExecuteAsync(solution, ns);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("list_classes_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, namespaceOption);

// get-namespace-structure command
var getNamespaceStructureCommand = new Command("get-namespace-structure", "Get complete namespace hierarchy");
var projectOption = new Option<string>("--project", "Project name") { IsRequired = true };
getNamespaceStructureCommand.AddOption(solutionOption);
getNamespaceStructureCommand.AddOption(projectOption);
getNamespaceStructureCommand.SetHandler(async (string solution, string project) =>
{
    try
    {
        var result = await GetNamespaceStructureCommand.ExecuteAsync(solution, project);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("get_namespace_structure_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, projectOption);

// Add all commands to root
rootCommand.AddCommand(listClassCommand);
rootCommand.AddCommand(findSymbolCommand);
rootCommand.AddCommand(getMethodCommand);
rootCommand.AddCommand(findUsagesCommand);
rootCommand.AddCommand(listClassesCommand);
rootCommand.AddCommand(getNamespaceStructureCommand);

return await rootCommand.InvokeAsync(args);

void OutputError(string code, string message)
{
    var error = new ErrorResult
    {
        Error = new ErrorInfo
        {
            Code = code,
            Message = message
        }
    };
    Console.WriteLine(JsonSerializer.Serialize(error, jsonOptions));
}
