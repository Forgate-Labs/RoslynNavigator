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

// get-methods command
var getMethodsCommand = new Command("get-methods", "Extract multiple methods from a class at once");
var methodsOption = new Option<string>("--methods", "Comma-separated list of method names") { IsRequired = true };
getMethodsCommand.AddOption(solutionOption);
getMethodsCommand.AddOption(classOption);
getMethodsCommand.AddOption(methodsOption);
getMethodsCommand.SetHandler(async (string solution, string className, string methods) =>
{
    try
    {
        var result = await GetMethodsCommand.ExecuteAsync(solution, className, methods);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("get_methods_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, classOption, methodsOption);

// check-overridable command
var checkOverridableCommand = new Command("check-overridable", "Check if a method is virtual/override/abstract");
checkOverridableCommand.AddOption(solutionOption);
checkOverridableCommand.AddOption(classOption);
checkOverridableCommand.AddOption(methodOption);
checkOverridableCommand.SetHandler(async (string solution, string className, string method) =>
{
    try
    {
        var result = await CheckOverridableCommand.ExecuteAsync(solution, className, method);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("check_overridable_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, classOption, methodOption);

// get-constructor-deps command
var getConstructorDepsCommand = new Command("get-constructor-deps", "Analyze constructor dependencies for DI");
getConstructorDepsCommand.AddOption(solutionOption);
getConstructorDepsCommand.AddOption(classOption);
getConstructorDepsCommand.SetHandler(async (string solution, string className) =>
{
    try
    {
        var result = await GetConstructorDepsCommand.ExecuteAsync(solution, className);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("get_constructor_deps_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, classOption);

// find-implementations command
var findImplementationsCommand = new Command("find-implementations", "Find all implementations of an interface");
var interfaceOption = new Option<string>("--interface", "Interface name to find implementations of") { IsRequired = true };
findImplementationsCommand.AddOption(solutionOption);
findImplementationsCommand.AddOption(interfaceOption);
findImplementationsCommand.SetHandler(async (string solution, string interfaceName) =>
{
    try
    {
        var result = await FindImplementationsCommand.ExecuteAsync(solution, interfaceName);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_implementations_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, interfaceOption);

// find-instantiations command
var findInstantiationsCommand = new Command("find-instantiations", "Find where a class is instantiated");
findInstantiationsCommand.AddOption(solutionOption);
findInstantiationsCommand.AddOption(classOption);
findInstantiationsCommand.SetHandler(async (string solution, string className) =>
{
    try
    {
        var result = await FindInstantiationsCommand.ExecuteAsync(solution, className);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_instantiations_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, classOption);

// find-callers command
var findCallersCommand = new Command("find-callers", "Find methods that call another method");
findCallersCommand.AddOption(solutionOption);
findCallersCommand.AddOption(symbolOption);
findCallersCommand.SetHandler(async (string solution, string symbol) =>
{
    try
    {
        var result = await FindCallersCommand.ExecuteAsync(solution, symbol);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_callers_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, symbolOption);

// get-hierarchy command
var getHierarchyCommand = new Command("get-hierarchy", "Get class inheritance hierarchy");
getHierarchyCommand.AddOption(solutionOption);
getHierarchyCommand.AddOption(classOption);
getHierarchyCommand.SetHandler(async (string solution, string className) =>
{
    try
    {
        var result = await GetHierarchyCommand.ExecuteAsync(solution, className);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("get_hierarchy_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, classOption);

// find-by-attribute command
var findByAttributeCommand = new Command("find-by-attribute", "Find members decorated with a specific attribute");
var attributeOption = new Option<string>("--attribute", "Attribute name (e.g., Obsolete, Given, HttpGet)") { IsRequired = true };
var patternOption = new Option<string?>("--pattern", "Optional text pattern to match in attribute arguments");
findByAttributeCommand.AddOption(solutionOption);
findByAttributeCommand.AddOption(attributeOption);
findByAttributeCommand.AddOption(patternOption);
findByAttributeCommand.SetHandler(async (string solution, string attribute, string? pattern) =>
{
    try
    {
        var result = await FindByAttributeCommand.ExecuteAsync(solution, attribute, pattern);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_by_attribute_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, attributeOption, patternOption);

// Add all commands to root
rootCommand.AddCommand(listClassCommand);
rootCommand.AddCommand(findSymbolCommand);
rootCommand.AddCommand(getMethodCommand);
rootCommand.AddCommand(findUsagesCommand);
rootCommand.AddCommand(listClassesCommand);
rootCommand.AddCommand(getNamespaceStructureCommand);
rootCommand.AddCommand(getMethodsCommand);
rootCommand.AddCommand(checkOverridableCommand);
rootCommand.AddCommand(getConstructorDepsCommand);
rootCommand.AddCommand(findImplementationsCommand);
rootCommand.AddCommand(findInstantiationsCommand);
rootCommand.AddCommand(findCallersCommand);
rootCommand.AddCommand(getHierarchyCommand);
rootCommand.AddCommand(findByAttributeCommand);

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
