
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;

namespace MLYSO.Web.Services;

public class CsvOptionService
{
    private readonly object _gate = new();
    private bool _initialized = false;
    private Dictionary<string, HashSet<string>> _options = new();

    public Task Init(string contentRoot, IConfiguration cfg)
    {
        lock (_gate)
        {
            if (_initialized) return Task.CompletedTask;
            _initialized = true;
        }

        var csvRel = cfg.GetSection("Dataset")["CsvPath"] ?? "App_Data/dataset/dynamic_supply_chain_logistics_dataset.csv";
        var csvPath = Path.Combine(contentRoot, csvRel.Replace('/', Path.DirectorySeparatorChar));
        var maxRowsStr = cfg.GetSection("Dataset")["LoadMaxRows"];
        var maxRows = int.TryParse(maxRowsStr, out var m) ? m : 5000;

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"[CsvOptionService] CSV not found at {csvPath}. Options will be empty until you place the file.");
            _options = new();
            return Task.CompletedTask;
        }

        var opts = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(csvPath);
        string? header = reader.ReadLine();
        if (header == null) { _options = new(); return Task.CompletedTask; }
        var cols = header.Split(',');

        for (int i = 0; i < cols.Length; i++)
            opts[cols[i].Trim()] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Simple CSV parsing; assumes no commas inside values. For complex CSVs plug in CsvHelper NuGet.
        string? line;
        int row = 0;
        while ((line = reader.ReadLine()) != null && row < maxRows)
        {
            row++;
            var parts = line.Split(',');
            for (int i = 0; i < Math.Min(cols.Length, parts.Length); i++)
            {
                var v = parts[i].Trim();
                if (!string.IsNullOrEmpty(v))
                {
                    if (v.Length > 80) v = v.Substring(0, 80);
                    opts[cols[i]].Add(v);
                }
            }
        }
        _options = opts;
        Console.WriteLine($"[CsvOptionService] Loaded options from {row} rows [{_options.Count} columns].");
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetColumns() => _options.Keys.OrderBy(k => k);

    public IEnumerable<string> GetOptions(string field)
    {
        return _options.TryGetValue(field, out var set) ? set.OrderBy(x => x) : Enumerable.Empty<string>();
    }

    public Dictionary<string, IEnumerable<string>> GetAllOptions(int maxPerField = 50)
    {
        return _options.ToDictionary(k => k.Key, v => v.Value.OrderBy(x => x).Take(maxPerField).AsEnumerable());
    }
}
