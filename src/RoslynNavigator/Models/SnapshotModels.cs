namespace RoslynNavigator.Services;

/// <summary>
/// Represents a row in the snapshot_classes table.
/// </summary>
public class SnapshotClassRow
{
    public int Id { get; set; }
    public int SnapshotId { get; set; } = 1;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Accessibility { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsStatic { get; set; }
    public string? BaseTypes { get; set; }
    public string? Implements { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    
    // Analysis signals
    public bool ReturnsNull { get; set; }
    public int CognitiveComplexity { get; set; }
    public bool HasTryCatch { get; set; }
    public bool CallsExternal { get; set; }
    public bool AccessesDb { get; set; }
    public bool FiltersByTenant { get; set; }
}

/// <summary>
/// Represents a row in the snapshot_methods table.
/// </summary>
public class SnapshotMethodRow
{
    public int Id { get; set; }
    public int SnapshotId { get; set; } = 1;
    public int ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ReturnType { get; set; }
    public string? Accessibility { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public string? Parameters { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    
    // Analysis signals
    public bool ReturnsNull { get; set; }
    public int CognitiveComplexity { get; set; }
    public bool HasTryCatch { get; set; }
    public bool CallsExternal { get; set; }
    public bool AccessesDb { get; set; }
    public bool FiltersByTenant { get; set; }
}

/// <summary>
/// Represents a row in the snapshot_dependencies table.
/// </summary>
public class SnapshotDependencyRow
{
    public int Id { get; set; }
    public int SnapshotId { get; set; } = 1;
    public int FromClassId { get; set; }
    public string ToNamespace { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

/// <summary>
/// Represents a row in the snapshot_calls table.
/// </summary>
public class SnapshotCallRow
{
    public int Id { get; set; }
    public int SnapshotId { get; set; } = 1;
    public int CallerMethodId { get; set; }
    public string TargetNamespace { get; set; } = string.Empty;
    public string TargetClass { get; set; } = string.Empty;
    public string TargetMethod { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}

/// <summary>
/// Represents a row in the snapshot_annotations table.
/// </summary>
public class SnapshotAnnotationRow
{
    public int Id { get; set; }
    public int SnapshotId { get; set; } = 1;
    public string TargetType { get; set; } = string.Empty; // "class" or "method"
    public int TargetId { get; set; }
    public string AnnotationName { get; set; } = string.Empty;
    public string? AnnotationArgs { get; set; }
}

/// <summary>
/// Represents a row in the snapshot_flags table.
/// </summary>
public class SnapshotFlagRow
{
    public int Id { get; set; }
    public int SnapshotId { get; set; } = 1;
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string FlagName { get; set; } = string.Empty;
    public int FlagValue { get; set; } = 1;
}
