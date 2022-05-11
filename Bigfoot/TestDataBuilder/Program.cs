// See https://aka.ms/new-console-template for more information
using BigfootLib;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

var sb = new StringBuilder();

string path = "c:\\temp\\json_bytes_brotli_smallest.txt";

const bool doWrite = false;
if (doWrite)
{
    var fileStream = File.Create(path);
    var writeStream = new System.IO.Compression.BrotliStream(fileStream, System.IO.Compression.CompressionLevel.SmallestSize);

    for (int j = 0; j < 100; j++)
    {
        Console.WriteLine(j);
        sb.Clear();
        for (int i = 0; i < 100; i++)
        {
            var stat = new Stat
            {
                UserId = i + 1 + j,
                Name = $"User {i + 1 + j}",
                Type = StatType.Login,
                Timestamp = DateTime.UtcNow.AddHours(-i),
                ProviderIds = new List<int> { i % 10, (i + 1 % 11) },
            };
            sb.AppendLine(JsonSerializer.Serialize(stat));
            string json = sb.ToString();
            var bytes = Encoding.UTF8.GetBytes(json);
            writeStream.Write(bytes);
        }
    }

    fileStream.Close();
    return;
}

var fileReadStream = File.OpenRead(path);
var readStream = new System.IO.Compression.BrotliStream(fileReadStream, System.IO.Compression.CompressionMode.Decompress);

//var tmpFs = File.Create(path + "2");
//readStream.CopyTo(tmpFs);
//tmpFs.Close();

//Console.WriteLine($"Done");
//return;

// Command:
//  Login count per user (type = Login), sorted by latest login desc
//  Login counts per provider, per day (yyyy-mm-dd), sorted by provider id asc, day desc

int tokens = 0;
int fragments = 0;

var sw = Stopwatch.StartNew();

var jsonFragments = new StreamJsonEnumerator(readStream);

var fragmentSpan = jsonFragments.GetNextJsonFragment();

while (!fragmentSpan.IsEmpty)
{
    string f = Encoding.UTF8.GetString(fragmentSpan);

    var reader = new Utf8JsonReader(fragmentSpan);
    while (reader.Read())
    {
        tokens++;
    }

    fragmentSpan = jsonFragments.GetNextJsonFragment();
    fragments++;
    Console.WriteLine($"Fragments: {fragments}, Tokens: {tokens}, ms: {sw.Elapsed}");
}

long elapsed = sw.ElapsedMilliseconds;
Console.WriteLine($"Fragments: {fragments}, Tokens: {tokens}, ms: {elapsed}");

enum StatType { Login, Something, }

class Stat
{
    public StatType Type { get; set; }
    public int UserId { get; set; }
    public string? Name { get; set; }
    public DateTime Timestamp { get; set; }
    public List<int> ProviderIds { get; set; } = new List<int>();
};
