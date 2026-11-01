using System.Security.Cryptography;
using System.Text.Json;

namespace DocumentIntake;

public record PlannedFile(string Source, string Destination, string Category, long Bytes, string Sha256);
public record IntakeResult(string Mode, int Planned, int Copied, IReadOnlyList<PlannedFile> Files);
public record VerificationResult(string Mode, int Checked, int Matched);

public static class Intake
{
    public static Dictionary<string, string> DefaultRules() => new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "documents", [".txt"] = "documents", [".docx"] = "documents",
        [".csv"] = "data", [".xlsx"] = "data", [".json"] = "data",
        [".png"] = "images", [".jpg"] = "images", [".jpeg"] = "images"
    };

    public static Dictionary<string, string> ReadRules(string path)
    {
        // Read each property directly so repeated JSON keys cannot silently replace earlier rules.
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("The rules file must contain a JSON object.");

        var rules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var extension = property.Name;
            if (extension.Length < 2 || extension[0] != '.' ||
                !extension[1..].All(char.IsAsciiLetterOrDigit))
                throw new ArgumentException("Rule keys must be simple extensions, such as .pdf.");
            if (property.Value.ValueKind != JsonValueKind.String)
                throw new ArgumentException("Rule categories must be strings.");

            var category = property.Value.GetString()!;
            if (string.IsNullOrWhiteSpace(category) || category is "." or ".." ||
                !category.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
                throw new ArgumentException("Categories must contain only letters, digits, hyphens or underscores.");
            if (!rules.TryAdd(extension, category))
                throw new ArgumentException("Duplicate extension rule (case-insensitive).");
        }
        return rules;
    }

    private static bool IsInside(string path, string parent) =>
        path.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    private static void RejectLinks(string path)
    {
        // Inspect all existing ancestors, including an absent destination's parent.
        for (var current = new DirectoryInfo(path); current != null; current = current.Parent)
        {
            if (current.LinkTarget != null ||
                (current.Exists && current.Attributes.HasFlag(FileAttributes.ReparsePoint)))
                throw new IOException("Symbolic links and reparse points are not supported: " + current.FullName);
        }
    }

    private static IEnumerable<string> Scan(string directory, bool includeDirectories = false)
    {
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (entry.LinkTarget != null || entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Symbolic links and reparse points are not supported: " + entry.FullName);
            if (entry is DirectoryInfo child)
            {
                if (includeDirectories) yield return child.FullName;
                foreach (var nested in Scan(child.FullName, includeDirectories)) yield return nested;
            }
            else yield return entry.FullName;
        }
    }

    public static string Hash(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    public static List<PlannedFile> Plan(string source, string destination, Dictionary<string, string>? rules = null, bool allowExistingCopies = false)
    {
        source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Source folder does not exist.");
        if (IsInside(destination, source) || IsInside(source, destination))
            throw new ArgumentException("Source and destination must be separate, non-nested folders.");
        RejectLinks(source);
        RejectLinks(destination);
        if (File.Exists(destination)) throw new IOException("Destination is a file.");
        rules ??= DefaultRules();
        // Validate caller-supplied categories too; this method is also a library API.
        foreach (var category in rules.Values)
            if (string.IsNullOrWhiteSpace(category) || !category.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
                throw new ArgumentException("Invalid category.");
        var normalizedRules = new Dictionary<string, string>(rules, StringComparer.OrdinalIgnoreCase);
        var planned = new List<PlannedFile>();
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Scan(source))
        {
            var category = normalizedRules.GetValueOrDefault(Path.GetExtension(file), "other");
            var target = Path.Combine(destination, category, Path.GetRelativePath(source, file));
            RejectLinks(target);
            if (!targets.Add(target) || Directory.Exists(target) || (!allowExistingCopies && File.Exists(target)))
                throw new IOException("Destination conflict: " + target);
            // Fail before copying if a regular file blocks a required directory.
            for (var parent = Path.GetDirectoryName(target); parent != null && IsInside(parent, destination); parent = Path.GetDirectoryName(parent))
                if (File.Exists(parent)) throw new IOException("A file blocks the destination directory: " + parent);
            planned.Add(new PlannedFile(file, target, category, new FileInfo(file).Length, Hash(file)));
        }
        return planned;
    }

    public static VerificationResult Verify(string source, string destination, Dictionary<string, string>? rules = null, bool exact = false)
    {
        // Compare copies with the current source files. No files are created or changed.
        destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
        var plan = Plan(source, destination, rules, allowExistingCopies: true);
        foreach (var item in plan)
        {
            RejectLinks(item.Source);
            RejectLinks(item.Destination);
            if (!File.Exists(item.Destination))
                throw new IOException("Missing copied file: " + item.Destination);
            if (new FileInfo(item.Destination).Length != item.Bytes || Hash(item.Destination) != item.Sha256)
                throw new IOException("Copied file differs from source: " + item.Destination);
        }
        if (exact)
        {
            // A normal verify checks expected copies. Exact mode also rejects stale or unrelated files.
            if (!Directory.Exists(destination))
                throw new DirectoryNotFoundException("Destination folder does not exist.");
            var expectedFiles = plan.Select(item => item.Destination).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in plan)
            {
                for (var parent = Path.GetDirectoryName(item.Destination);
                     parent != null && !parent.Equals(destination, StringComparison.OrdinalIgnoreCase);
                     parent = Path.GetDirectoryName(parent))
                    expectedDirectories.Add(parent);
            }
            foreach (var entry in Scan(destination, includeDirectories: true))
                if (!expectedFiles.Contains(entry) && !expectedDirectories.Contains(entry))
                    throw new IOException("Unexpected destination entry: " + entry);
        }
        return new VerificationResult(exact ? "verify-exact" : "verify", plan.Count, plan.Count);
    }

    public static IntakeResult Run(string source, string destination, bool apply, Dictionary<string, string>? rules = null)
    {
        var plan = Plan(source, destination, rules);
        if (!apply) return new IntakeResult("preview", plan.Count, 0, plan);
        var copied = 0;
        foreach (var item in plan)
        {
            RejectLinks(item.Source);
            RejectLinks(item.Destination);
            if (Hash(item.Source) != item.Sha256) throw new IOException("Source changed since planning: " + item.Source);
            var parent = Path.GetDirectoryName(item.Destination)!;
            Directory.CreateDirectory(parent);
            var temporary = Path.Combine(parent, ".intake-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var input = File.OpenRead(item.Source))
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    input.CopyTo(output);
                if (Hash(temporary) != item.Sha256) throw new IOException("Source changed while copying: " + item.Source);
                // Rename only after the complete copy matches. Never replace an existing file.
                File.Move(temporary, item.Destination, overwrite: false);
                copied++;
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
        return new IntakeResult("apply", plan.Count, copied, plan);
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            Console.WriteLine("DocumentIntake <preview|apply|verify|verify-exact> <source> <destination> [rules.json]");
            Console.WriteLine("Preview makes no changes. Apply copies files and never deletes originals or overwrites targets.");
            Console.WriteLine("Verify-exact also rejects unexpected destination files. Both verify modes are read-only.");
            return 0;
        }
        if (args.Length is < 3 or > 4 || args[0] is not ("preview" or "apply" or "verify" or "verify-exact"))
        {
            Console.Error.WriteLine("Usage: DocumentIntake <preview|apply|verify|verify-exact> <source> <destination> [rules.json]");
            return 2;
        }
        try
        {
            var rules = args.Length == 4 ? Intake.ReadRules(args[3]) : Intake.DefaultRules();
            object result = args[0] is "verify" or "verify-exact"
                ? Intake.Verify(args[1], args[2], rules, exact: args[0] == "verify-exact")
                : Intake.Run(args[1], args[2], args[0] == "apply", rules);
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or JsonException or NotSupportedException)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine("No source files were deleted. If apply stopped partway, earlier verified copies may remain.");
            return 1;
        }
    }
}
