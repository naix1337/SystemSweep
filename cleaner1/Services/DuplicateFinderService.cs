using System.IO;
using System.Security.Cryptography;

namespace ModernFileCleaner.Services;

public class DuplicateGroup
{
    public string Hash { get; set; } = "";
    public long SizeBytes { get; set; }
    public List<string> Files { get; set; } = new();
    public bool IsSelected { get; set; } = true;
    public string SizeFormatted => FormatBytes(SizeBytes);
    private static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes} B" :
        bytes < 1024 * 1024 ? $"{bytes / 1024.0:F1} KB" :
        bytes < 1024 * 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):F1} MB" :
        $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
}

public class DuplicateFinderService
{
    public async Task<List<DuplicateGroup>> FindDuplicatesAsync(string rootPath, IProgress<string>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var groups = new Dictionary<string, DuplicateGroup>();
            var allFiles = new List<string>();

            try
            {
                foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    allFiles.Add(file);
                }
            }
            catch (UnauthorizedAccessException) { }

            progress?.Report($"Scanning {allFiles.Count} files...");

            // Phase 1: Group by (name + size)
            var candidates = allFiles
                .GroupBy(f => $"{Path.GetFileName(f)}|{new FileInfo(f).Length}")
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            progress?.Report($"Hashing {candidates.Count} candidates...");

            // Phase 2: Hash candidates
            foreach (var file in candidates)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var stream = File.OpenRead(file);
                    var hash = BitConverter.ToString(SHA256.HashData(stream)).Replace("-", "");

                    if (!groups.ContainsKey(hash))
                        groups[hash] = new DuplicateGroup { Hash = hash, SizeBytes = new FileInfo(file).Length };
                    groups[hash].Files.Add(file);
                }
                catch { }
            }

            return groups.Values.Where(g => g.Files.Count > 1).ToList();
        }, ct);
    }
}
