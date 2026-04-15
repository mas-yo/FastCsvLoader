// FastCsvLoader demo
var csv = await FastCsvLoader.LoadAsync(args[0], args.Length > 1 ? args[1][0] : ',');
PrintCsv(csv);

static void PrintCsv(CsvData csv)
{
    foreach (var row in csv)
    {
        var first = true;
        foreach (var cell in row)
        {
            if (!first) Console.Write(" | ");
            Console.Out.Write(cell);
            first = false;
        }
        Console.WriteLine();
    }
}
