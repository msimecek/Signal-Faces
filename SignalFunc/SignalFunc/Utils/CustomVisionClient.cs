using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SignalFunc
{
    public class CustomVisionClient
    {
        public string PredictionKey { get; set; }
        public string PredictionEndpoint { get; set; }

        public CustomVisionClient(string predictionKey, string predictionEndpoint)
        {
            PredictionKey = predictionKey;
            PredictionEndpoint = predictionEndpoint;
        }

        public async Task<PredictionResult> PredictAsync(Stream image)
        {
            PredictionResult result = null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", this.PredictionKey);

                image.Seek(0, SeekOrigin.Begin);
                var content = new StreamContent(image);
                HttpResponseMessage response = await client.PostAsync(this.PredictionEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    string res = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<PredictionResult>(res);
                }
            }

            return result;
        }
    }

    public class PredictionResult
    {
        public string Id { get; set; }
        public Prediction[] Predictions { get; set; }
        public string Created { get; set; }
        public string Iteration { get; set; }
        public string Project { get; set; }
    }

    public class Prediction
    {
        public string Tag { get; set; }
        public double Probability { get; set; }
        public string TagId { get; set; }
    }
}
