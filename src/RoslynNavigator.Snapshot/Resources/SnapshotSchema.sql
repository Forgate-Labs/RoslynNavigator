-- Snapshot Schema for RoslynNavigator
-- Defines SQLite tables for storing code analysis results
-- Tables are created idempotently with IF NOT EXISTS

-- Metadata table for snapshot information
CREATE TABLE IF NOT EXISTS snapshot_meta (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    generated_at TEXT NOT NULL,
    solution_path TEXT NOT NULL,
    schema_version INTEGER DEFAULT 1
);

-- Classes table with analysis signal columns
CREATE TABLE IF NOT EXISTS classes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    snapshot_id INTEGER NOT NULL DEFAULT 1,
    namespace TEXT NOT NULL,
    name TEXT NOT NULL,
    kind TEXT NOT NULL,
    accessibility TEXT,
    is_abstract INTEGER DEFAULT 0,
    is_sealed INTEGER DEFAULT 0,
    is_static INTEGER DEFAULT 0,
    base_types TEXT,
    implements TEXT,
    file_path TEXT NOT NULL,
    start_line INTEGER NOT NULL,
    end_line INTEGER NOT NULL,
    returns_null INTEGER DEFAULT 0,
    cognitive_complexity INTEGER DEFAULT 0,
    has_try_catch INTEGER DEFAULT 0,
    calls_external INTEGER DEFAULT 0,
    accesses_db INTEGER DEFAULT 0,
    filters_by_tenant INTEGER DEFAULT 0,
    FOREIGN KEY (snapshot_id) REFERENCES snapshot_meta(id)
);

CREATE INDEX IF NOT EXISTS idx_classes_snapshot ON classes(snapshot_id);
CREATE INDEX IF NOT EXISTS idx_classes_namespace ON classes(namespace);
CREATE INDEX IF NOT EXISTS idx_classes_name ON classes(name);
CREATE INDEX IF NOT EXISTS idx_classes_file ON classes(file_path);

-- Methods table with analysis signal columns
CREATE TABLE IF NOT EXISTS methods (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    snapshot_id INTEGER NOT NULL DEFAULT 1,
    class_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    return_type TEXT,
    accessibility TEXT,
    is_virtual INTEGER DEFAULT 0,
    is_override INTEGER DEFAULT 0,
    is_static INTEGER DEFAULT 0,
    is_abstract INTEGER DEFAULT 0,
    parameters TEXT,
    start_line INTEGER NOT NULL,
    end_line INTEGER NOT NULL,
    returns_null INTEGER DEFAULT 0,
    cognitive_complexity INTEGER DEFAULT 0,
    has_try_catch INTEGER DEFAULT 0,
    calls_external INTEGER DEFAULT 0,
    accesses_db INTEGER DEFAULT 0,
    filters_by_tenant INTEGER DEFAULT 0,
    parameter_count INTEGER DEFAULT 0,
    uses_insecure_random INTEGER DEFAULT 0,
    uses_weak_crypto INTEGER DEFAULT 0,
    catches_general_exception INTEGER DEFAULT 0,
    throws_general_exception INTEGER DEFAULT 0,
    has_sql_string_concatenation INTEGER DEFAULT 0,
    has_hardcoded_secret INTEGER DEFAULT 0,
    FOREIGN KEY (snapshot_id) REFERENCES snapshot_meta(id),
    FOREIGN KEY (class_id) REFERENCES classes(id)
);

CREATE INDEX IF NOT EXISTS idx_methods_snapshot ON methods(snapshot_id);
CREATE INDEX IF NOT EXISTS idx_methods_class ON methods(class_id);
CREATE INDEX IF NOT EXISTS idx_methods_name ON methods(name);

-- Dependencies table for tracking references between types
CREATE TABLE IF NOT EXISTS dependencies (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    snapshot_id INTEGER NOT NULL DEFAULT 1,
    from_class_id INTEGER NOT NULL,
    to_namespace TEXT NOT NULL,
    to_name TEXT NOT NULL,
    kind TEXT NOT NULL,
    FOREIGN KEY (snapshot_id) REFERENCES snapshot_meta(id),
    FOREIGN KEY (from_class_id) REFERENCES classes(id)
);

CREATE INDEX IF NOT EXISTS idx_deps_snapshot ON dependencies(snapshot_id);
CREATE INDEX IF NOT EXISTS idx_deps_from ON dependencies(from_class_id);
CREATE INDEX IF NOT EXISTS idx_deps_to ON dependencies(to_namespace, to_name);

-- Calls table for method invocation tracking
CREATE TABLE IF NOT EXISTS calls (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    snapshot_id INTEGER NOT NULL DEFAULT 1,
    caller_method_id INTEGER NOT NULL,
    target_namespace TEXT NOT NULL,
    target_class TEXT NOT NULL,
    target_method TEXT NOT NULL,
    line_number INTEGER NOT NULL,
    FOREIGN KEY (snapshot_id) REFERENCES snapshot_meta(id),
    FOREIGN KEY (caller_method_id) REFERENCES methods(id)
);

CREATE INDEX IF NOT EXISTS idx_calls_snapshot ON calls(snapshot_id);
CREATE INDEX IF NOT EXISTS idx_calls_caller ON calls(caller_method_id);
CREATE INDEX IF NOT EXISTS idx_calls_target ON calls(target_namespace, target_class, target_method);

-- Annotations table for attributes and attributes-like decorations
CREATE TABLE IF NOT EXISTS annotations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    snapshot_id INTEGER NOT NULL DEFAULT 1,
    target_type TEXT NOT NULL,
    target_id INTEGER NOT NULL,
    annotation_name TEXT NOT NULL,
    annotation_args TEXT,
    FOREIGN KEY (snapshot_id) REFERENCES snapshot_meta(id)
);

CREATE INDEX IF NOT EXISTS idx_annotations_snapshot ON annotations(snapshot_id);
CREATE INDEX IF NOT EXISTS idx_annotations_target ON annotations(target_type, target_id);
CREATE INDEX IF NOT EXISTS idx_annotations_name ON annotations(annotation_name);

-- Flags table for miscellaneous boolean markers
CREATE TABLE IF NOT EXISTS flags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    snapshot_id INTEGER NOT NULL DEFAULT 1,
    target_type TEXT NOT NULL,
    target_id INTEGER NOT NULL,
    flag_name TEXT NOT NULL,
    flag_value INTEGER DEFAULT 1,
    FOREIGN KEY (snapshot_id) REFERENCES snapshot_meta(id)
);

CREATE INDEX IF NOT EXISTS idx_flags_snapshot ON flags(snapshot_id);
CREATE INDEX IF NOT EXISTS idx_flags_target ON flags(target_type, target_id);
CREATE INDEX IF NOT EXISTS idx_flags_name ON flags(flag_name);
