using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;

namespace ElectionCsvParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RetrieveResults();
        }

        private static async Task RetrieveResults()
        {
            var csv = File.ReadAllText("d:\\data.csv");
            var csvParser = new CsvParser(new StringReader(csv));
            var headers = (await csvParser.ReadAsync()).ToList();
            var candidates = new Dictionary<string, int>
            {
                {"PSD", 0},
                {"USR", 0},
                {"PRO Romania", 0},
                {"UDMR", 0},
                {"PMP", 0},
                {"PNL", 0},
                {"ALDE", 0},
                {"PRO DEMO", 0},
                {"PSR", 0},
                {"PSDI", 0},
                {"PRU", 0},
                {"UNPR", 0},
                {"BUN", 0},
                {"Gregoriana Tudoran", 0},
                {"George Simion", 0},
                {"Peter Costea", 0},
            };
            string[] results;
            do
            {
                results = await csvParser.ReadAsync();
                if (results == null)
                    break;
                for (int i = 0; i < 16; i++)
                {
                    var votes = int.Parse(results[headers.IndexOf($"g{i + 1}")]);
                    var candidate = candidates.ElementAt(i);
                    candidates[candidate.Key] += votes;

                    //candidate = new KeyValuePair<string, int>(candidate.Key, candidate.Value + votes);
                }

                // var usr = int.Parse(results[headers.ToList().IndexOf("g2")]);
                // votes["usr"] += usr;
            } while (results != null);

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates.ElementAt(i);
                Debug.WriteLine($"{candidate.Key} has {candidate.Value} votes");
            }
        }
    }
}
