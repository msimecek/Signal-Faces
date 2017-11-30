using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using SignalFunc.Models;

namespace SignalFunc
{
    public static class DetectsCheck
    {
        [FunctionName("DetectsCheck")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DetectsCheck/{imageName}")]HttpRequestMessage req, 
            string imageName,
            [Table("detects", " ", "{imageName}", Connection = "AzureWebJobsStorage")] DetectEntity detect,
            TraceWriter log)
        {
            if (detect == null)
                return req.CreateResponse(HttpStatusCode.OK, DetectEntity.DetectState.NotProcessed);

            return req.CreateResponse(HttpStatusCode.OK, detect.Faces);
        }

        
    }
}
