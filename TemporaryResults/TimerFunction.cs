using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TemporaryResults
{
    public static class TimerFunction
    {
        [FunctionName("TimerFunction")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Uploading files");

            await UpdateResults("https://prezenta.bec.ro/europarlamentare26052019/data/pv/csv/pv_RO_EUP_PROV.csv", "provizoriiro.csv", log);
            log.LogInformation($"Romania results uploaded successfully");

            await UpdateResults("https://prezenta.bec.ro/europarlamentare26052019/data/pv/csv/pv_RO_EUP_PART.csv", "partialero.csv", log);
            log.LogInformation($"Romania partial results uploaded successfully");

            await UpdateResults("https://prezenta.bec.ro/europarlamentare26052019/data/pv/csv/pv_RO_EUP_FINAL.csv", "finalero.csv", log);
            log.LogInformation($"Romania final results uploaded successfully");

            await UpdateResults("https://prezenta.bec.ro/europarlamentare26052019/data/pv/csv/pv_SR_EUP_PROV.csv", "provizoriidiaspora.csv", log);
            log.LogInformation($"Diaspora results uploaded successfully");

            await UpdateResults("https://prezenta.bec.ro/europarlamentare26052019/data/pv/csv/pv_SR_EUP_PART.csv", "partialediaspora.csv", log);
            log.LogInformation($"Diaspora partial results uploaded successfully");

            await UpdateResults("https://prezenta.bec.ro/europarlamentare26052019/data/pv/csv/pv_SR_EUP_FINAL.csv", "finalediaspora.csv", log);
            log.LogInformation($"Diaspora partial results uploaded successfully");

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private static async Task UpdateResults(string url, string fileName, ILogger log)
        {
            var response = await new HttpClient().GetStringAsync(url);
            if (response.StartsWith("<!DOCTYPE html>")) //it should be a csv, so if it starts with this it means I most likely got back a 404 page
            {
                log.LogInformation($"Got 404");
                return;
            }
            await UploadFileToStorage(new MemoryStream(Encoding.UTF8.GetBytes(response)), fileName);
        }

        private static async Task<bool> UploadFileToStorage(Stream fileStream, string fileName)
        {
            var storageCredentials = new StorageCredentials("<bloc name>", "<key>");
            var storageAccount = new CloudStorageAccount(storageCredentials, true);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("results");
            var blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.UploadFromStreamAsync(fileStream );
            return await Task.FromResult(true);
        }
    }
}
