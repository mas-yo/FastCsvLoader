public class FastCsvLoaderTests
{
    private static CsvData Parse(string content, char separator = ',')
    {
        return new CsvData(content, separator);
    }

    private static List<List<string>> ToLists(CsvData csv)
    {
        var rows = new List<List<string>>();
        foreach (var row in csv)
        {
            var cells = new List<string>();
            foreach (var cell in row)
                cells.Add(cell.ToString());
            rows.Add(cells);
        }
        return rows;
    }

    [Fact]
    public void SimpleCsv()
    {
        var result = ToLists(Parse("aaa,bbb,ccc\r\nzzz,yyy,xxx"));
        Assert.Equal(2, result.Count);
        Assert.Equal(["aaa", "bbb", "ccc"], result[0]);
        Assert.Equal(["zzz", "yyy", "xxx"], result[1]);
    }

    [Fact]
    public void TabSeparated()
    {
        var result = ToLists(Parse("a\tb\tc\r\n1\t2\t3", '\t'));
        Assert.Equal(2, result.Count);
        Assert.Equal(["a", "b", "c"], result[0]);
        Assert.Equal(["1", "2", "3"], result[1]);
    }

    [Fact]
    public void QuotedFieldsWithComma()
    {
        var result = ToLists(Parse("\"a,b\",c,\"d,e\""));
        Assert.Equal(1, result.Count);
        Assert.Equal(["a,b", "c", "d,e"], result[0]);
    }

    [Fact]
    public void EscapedDoubleQuotes()
    {
        // "aaa","b""bb","ccc" → fields: aaa, b""bb (raw), ccc
        var result = ToLists(Parse("\"aaa\",\"b\"\"bb\",\"ccc\""));
        Assert.Equal(1, result.Count);
        Assert.Equal("aaa", result[0][0]);
        Assert.Equal("b\"\"bb", result[0][1]); // raw: escaped quotes preserved
        Assert.Equal("ccc", result[0][2]);
    }

    [Fact]
    public void NewlineInsideQuotedField_CRLF()
    {
        var result = ToLists(Parse("\"a\r\nb\",c\r\nd,e"));
        Assert.Equal(2, result.Count);
        Assert.Equal(["a\r\nb", "c"], result[0]);
        Assert.Equal(["d", "e"], result[1]);
    }

    [Fact]
    public void NewlineInsideQuotedField_LF()
    {
        var result = ToLists(Parse("\"a\nb\",c\nd,e"));
        Assert.Equal(2, result.Count);
        Assert.Equal(["a\nb", "c"], result[0]);
        Assert.Equal(["d", "e"], result[1]);
    }

    [Fact]
    public void EmptyFields()
    {
        var result = ToLists(Parse(",,"));
        Assert.Equal(1, result.Count);
        Assert.Equal(["", "", ""], result[0]);
    }

    [Fact]
    public void EmptyContent()
    {
        var result = ToLists(Parse(""));
        Assert.Empty(result);
    }

    [Fact]
    public void TrailingCRLF()
    {
        var result = ToLists(Parse("a,b\r\n"));
        Assert.Single(result);
        Assert.Equal(["a", "b"], result[0]);
    }

    [Fact]
    public void NoTrailingCRLF()
    {
        var result = ToLists(Parse("a,b"));
        Assert.Single(result);
        Assert.Equal(["a", "b"], result[0]);
    }

    [Fact]
    public void SingleField()
    {
        var result = ToLists(Parse("hello"));
        Assert.Single(result);
        Assert.Equal(["hello"], result[0]);
    }

    [Fact]
    public void SingleRow_MultipleFields()
    {
        var result = ToLists(Parse("a,b,c"));
        Assert.Single(result);
        Assert.Equal(["a", "b", "c"], result[0]);
    }

    [Fact]
    public void MixedQuotedAndUnquoted()
    {
        var result = ToLists(Parse("\"quoted\",plain,\"also quoted\""));
        Assert.Single(result);
        Assert.Equal(["quoted", "plain", "also quoted"], result[0]);
    }

    [Fact]
    public void QuotedEmptyField()
    {
        var result = ToLists(Parse("\"\",a,\"\""));
        Assert.Single(result);
        Assert.Equal(["", "a", ""], result[0]);
    }

    [Fact]
    public void CRLineEnding()
    {
        var result = ToLists(Parse("a,b\rc,d"));
        Assert.Equal(2, result.Count);
        Assert.Equal(["a", "b"], result[0]);
        Assert.Equal(["c", "d"], result[1]);
    }

    [Fact]
    public void LFLineEnding()
    {
        var result = ToLists(Parse("a,b\nc,d"));
        Assert.Equal(2, result.Count);
        Assert.Equal(["a", "b"], result[0]);
        Assert.Equal(["c", "d"], result[1]);
    }

    [Fact]
    public void MultipleEmptyRows()
    {
        var result = ToLists(Parse("a\r\n\r\nb"));
        Assert.Equal(3, result.Count);
        Assert.Equal(["a"], result[0]);
        Assert.Equal([""], result[1]);
        Assert.Equal(["b"], result[2]);
    }

    [Fact]
    public void TrailingComma()
    {
        var result = ToLists(Parse("a,b,"));
        Assert.Single(result);
        Assert.Equal(["a", "b", ""], result[0]);
    }

    [Fact]
    public async Task LoadAsync_ReadsFile()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmpFile, "x,y\r\n1,2");
            var csv = await FastCsvLoader.LoadAsync(tmpFile);
            var result = ToLists(csv);
            Assert.Equal(2, result.Count);
            Assert.Equal(["x", "y"], result[0]);
            Assert.Equal(["1", "2"], result[1]);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public async Task LoadAsync_TsvFile()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmpFile, "a\tb\r\n1\t2");
            var csv = await FastCsvLoader.LoadAsync(tmpFile, '\t');
            var result = ToLists(csv);
            Assert.Equal(2, result.Count);
            Assert.Equal(["a", "b"], result[0]);
            Assert.Equal(["1", "2"], result[1]);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}
