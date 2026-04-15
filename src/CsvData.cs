/// <summary>
/// Holds loaded CSV content and provides zero-allocation row enumeration via foreach.
/// </summary>
public readonly struct CsvData
{
    private readonly string _content;
    private readonly char _separator;

    public CsvData(string content, char separator)
    {
        _content = content ?? string.Empty;
        _separator = separator;
    }

    public RowEnumerator GetEnumerator() => new RowEnumerator(_content.AsSpan(), _separator);

    /// <summary>
    /// Enumerates CSV rows from the loaded content.
    /// Handles CRLF, LF, and bare CR line endings.
    /// Respects quoted fields that may contain embedded newlines.
    /// </summary>
    public ref struct RowEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        private readonly char _separator;
        private CsvRow _current;

        internal RowEnumerator(ReadOnlySpan<char> content, char separator)
        {
            _remaining = content;
            _separator = separator;
            _current = default;
        }

        public CsvRow Current => _current;

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
                return false;

            int rowEnd = FindRowEnd(_remaining);
            var rowSpan = _remaining[..rowEnd];

            // Skip past line ending (CRLF, LF, or CR)
            int skip = rowEnd;
            if (skip < _remaining.Length)
            {
                if (_remaining[skip] == '\r')
                {
                    skip++;
                    if (skip < _remaining.Length && _remaining[skip] == '\n')
                        skip++;
                }
                else if (_remaining[skip] == '\n')
                {
                    skip++;
                }
            }

            _remaining = _remaining[skip..];
            _current = new CsvRow(rowSpan, _separator);
            return true;
        }

        /// <summary>
        /// Finds the end-of-row position, skipping over newlines inside quoted fields.
        /// </summary>
        private static int FindRowEnd(ReadOnlySpan<char> span)
        {
            bool inQuotes = false;
            for (int i = 0; i < span.Length; i++)
            {
                if (inQuotes)
                {
                    if (span[i] == '"')
                    {
                        if (i + 1 < span.Length && span[i + 1] == '"')
                            i++; // skip escaped double-quote
                        else
                            inQuotes = false;
                    }
                }
                else
                {
                    if (span[i] == '"')
                        inQuotes = true;
                    else if (span[i] == '\r' || span[i] == '\n')
                        return i;
                }
            }
            return span.Length;
        }
    }
}
