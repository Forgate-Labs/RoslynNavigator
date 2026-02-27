using System.CommandLine;
using System.Reflection;
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

var fullVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
    ?? "unknown";
var version = fullVersion.Split('+')[0];

var rootCommand = new RootCommand($"Roslyn Navigator v{version} - Semantic C# code navigation tool");

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

// find-step-definition command
var findStepDefinitionCommand = new Command("find-step-definition", "Find Reqnroll/SpecFlow step definitions by pattern");
var stepPatternOption = new Option<string>("--pattern", "Text pattern to search for in step definitions") { IsRequired = true };
findStepDefinitionCommand.AddOption(solutionOption);
findStepDefinitionCommand.AddOption(stepPatternOption);
findStepDefinitionCommand.SetHandler(async (string solution, string pattern) =>
{
    try
    {
        var result = await FindStepDefinitionCommand.ExecuteAsync(solution, pattern);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_step_definition_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, stepPatternOption);

// find-interface-consumers command
var findInterfaceConsumersCommand = new Command("find-interface-consumers", "Find interface implementations and injection points");
findInterfaceConsumersCommand.AddOption(solutionOption);
findInterfaceConsumersCommand.AddOption(interfaceOption);
findInterfaceConsumersCommand.SetHandler(async (string solution, string interfaceName) =>
{
    try
    {
        var result = await FindInterfaceConsumersCommand.ExecuteAsync(solution, interfaceName);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("find_interface_consumers_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, solutionOption, interfaceOption);

// list-feature-scenarios command
var listFeatureScenariosCommand = new Command("list-feature-scenarios", "List scenarios from Gherkin .feature files");
var pathOption = new Option<string>("--path", "Directory containing .feature files") { IsRequired = true };
listFeatureScenariosCommand.AddOption(pathOption);
listFeatureScenariosCommand.SetHandler(async (string path) =>
{
    try
    {
        var result = await ListFeatureScenariosCommand.ExecuteAsync(path);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("list_feature_scenarios_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, pathOption);

// ── dotnet command group ────────────────────────────────────────────────────
var dotnetCommand = new Command("dotnet", "dotnet-specific operations (scaffold, etc.)");
var dotnetScaffoldCommand = new Command("scaffold", "Scaffold new C# type files");

// dotnet scaffold class
var scaffoldClassPathArg = new Argument<string>("path", "Output file path for the new class");
var scaffoldClassNamespaceArg = new Argument<string>("namespace", "Namespace for the new class");
var scaffoldClassNameArg = new Argument<string>("className", "Name of the new class");
var scaffoldClassSubcommand = new Command("class", "Scaffold a new C# class file");
scaffoldClassSubcommand.AddArgument(scaffoldClassPathArg);
scaffoldClassSubcommand.AddArgument(scaffoldClassNamespaceArg);
scaffoldClassSubcommand.AddArgument(scaffoldClassNameArg);
scaffoldClassSubcommand.SetHandler(async (string path, string ns, string typeName) =>
{
    try
    {
        var result = await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "class");
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("dotnet_scaffold_class_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, scaffoldClassPathArg, scaffoldClassNamespaceArg, scaffoldClassNameArg);

// dotnet scaffold interface
var scaffoldInterfacePathArg = new Argument<string>("path", "Output file path for the new interface");
var scaffoldInterfaceNamespaceArg = new Argument<string>("namespace", "Namespace for the new interface");
var scaffoldInterfaceNameArg = new Argument<string>("interfaceName", "Name of the new interface");
var scaffoldInterfaceSubcommand = new Command("interface", "Scaffold a new C# interface file");
scaffoldInterfaceSubcommand.AddArgument(scaffoldInterfacePathArg);
scaffoldInterfaceSubcommand.AddArgument(scaffoldInterfaceNamespaceArg);
scaffoldInterfaceSubcommand.AddArgument(scaffoldInterfaceNameArg);
scaffoldInterfaceSubcommand.SetHandler(async (string path, string ns, string typeName) =>
{
    try
    {
        var result = await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "interface");
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("dotnet_scaffold_interface_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, scaffoldInterfacePathArg, scaffoldInterfaceNamespaceArg, scaffoldInterfaceNameArg);

// dotnet scaffold record
var scaffoldRecordPathArg = new Argument<string>("path", "Output file path for the new record");
var scaffoldRecordNamespaceArg = new Argument<string>("namespace", "Namespace for the new record");
var scaffoldRecordNameArg = new Argument<string>("recordName", "Name of the new record");
var scaffoldRecordSubcommand = new Command("record", "Scaffold a new C# record file");
scaffoldRecordSubcommand.AddArgument(scaffoldRecordPathArg);
scaffoldRecordSubcommand.AddArgument(scaffoldRecordNamespaceArg);
scaffoldRecordSubcommand.AddArgument(scaffoldRecordNameArg);
scaffoldRecordSubcommand.SetHandler(async (string path, string ns, string typeName) =>
{
    try
    {
        var result = await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "record");
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("dotnet_scaffold_record_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, scaffoldRecordPathArg, scaffoldRecordNamespaceArg, scaffoldRecordNameArg);

// dotnet scaffold enum
var scaffoldEnumPathArg = new Argument<string>("path", "Output file path for the new enum");
var scaffoldEnumNamespaceArg = new Argument<string>("namespace", "Namespace for the new enum");
var scaffoldEnumNameArg = new Argument<string>("enumName", "Name of the new enum");
var scaffoldEnumSubcommand = new Command("enum", "Scaffold a new C# enum file");
scaffoldEnumSubcommand.AddArgument(scaffoldEnumPathArg);
scaffoldEnumSubcommand.AddArgument(scaffoldEnumNamespaceArg);
scaffoldEnumSubcommand.AddArgument(scaffoldEnumNameArg);
scaffoldEnumSubcommand.SetHandler(async (string path, string ns, string typeName) =>
{
    try
    {
        var result = await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "enum");
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("dotnet_scaffold_enum_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, scaffoldEnumPathArg, scaffoldEnumNamespaceArg, scaffoldEnumNameArg);

dotnetScaffoldCommand.AddCommand(scaffoldClassSubcommand);
dotnetScaffoldCommand.AddCommand(scaffoldInterfaceSubcommand);
dotnetScaffoldCommand.AddCommand(scaffoldRecordSubcommand);
dotnetScaffoldCommand.AddCommand(scaffoldEnumSubcommand);
dotnetCommand.AddCommand(dotnetScaffoldCommand);

// ── file command group ──────────────────────────────────────────────────────
var fileCommand = new Command("file", "File read and staged-edit operations");

// file read
var fileReadPathArg = new Argument<string>("path", "Path to the file to read");
var fileReadLinesOption = new Option<string?>("--lines", "Line range to read (e.g., 10-20)");
var fileReadSubcommand = new Command("read", "Read a file with line numbers");
fileReadSubcommand.AddArgument(fileReadPathArg);
fileReadSubcommand.AddOption(fileReadLinesOption);
fileReadSubcommand.SetHandler(async (string path, string? lines) =>
{
    try
    {
        var result = await FileReadCommand.ExecuteAsync(path, lines);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_read_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, fileReadPathArg, fileReadLinesOption);

// file grep
var fileGrepPatternArg = new Argument<string>("pattern", "Regex pattern to search for");
var fileGrepPathArg = new Argument<string?>("path", "File or directory to search (default: current directory)") { Arity = ArgumentArity.ZeroOrOne };
var fileGrepExtOption = new Option<string?>("--ext", "File extension filter (e.g., .cs)");
var fileGrepMaxLinesOption = new Option<int>("--max-lines", () => 100, "Maximum number of matching lines to return");
var fileGrepSubcommand = new Command("grep", "Search for a regex pattern in files");
fileGrepSubcommand.AddArgument(fileGrepPatternArg);
fileGrepSubcommand.AddArgument(fileGrepPathArg);
fileGrepSubcommand.AddOption(fileGrepExtOption);
fileGrepSubcommand.AddOption(fileGrepMaxLinesOption);
fileGrepSubcommand.SetHandler(async (string pattern, string? path, string? ext, int maxLines) =>
{
    try
    {
        var result = await FileGrepCommand.ExecuteAsync(pattern, path, ext, maxLines);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_grep_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, fileGrepPatternArg, fileGrepPathArg, fileGrepExtOption, fileGrepMaxLinesOption);

// file plan subgroup
var filePlanCommand = new Command("plan", "Stage file operations for atomic commit");

// file plan edit
var planEditPathArg = new Argument<string>("path", "File path to edit");
var planEditLineArg = new Argument<int>("line", "1-based line number to edit");
var planEditOldArg = new Argument<string>("old", "Expected current content of the line");
var planEditNewArg = new Argument<string>("new", "Replacement content for the line");
var planEditSubcommand = new Command("edit", "Stage a line edit (validates old string before accepting)");
planEditSubcommand.AddArgument(planEditPathArg);
planEditSubcommand.AddArgument(planEditLineArg);
planEditSubcommand.AddArgument(planEditOldArg);
planEditSubcommand.AddArgument(planEditNewArg);
planEditSubcommand.SetHandler(async (string path, int line, string old, string newContent) =>
{
    try
    {
        var result = await FilePlanEditCommand.ExecuteAsync(path, line, old, newContent);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_plan_edit_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, planEditPathArg, planEditLineArg, planEditOldArg, planEditNewArg);

// file plan write
var planWritePathArg = new Argument<string>("path", "File path to write");
var planWriteContentArg = new Argument<string>("content", "Full file content");
var planWriteSubcommand = new Command("write", "Stage a full file write (creates or overwrites)");
planWriteSubcommand.AddArgument(planWritePathArg);
planWriteSubcommand.AddArgument(planWriteContentArg);
planWriteSubcommand.SetHandler(async (string path, string content) =>
{
    try
    {
        var result = await FilePlanWriteCommand.ExecuteAsync(path, content);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_plan_write_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, planWritePathArg, planWriteContentArg);

// file plan append
var planAppendPathArg = new Argument<string>("path", "File path to append to");
var planAppendContentArg = new Argument<string>("content", "Content to append");
var planAppendSubcommand = new Command("append", "Stage content to append to a file");
planAppendSubcommand.AddArgument(planAppendPathArg);
planAppendSubcommand.AddArgument(planAppendContentArg);
planAppendSubcommand.SetHandler(async (string path, string content) =>
{
    try
    {
        var result = await FilePlanAppendCommand.ExecuteAsync(path, content);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_plan_append_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, planAppendPathArg, planAppendContentArg);

// file plan delete
var planDeletePathArg = new Argument<string>("path", "File path");
var planDeleteLineArg = new Argument<int>("line", "1-based line number to delete");
var planDeleteOldArg = new Argument<string>("old", "Expected current content of the line");
var planDeleteSubcommand = new Command("delete", "Stage a line deletion (validates old string before accepting)");
planDeleteSubcommand.AddArgument(planDeletePathArg);
planDeleteSubcommand.AddArgument(planDeleteLineArg);
planDeleteSubcommand.AddArgument(planDeleteOldArg);
planDeleteSubcommand.SetHandler(async (string path, int line, string old) =>
{
    try
    {
        var result = await FilePlanDeleteCommand.ExecuteAsync(path, line, old);
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_plan_delete_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, planDeletePathArg, planDeleteLineArg, planDeleteOldArg);

filePlanCommand.AddCommand(planEditSubcommand);
filePlanCommand.AddCommand(planWriteSubcommand);
filePlanCommand.AddCommand(planAppendSubcommand);
filePlanCommand.AddCommand(planDeleteSubcommand);

// file status
var fileStatusJsonOption = new Option<bool>("--json", "Output machine-readable JSON");
var fileStatusSubcommand = new Command("status", "Preview all staged changes as a unified diff");
fileStatusSubcommand.AddOption(fileStatusJsonOption);
fileStatusSubcommand.SetHandler(async (bool json) =>
{
    try
    {
        var result = await FileStatusCommand.ExecuteAsync();
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
        else
            Console.WriteLine(result.UnifiedDiff);
    }
    catch (Exception ex)
    {
        OutputError("file_status_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, fileStatusJsonOption);

// file commit
var fileCommitJsonOption = new Option<bool>("--json", "Output machine-readable JSON");
var fileCommitSubcommand = new Command("commit", "Apply all staged changes atomically and return a unified diff");
fileCommitSubcommand.AddOption(fileCommitJsonOption);
fileCommitSubcommand.SetHandler(async (bool json) =>
{
    try
    {
        var result = await FileCommitCommand.ExecuteAsync();
        if (json)
            Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
        else
            Console.WriteLine(result.UnifiedDiff);
    }
    catch (Exception ex)
    {
        OutputError("file_commit_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, fileCommitJsonOption);

// file rollback
var fileRollbackSubcommand = new Command("rollback", "Restore all files from the last commit backup");
fileRollbackSubcommand.SetHandler(async () =>
{
    try
    {
        var result = await FileRollbackCommand.ExecuteAsync();
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_rollback_error", ex.Message);
        Environment.ExitCode = 1;
    }
});

// file clear
var fileClearSubcommand = new Command("clear", "Discard all staged operations without touching any file");
fileClearSubcommand.SetHandler(async () =>
{
    try
    {
        var result = await FileClearCommand.ExecuteAsync();
        Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    }
    catch (Exception ex)
    {
        OutputError("file_clear_error", ex.Message);
        Environment.ExitCode = 1;
    }
});

fileCommand.AddCommand(filePlanCommand);
fileCommand.AddCommand(fileStatusSubcommand);
fileCommand.AddCommand(fileCommitSubcommand);
fileCommand.AddCommand(fileRollbackSubcommand);
fileCommand.AddCommand(fileClearSubcommand);
fileCommand.AddCommand(fileReadSubcommand);
fileCommand.AddCommand(fileGrepSubcommand);

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
rootCommand.AddCommand(findStepDefinitionCommand);
rootCommand.AddCommand(findInterfaceConsumersCommand);
rootCommand.AddCommand(listFeatureScenariosCommand);
rootCommand.AddCommand(dotnetCommand);
rootCommand.AddCommand(fileCommand);

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
