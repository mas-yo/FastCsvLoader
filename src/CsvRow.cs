/// <summary>
/// Represents a single CSV row. Provides zero-allocation cell enumeration via foreach.
/// </summary>
public readonly ref struct CsvRow
{
    private readonly ReadOnlySpan<char> _rowSpan;
    private readonly char _separator;

    internal CsvRow(ReadOnlySpan<char> rowSpan, char separator)
    {
        _rowSpan = rowSpan;
        _separator = separator;
    }

    public CellEnumerator GetEnumerator() => new CellEnumerator(_rowSpan, _separator);

    /// <summary>
    /// Enumerates individual cells (fields) within a CSV row.
    /// Each cell is returned as a ReadOnlySpan&lt;char&gt; over the original content.
    /// Quoted fields return the raw content between quotes (escaped "" is NOT unescaped).
    /// </summary>
    public ref struct CellEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        private readonly char _separator;
        private ReadOnlySpan<char> _current;
        private bool _finished;

        internal CellEnumerator(ReadOnlySpan<char> row, char separator)
        {
            _remaining = row;
            _separator = separator;
            _current = default;
            _finished = false;
        }

        public ReadOnlySpan<char> Current => _current;

        public bool MoveNext()
        {
            if (_finished) return false;

            if (_remaining.Length > 0 && _remaining[0] == '"')
            {
                // Quoted field: find the closing quote
                int i = 1;
                while (i < _remaining.Length)
                {
                    if (_remaining[i] == '"')
                    {
                        if (i + 1 < _remaining.Length && _remaining[i + 1] == '"')
                        {
                            i += 2; // escaped double-quote
                        }
                        else
                        {
                            // Closing quote found
                            _current = _remaining[1..i];
                            i++; // skip closing quote
                            if (i < _remaining.Length && _remaining[i] == _separator)
                            {
                                _remaining = _remaining[(i + 1)..];
                            }
                            else
                            {
                                _remaining = default;
                                _finished = true;
                            }
                            return true;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
                // Unterminated quote — treat remaining as the field
                _current = _remaining[1..];
                _remaining = default;
                _finished = true;
                return true;
            }
            else
            {
                // Non-quoted field
                int sepIdx = _remaining.IndexOf(_separator);
                if (sepIdx >= 0)
                {
                    _current = _remaining[..sepIdx];
                    _remaining = _remaining[(sepIdx + 1)..];
                }
                else
                {
                    _current = _remaining;
                    _remaining = default;
                    _finished = true;
                }
                return true;
            }
        }
    }
}
