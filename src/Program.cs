using System.Diagnostics;

var tsvPath = args.Length > 0 ? args[0] : Path.Combine("..", "tests", "test_100k.tsv");

if (!File.Exists(tsvPath))
{
    Console.Error.WriteLine($"File not found: {tsvPath}");
    return 1;
}

var fileInfo = new FileInfo(tsvPath);
Console.WriteLine($"File: {tsvPath} ({fileInfo.Length / 1024.0 / 1024.0:F2} MB)");
Console.WriteLine(new string('-', 60));

// Warm-up: read file once to populate OS file cache
_ = File.ReadAllBytes(tsvPath);

const int iterations = 5;

// --- Method 1: StreamReader line-by-line ---
var sw = Stopwatch.StartNew();
for (int iter = 0; iter < iterations; iter++)
{
    int rowCount = 0;
    using var reader = new StreamReader(tsvPath);
    string? header = reader.ReadLine(); // skip header
    string[]? columns = header?.Split('\t');

    while (reader.ReadLine() is { } line)
    {
        string[] fields = line.Split('\t');
        rowCount++;
    }
}
sw.Stop();
var avgMs1 = sw.Elapsed.TotalMilliseconds / iterations;
Console.WriteLine($"StreamReader + Split     : {avgMs1,8:F1} ms avg ({iterations} runs)");

// --- Method 2: File.ReadAllLines ---
sw.Restart();
for (int iter = 0; iter < iterations; iter++)
{
    string[] lines = File.ReadAllLines(tsvPath);
    int rowCount = 0;
    for (int i = 1; i < lines.Length; i++)
    {
        string[] fields = lines[i].Split('\t');
        rowCount++;
    }
}
sw.Stop();
var avgMs2 = sw.Elapsed.TotalMilliseconds / iterations;
Console.WriteLine($"ReadAllLines + Split     : {avgMs2,8:F1} ms avg ({iterations} runs)");

// --- Method 3: ReadAllText + Span-based parsing ---
sw.Restart();
for (int iter = 0; iter < iterations; iter++)
{
    string text = File.ReadAllText(tsvPath);
    ReadOnlySpan<char> span = text.AsSpan();
    int rowCount = 0;
    bool isHeader = true;

    while (!span.IsEmpty)
    {
        int newlineIdx = span.IndexOf('\n');
        ReadOnlySpan<char> line = newlineIdx >= 0 ? span[..newlineIdx] : span;
        span = newlineIdx >= 0 ? span[(newlineIdx + 1)..] : default;

        if (line.EndsWith("\r"))
            line = line[..^1];

        if (isHeader) { isHeader = false; continue; }
        if (line.IsEmpty) continue;

        // Parse fields using Span to avoid allocations
        int fieldCount = 0;
        ReadOnlySpan<char> remaining = line;
        while (!remaining.IsEmpty)
        {
            int tabIdx = remaining.IndexOf('\t');
            ReadOnlySpan<char> field = tabIdx >= 0 ? remaining[..tabIdx] : remaining;
            remaining = tabIdx >= 0 ? remaining[(tabIdx + 1)..] : default;
            fieldCount++;
        }
        rowCount++;
    }
}
sw.Stop();
var avgMs3 = sw.Elapsed.TotalMilliseconds / iterations;
Console.WriteLine($"ReadAllText + Span parse : {avgMs3,8:F1} ms avg ({iterations} runs)");

// --- Method 4: Buffered StreamReader with large buffer ---
sw.Restart();
for (int iter = 0; iter < iterations; iter++)
{
    int rowCount = 0;
    using var fs = new FileStream(tsvPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
    using var reader = new StreamReader(fs, bufferSize: 64 * 1024);
    reader.ReadLine(); // skip header

    while (reader.ReadLine() is { } line)
    {
        string[] fields = line.Split('\t');
        rowCount++;
    }
}
sw.Stop();
var avgMs4 = sw.Elapsed.TotalMilliseconds / iterations;
Console.WriteLine($"Buffered StreamReader    : {avgMs4,8:F1} ms avg ({iterations} runs)");

Console.WriteLine(new string('-', 60));
Console.WriteLine("Done.");
return 0;
