using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using SignalFunc.Models;

namespace SignalFunc
{
    public static class ImageProcessor
    {
        private static Dictionary<string, string[]> CategoryMapping = new Dictionary<string, string[]>()
        {
            { "Headwear", new string[] { "Bekovka", "Kapuce", "Klobouk", "Ksiltovka", "Kulich", "Cepice" } },
            { "Accessories", new string[] { "Destnik", "Kabelka", "Sala" } },
            { "Outerwear", new string[] { "Bunda", "Bunda perova", "Bunda sportovni", "Kabat", "Sako", "Kosile kostkovana", "Tricko" } },
            { "Other", new string[] { "Batoh", "Boty" } }
        };

        [FunctionName("ImageProcessor")]
        public static void Run([BlobTrigger("photos/{name}", Connection = "AzureWebJobsStorage")]Stream photo,
                                string name,
                                [DocumentDB("SignalData", "Faces", ConnectionStringSetting = "CosmosDbConnection")]out dynamic document,
                                [Table("detects", Connection = "AzureWebJobsStorage")]out DetectEntity detectCheck,
                                TraceWriter log)
        {
            log.Info($"Processing:{name} \n Size: {photo.Length} Bytes");

            //DEBUG
            //document = null;
            //return;

            var result = new DetectionResult()
            {
                DateTime = DateTime.Now,
                Source = DetectionResult.SOURCE_PRODUCTION,
                Image = Utils.EncodeStreamToBase64(photo),
                Tags = new List<Tag>()
            };

            // Send image to FaceAPI
            MemoryStream detectPhotoStream = new MemoryStream();
            photo.CopyTo(detectPhotoStream);
            detectPhotoStream.Seek(0, SeekOrigin.Begin);

            if (detectPhotoStream.Length == 0)
            {
                log.Error("Image size 0.");
                document = null;
                detectCheck = new DetectEntity(name, 0);
                return;
            }

            Face firstFace = GetFirstFaceAsync(detectPhotoStream, log).Result;
            if (firstFace == null)
            {
                document = null;
                detectCheck = new DetectEntity(name, 0);
                return;
            }

            detectCheck = new DetectEntity(name, 1);

            result.Tags.Add(new Tag("Age", firstFace.FaceAttributes.Age));
            result.Tags.Add(new Tag("Gender", firstFace.FaceAttributes.Gender));
            result.Tags.Add(new Tag("Smile", firstFace.FaceAttributes.Smile, firstFace.FaceAttributes.Smile));

            int glasses = firstFace.FaceAttributes.Glasses > 0 ? 1 : 0;
            result.Tags.Add(new Tag("Glasses", glasses, glasses));

            Tuple<string, double> facialHair = GetTopFacialHair(firstFace.FaceAttributes.FacialHair);
            result.Tags.Add(new Tag("FacialHair", facialHair.Item1, facialHair.Item2));

            result.FaceRectangle = firstFace.FaceRectangle;

            if (Environment.GetEnvironmentVariable("UseTagRecognition", EnvironmentVariableTarget.Process).ToLower() == bool.TrueString.ToLower())
            {
                // Send image to Custom Vision
                var predictionClient = new CustomVisionClient(
                    Environment.GetEnvironmentVariable("PredictionKey"),
                    Environment.GetEnvironmentVariable("PredictionEndpoint"));

                PredictionResult predRes = predictionClient.PredictAsync(photo).Result;

                if (predRes == null)
                {
                    log.Info("Prediction result empty.");
                }
                else
                {
                    // Put results into single JSON
                    // - results coming from API are ordered by Probabilty, taking first 4
                    var firstResults = predRes.Predictions.Take(4);
                    foreach (var pr in firstResults)
                    {
                        string category = CategoryMapping.Where(c => c.Value.Contains(pr.Tag)).FirstOrDefault().Key;
                        result.Tags.Add(new Tag(category, pr.Tag, pr.Probability));
                    }
                }
            }

            // Send to front-end API
            using (var hc = new HttpClient())
            {
                var res = hc.PostAsync(Environment.GetEnvironmentVariable("SaveApiEndpoint"),
                    new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")).Result;

                if (res.IsSuccessStatusCode)
                {
                    log.Info("Data sent to API.");
                }
                else
                {
                    log.Error("Unable to send data to API. (" + res.Content.ReadAsStringAsync().Result + ")");
                }
            }

            // Save to DocumentDB
            result.Image = null; // neukládáme obrázek
            document = result;

            log.Info(JsonConvert.SerializeObject(result.Tags, Formatting.Indented));
        }

        private static async Task<Face> GetFirstFaceAsync(Stream image, TraceWriter log)
        {
            var faceClient = new FaceServiceClient(
                                Environment.GetEnvironmentVariable("FaceAPIKey", EnvironmentVariableTarget.Process),
                                Environment.GetEnvironmentVariable("FaceAPIEndpoint", EnvironmentVariableTarget.Process));

            try
            {
                Face[] detectResult = await faceClient.DetectAsync(
                    image,
                    returnFaceAttributes:
                        new FaceAttributeType[] {
                        FaceAttributeType.Age,
                        FaceAttributeType.FacialHair,
                        FaceAttributeType.Gender,
                        FaceAttributeType.Smile,
                        FaceAttributeType.Glasses
                        },
                    returnFaceLandmarks: true);

                if (detectResult.Length > 0)
                {
                    Face firstFace = detectResult[0];
                    return firstFace;
                }
                else
                {
                    //TODO: no face - skonèit / vyvolat znovu capture?
                    log.Info("No faces detected.");
                    return null;
                }
            }
            catch (Exception ex) when ((ex is AggregateException && ex.InnerException is FaceAPIException) || ex is FaceAPIException)
            {
                FaceAPIException exp = null;

                if (ex is FaceAPIException)
                {
                    exp = (FaceAPIException)ex;
                }
                else if (ex is AggregateException && ex.InnerException is FaceAPIException)
                {
                    exp = (FaceAPIException)ex.InnerException;
                }

                log.Error("Detection Error: " + exp?.ErrorMessage);

                // pokud bude code 429, poèkat chvíli a spustit znovu

                return null;
            }
        }

        /// <summary>
        /// Returns the name and confidence of top facial hair. Possible values: Beard | Moustache
        /// Returns "Beard" and 0 when none found.
        /// </summary>
        /// <returns>name, confidence</returns>
        private static Tuple<string, double> GetTopFacialHair(FacialHair input)
        {
            if (input.Beard >= input.Moustache)
            {
                return new Tuple<string, double>("Beard", input.Beard);
            }

            if (input.Moustache > input.Beard)
            {
                return new Tuple<string, double>("Moustache", input.Moustache);
            }

            return new Tuple<string, double>("Beard", 0);
        }

    }
}
