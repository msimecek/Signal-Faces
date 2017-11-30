using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace SignalClient.Services
{
    public class StorageService
    {
        private const string CONTAINER_SAS = "[SAS URL]";

        public static async Task<string> UploadImageAsync(Stream image)
        {
            var container = new CloudBlobContainer(new Uri(CONTAINER_SAS));
            string fileName = Guid.NewGuid().ToString() + ".jpg";
            CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
            await blob.UploadFromStreamAsync(image);

            return fileName;
        }
    }
}
