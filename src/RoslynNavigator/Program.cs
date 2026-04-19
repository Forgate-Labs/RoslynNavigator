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
        await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "class");
        Console.WriteLine("DONE");
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
        await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "interface");
        Console.WriteLine("DONE");
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
        await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "record");
        Console.WriteLine("DONE");
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
        await DotnetScaffoldCommand.ExecuteAsync(path, ns, typeName, "enum");
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_scaffold_enum_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, scaffoldEnumPathArg, scaffoldEnumNamespaceArg, scaffoldEnumNameArg);

// dotnet add using
var addUsingPathArg = new Argument<string>("path", "Path to the C# file");
var addUsingNamespaceArg = new Argument<string>("namespace", "Namespace to add as using directive");
var addUsingSubcommand = new Command("using", "Add a using directive (no-op if already present)");
addUsingSubcommand.AddArgument(addUsingPathArg);
addUsingSubcommand.AddArgument(addUsingNamespaceArg);
addUsingSubcommand.SetHandler(async (string path, string ns) =>
{
    try
    {
        await DotnetAddCommand.ExecuteUsingAsync(path, ns);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_add_using_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, addUsingPathArg, addUsingNamespaceArg);

// dotnet add field
var addFieldPathArg = new Argument<string>("path", "Path to the C# file");
var addFieldClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var addFieldAccessArg = new Argument<string>("access", "Access modifier (e.g., private, public)");
var addFieldTypeArg = new Argument<string>("type", "Field type (e.g., int, string, ILogger)");
var addFieldNameArg = new Argument<string>("name", "Field name (underscore prefix added automatically)");
var addFieldSubcommand = new Command("field", "Add a field to a class/record/struct");
addFieldSubcommand.AddArgument(addFieldPathArg);
addFieldSubcommand.AddArgument(addFieldClassArg);
addFieldSubcommand.AddArgument(addFieldAccessArg);
addFieldSubcommand.AddArgument(addFieldTypeArg);
addFieldSubcommand.AddArgument(addFieldNameArg);
addFieldSubcommand.SetHandler(async (string path, string className, string access, string type, string name) =>
{
    try
    {
        var content = $"{access} {type} _{name};";
        await DotnetAddCommand.ExecuteMemberAsync(path, className, "field", content);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_add_field_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, addFieldPathArg, addFieldClassArg, addFieldAccessArg, addFieldTypeArg, addFieldNameArg);

// dotnet add property
var addPropertyPathArg = new Argument<string>("path", "Path to the C# file");
var addPropertyClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var addPropertyAccessArg = new Argument<string>("access", "Access modifier (e.g., private, public)");
var addPropertyTypeArg = new Argument<string>("type", "Property type (e.g., int, string)");
var addPropertyNameArg = new Argument<string>("name", "Property name (caller provides casing)");
var addPropertySubcommand = new Command("property", "Add an auto-property to a class/record/struct");
addPropertySubcommand.AddArgument(addPropertyPathArg);
addPropertySubcommand.AddArgument(addPropertyClassArg);
addPropertySubcommand.AddArgument(addPropertyAccessArg);
addPropertySubcommand.AddArgument(addPropertyTypeArg);
addPropertySubcommand.AddArgument(addPropertyNameArg);
addPropertySubcommand.SetHandler(async (string path, string className, string access, string type, string name) =>
{
    try
    {
        var content = $"{access} {type} {name} {{ get; set; }}";
        await DotnetAddCommand.ExecuteMemberAsync(path, className, "property", content);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_add_property_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, addPropertyPathArg, addPropertyClassArg, addPropertyAccessArg, addPropertyTypeArg, addPropertyNameArg);

// dotnet add constructor
var addCtorPathArg = new Argument<string>("path", "Path to the C# file");
var addCtorClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var addCtorContentArg = new Argument<string>("content", "Full constructor source (signature + body)");
var addCtorSubcommand = new Command("constructor", "Add a constructor to a class/record/struct");
addCtorSubcommand.AddArgument(addCtorPathArg);
addCtorSubcommand.AddArgument(addCtorClassArg);
addCtorSubcommand.AddArgument(addCtorContentArg);
addCtorSubcommand.SetHandler(async (string path, string className, string content) =>
{
    try
    {
        await DotnetAddCommand.ExecuteMemberAsync(path, className, "constructor", content);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_add_constructor_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, addCtorPathArg, addCtorClassArg, addCtorContentArg);

// dotnet add method
var addMethodPathArg = new Argument<string>("path", "Path to the C# file");
var addMethodClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var addMethodContentArg = new Argument<string>("content", "Full method source (signature + body)");
var addMethodSubcommand = new Command("method", "Add a method to a class/record/struct");
addMethodSubcommand.AddArgument(addMethodPathArg);
addMethodSubcommand.AddArgument(addMethodClassArg);
addMethodSubcommand.AddArgument(addMethodContentArg);
addMethodSubcommand.SetHandler(async (string path, string className, string content) =>
{
    try
    {
        await DotnetAddCommand.ExecuteMemberAsync(path, className, "method", content);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_add_method_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, addMethodPathArg, addMethodClassArg, addMethodContentArg);

var dotnetAddCommand = new Command("add", "Add members to existing C# types");
dotnetAddCommand.AddCommand(addUsingSubcommand);
dotnetAddCommand.AddCommand(addFieldSubcommand);
dotnetAddCommand.AddCommand(addPropertySubcommand);
dotnetAddCommand.AddCommand(addCtorSubcommand);
dotnetAddCommand.AddCommand(addMethodSubcommand);

dotnetScaffoldCommand.AddCommand(scaffoldClassSubcommand);
dotnetScaffoldCommand.AddCommand(scaffoldInterfaceSubcommand);
dotnetScaffoldCommand.AddCommand(scaffoldRecordSubcommand);
dotnetScaffoldCommand.AddCommand(scaffoldEnumSubcommand);
dotnetCommand.AddCommand(dotnetScaffoldCommand);
dotnetCommand.AddCommand(dotnetAddCommand);

// dotnet update property
var updPropertyPathArg = new Argument<string>("path", "Path to the C# file");
var updPropertyClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var updPropertyNameArg = new Argument<string>("propertyName", "Name of the property to replace");
var updPropertyContentArg = new Argument<string>("content", "Full replacement property source");
var updPropertySubcommand = new Command("property", "Replace a property in a class/record/struct");
updPropertySubcommand.AddArgument(updPropertyPathArg);
updPropertySubcommand.AddArgument(updPropertyClassArg);
updPropertySubcommand.AddArgument(updPropertyNameArg);
updPropertySubcommand.AddArgument(updPropertyContentArg);
updPropertySubcommand.SetHandler(async (string path, string className, string name, string content) =>
{
    try
    {
        await DotnetUpdateCommand.ExecuteAsync(path, className, "property", name, content);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_update_property_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, updPropertyPathArg, updPropertyClassArg, updPropertyNameArg, updPropertyContentArg);

// dotnet update field
var updFieldPathArg = new Argument<string>("path", "Path to the C# file");
var updFieldClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var updFieldNameArg = new Argument<string>("fieldName", "Name of the field to replace (with or without leading underscore)");
var updFieldContentArg = new Argument<string>("content", "Full replacement field source");
var updFieldSubcommand = new Command("field", "Replace a field in a class/record/struct");
updFieldSubcommand.AddArgument(updFieldPathArg);
updFieldSubcommand.AddArgument(updFieldClassArg);
updFieldSubcommand.AddArgument(updFieldNameArg);
updFieldSubcommand.AddArgument(updFieldContentArg);
updFieldSubcommand.SetHandler(async (string path, string className, string name, string content) =>
{
    try
    {
        await DotnetUpdateCommand.ExecuteAsync(path, className, "field", name, content);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_update_field_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, updFieldPathArg, updFieldClassArg, updFieldNameArg, updFieldContentArg);

// dotnet remove method
var remMethodPathArg = new Argument<string>("path", "Path to the C# file");
var remMethodClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var remMethodNameArg = new Argument<string>("methodName", "Name of the method to remove");
var remMethodSubcommand = new Command("method", "Remove a method from a class/record/struct");
remMethodSubcommand.AddArgument(remMethodPathArg);
remMethodSubcommand.AddArgument(remMethodClassArg);
remMethodSubcommand.AddArgument(remMethodNameArg);
remMethodSubcommand.SetHandler(async (string path, string className, string name) =>
{
    try
    {
        await DotnetRemoveCommand.ExecuteAsync(path, className, "method", name);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_remove_method_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, remMethodPathArg, remMethodClassArg, remMethodNameArg);

// dotnet remove property
var remPropertyPathArg = new Argument<string>("path", "Path to the C# file");
var remPropertyClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var remPropertyNameArg = new Argument<string>("propertyName", "Name of the property to remove");
var remPropertySubcommand = new Command("property", "Remove a property from a class/record/struct");
remPropertySubcommand.AddArgument(remPropertyPathArg);
remPropertySubcommand.AddArgument(remPropertyClassArg);
remPropertySubcommand.AddArgument(remPropertyNameArg);
remPropertySubcommand.SetHandler(async (string path, string className, string name) =>
{
    try
    {
        await DotnetRemoveCommand.ExecuteAsync(path, className, "property", name);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_remove_property_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, remPropertyPathArg, remPropertyClassArg, remPropertyNameArg);

// dotnet remove field
var remFieldPathArg = new Argument<string>("path", "Path to the C# file");
var remFieldClassArg = new Argument<string>("className", "Name of the target class/record/struct");
var remFieldNameArg = new Argument<string>("fieldName", "Name of the field to remove (with or without leading underscore)");
var remFieldSubcommand = new Command("field", "Remove a field from a class/record/struct");
remFieldSubcommand.AddArgument(remFieldPathArg);
remFieldSubcommand.AddArgument(remFieldClassArg);
remFieldSubcommand.AddArgument(remFieldNameArg);
remFieldSubcommand.SetHandler(async (string path, string className, string name) =>
{
    try
    {
        await DotnetRemoveCommand.ExecuteAsync(path, className, "field", name);
        Console.WriteLine("DONE");
    }
    catch (Exception ex)
    {
        OutputError("dotnet_remove_field_error", ex.Message);
        Environment.ExitCode = 1;
    }
}, remFieldPathArg, remFieldClassArg, remFieldNameArg);

var dotnetUpdateCommand = new Command("update", "Replace members in existing C# types");
dotnetUpdateCommand.AddCommand(updPropertySubcommand);
dotnetUpdateCommand.AddCommand(updFieldSubcommand);

var dotnetRemoveCommand = new Command("remove", "Remove members from existing C# types");
dotnetRemoveCommand.AddCommand(remMethodSubcommand);
dotnetRemoveCommand.AddCommand(remPropertySubcommand);
dotnetRemoveCommand.AddCommand(remFieldSubcommand);

dotnetCommand.AddCommand(dotnetUpdateCommand);
dotnetCommand.AddCommand(dotnetRemoveCommand);

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
