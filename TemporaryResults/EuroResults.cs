using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            var type = req.Query["type"];
            if (string.IsNullOrWhiteSpace(type))
                type = "finale";
            var localFileName = type + "ro";
            var diasporaFileName = type + "diaspora";
            html.Append($"<h1>Rezultate {type}</h1></br> " +
                        "<br/><a target=\"blank\" href=\"?code=0ubeBbfmos0UYZcbWmvzajhu5QSdM8Wx/O311I/E0VFgmQa9hZ1zlw==&type=provizorii\">Puteti vedea rezultatele provizorii aici</a>"+
                        "<br/><a target=\"blank\" href=\"?code=0ubeBbfmos0UYZcbWmvzajhu5QSdM8Wx/O311I/E0VFgmQa9hZ1zlw==&type=partiale\">Puteti vedea rezultatele partiale aici</a>"+
                        "<br/><a target=\"blank\" href=\"?code=0ubeBbfmos0UYZcbWmvzajhu5QSdM8Wx/O311I/E0VFgmQa9hZ1zlw==&type=finale\">Puteti vedea rezultatele finale aici</a>");
            html.Append($"<br/><br/>DISCLAIMER: Pot fi diferente intre rezultatele de aici si cele oficiale. Am scos voturile anulate, dar procentajul nu pare sa fie corect la procentajul de voturi numarate.<br/>");

            (Dictionary<string, int> candidates, int cancelledVotes) localResults = await GetHtmlResults(html, "tara", 8954959, localFileName);
            var localVotes = localResults.candidates;
            var diasporaResults = await GetHtmlResults(html, "diaspora", 375219, diasporaFileName);
            var diaspora = diasporaResults.candidates;
            var totalCancelledVotes = localResults.cancelledVotes + diasporaResults.cancelledVotes;
            foreach (var vote in localVotes)
            {
                diaspora[vote.Key] += vote.Value;
            }
            AddResultsFromVotes(html, "Romania + diaspora", (8954959 + 375219), diaspora, totalCancelledVotes);

            html.Append($"<br/>");
            html.Append("Sursa: Voturi provizorii de pe <a  target=\"blank\" href=\"https://prezenta.bec.ro/europarlamentare26052019/romania-pv-temp\">https://prezenta.bec.ro/europarlamentare26052019/romania-pv-temp</a>");
            html.Append("<br/><br/>Bogdan Bujdea - <a target=\"blank\" href=\"https://twitter.com/thewindev\">@thewindev</a>");
            html.Append("<br/><br/> <a target=\"blank\" href=\"https://code4.ro/ro\">Code for Romania</a>");
            html.Append("</body></html>");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(html.ToString()));
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        private static async Task<(Dictionary<string, int> candidates, int cancelledVotes)> GetHtmlResults(StringBuilder html, string name, decimal voteCount, string fileName)
        {
            //var csv = File.ReadAllText($"d:\\partialero.csv");
            var csv = await new HttpClient().GetStringAsync($"https://diettrackerstorage.blob.core.windows.net/results/{fileName}.csv");
            var (candidates, cancelledVotes) = await RetrieveResults(csv);
            AddResultsFromVotes(html, name, voteCount, candidates, cancelledVotes);
            return (candidates, cancelledVotes);
        }

        private static void AddResultsFromVotes(StringBuilder html, string name, decimal voteCount, Dictionary<string, int> dictionary, int cancelledVotes)
        {
            var sum = dictionary.Sum(c => c.Value);
            var index = 1;
            html.Append($"<br/>");
            html.Append($"<h2>Voturi {name}</h2><br />");
            html.Append($"Total voturi {name}: {voteCount}<br />");
            html.Append($"Total voturi anulate in {name}: {cancelledVotes}<br />");
            voteCount -= cancelledVotes;
            var countedPercentage = Math.Round(sum / (decimal)voteCount * 100, 2);
            html.Append($"Voturi numarate {name}: {sum} - <b>{Math.Round(countedPercentage, 2)}% </b>*sunt sanse ca procentajul voturilor numarate sa fie gresit<br /><br />");
            html.Append($"<br/>");
            html.Append($"<table >");
            html.Append($"<tr>");
            html.Append($"<th>Pozitie</th>");
            html.Append($"<th>Nume</th>");
            html.Append($"<th>Procent</th>");
            html.Append($"<th>Voturi</th>");
            html.Append($"<th>Distanta fata de cel anterior</th>");
            html.Append($"</tr>");
            KeyValuePair<string, int> lastKvp = new KeyValuePair<string, int>();
            foreach (var kvp in dictionary.OrderByDescending(d => d.Value))
            {
                decimal percentage = Math.Round((decimal)kvp.Value / (decimal)sum * 100, 2);
                html.Append($"<tr>");
                html.Append($"<td>{index++}</td><td>{kvp.Key}</td><td><b>{percentage}%</b></td><td>{kvp.Value:N0}</td>");
                if (lastKvp.Value != 0)
                {
                    html.Append(
                        $"<td><b>{(lastKvp.Value - kvp.Value):N0}</b></td>");
                }

                lastKvp = kvp;
                html.Append($"</tr>");
            }
            html.Append($"</table >");

        }

        private static async Task<(Dictionary<string, int> candidates, int cancelledVotes)> RetrieveResults(string csv)
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
                {"Peter Costea", 0}
            };
            string[] results;
            var cancelledVotes = 0;
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
                cancelledVotes += int.Parse(results[headers.IndexOf($"f")]);
            } while (results != null);

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates.ElementAt(i);
                Debug.WriteLine($"{candidate.Key} has {candidate.Value} votes");
            }

            return (candidates, cancelledVotes);
        }

    }
}
