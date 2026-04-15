/// <summary>
/// High-performance CSV/TSV loader with zero-allocation parsing via ref struct enumerators.
/// </summary>
public static class FastCsvLoader
{
    /// <summary>
    /// Loads a CSV/TSV file asynchronously and returns a <see cref="CsvData"/> for enumeration.
    /// </summary>
    /// <param name="filePath">Path to the CSV/TSV file.</param>
    /// <param name="separator">Field separator character (default: comma).</param>
    /// <returns>A <see cref="CsvData"/> that can be enumerated row-by-row, then cell-by-cell.</returns>
    public static async Task<CsvData> LoadAsync(string filePath, char separator = ',')
    {
        var content = await File.ReadAllTextAsync(filePath);
        return new CsvData(content, separator);
    }
}
