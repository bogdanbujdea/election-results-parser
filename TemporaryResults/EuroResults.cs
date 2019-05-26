using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TemporaryResults
{
    public static class EuroResults
    {
        [FunctionName("euro")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var html = new StringBuilder();
            html.Append("<html><body>");

            await GetHtmlResults(html, "tara", 8954959, "results");
            await GetHtmlResults(html, "diaspora", 369775, "diaspora");

            html.Append(
                "Sursa: Voturi provizorii de pe <a  target=\"blank\" href=\"https://prezenta.bec.ro/europarlamentare26052019/romania-pv-temp\">https://prezenta.bec.ro/europarlamentare26052019/romania-pv-temp</a>");
            html.Append("<br/><br/>Bogdan Bujdea - <a target=\"blank\" href=\"https:/twitter.com/thewindev\">@thewindev</a>");
            html.Append("</body></html>");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(html.ToString()));
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        private static async Task<StringBuilder> GetHtmlResults(StringBuilder html, string name, decimal voteCount, string fileName)
        {
            //var csv = File.ReadAllText($"d:\\{fileName}.csv");
            var csv = await new HttpClient().GetStringAsync($"https://diettrackerstorage.blob.core.windows.net/results/{fileName}.csv");
            var dictionary = await RetrieveResults(csv);
            var sum = dictionary.Sum(c => c.Value);
            var index = 1;
            html.Append($"<br/>");
            html.Append($"<h2>Voturi {name}</h2><br />");
            html.Append($"Total voturi {name}: {voteCount}<br />");
            decimal countedPercentage = Math.Round(sum / (decimal)voteCount * 100, 2);
            html.Append($"Voturi numarate {name}: {sum} - {Math.Round(countedPercentage, 2)}%<br /><br />");
            html.Append($"<br/>");
            foreach (var kvp in dictionary.OrderByDescending(d => d.Value))
            {
                decimal percentage = Math.Round((decimal) kvp.Value / (decimal) sum * 100, 2);
                html.Append($"{index++}. {kvp.Key} - {kvp.Value} - {percentage}%");
                html.Append($"<br/>");
            }

            return html;
        }

        private static async Task<Dictionary<string, int>> RetrieveResults(string csv)
        {
            var csvParser = new CsvParser(new StringReader(csv));
            var headers = (await csvParser.ReadAsync()).ToList();
            var candidates = new Dictionary<string, int>
    {
        {"PSD", 0},
        {"USR-PLUS", 0},
        {"PRO Romania", 0},
        {"UDMR", 0},
        {"PNL", 0},
        {"ALDE", 0},
        {"PRO DEMO", 0},
        {"PMP", 0},
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
                }

            } while (results != null);

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates.ElementAt(i);
                Debug.WriteLine($"{candidate.Key} has {candidate.Value} votes");
            }

            return candidates;
        }

    }
}
