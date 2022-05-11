using BigfootLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Test
{
    public class FragmentEnumeratorTest
    {
        [Fact]
        public void Test1()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                var stat = new Stat
                {
                    UserId = i + 1,
                    Name = $"User {i + 1}",
                    Type = StatType.Login,
                    Timestamp = DateTime.UtcNow.AddHours(-i),
                    ProviderIds = new List<int> { i % 10, (i + 1) % 11 },
                };
                sb.Append(JsonSerializer.Serialize(stat));
            }

            string json = sb.ToString();
            var bytes = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream(bytes);

            var fragments = new StreamJsonEnumerator(stream);
            var fragment = fragments.GetNextJsonFragment();
            while (!fragment.IsEmpty)
            {
                fragment = fragments.GetNextJsonFragment();
            }
        }
    }

    enum StatType { Login, Something, }

    class Stat
    {
        public StatType Type { get; set; }
        public int UserId { get; set; }
        public string? Name { get; set; }
        public DateTime Timestamp { get; set; }
        public List<int> ProviderIds { get; set; } = new List<int>();
    };
}