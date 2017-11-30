using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Media.Capture;
using System.Threading;
using Windows.Storage;
using System.Diagnostics;

namespace SignalClient
{
    public class IpCamera
    {
        public string IpAddress { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string CameraUrlTemplate { get; set; } = "http://{IpAddress}/Streaming/channels/1/picture";

        [JsonIgnore]
        public HttpClient HttpClient { get; set; }


        public IpCamera(string ipAddress, string username, string password)
        {
            IpAddress = ipAddress;
            Username = username;
            Password = password;

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.Credentials = new NetworkCredential(Username, Password);
            HttpClient = new HttpClient(clientHandler);
            HttpClient.Timeout = TimeSpan.FromSeconds(2);
        }

        public async Task<bool> CheckConnectivityAsync()
        {
            if (IpAddress == null) throw new ArgumentException("IP address cannot be null.");

            try
            {
                //var response = await HttpClient.GetAsync($"http://{IpAddress}");
                //response.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HttpClient.GetAsync($'http://{IpAddress}') " + ex.Message);
                return false;
            }

        }

        public async Task<Stream> CaptureImageAsync()
        {
            Stream result = null;
            try
            {
                result = await ReadSnapshotAsStream();
            }
            catch (Exception error)
            {
                Debug.WriteLine($"CaptureImageAsync IP: {IpAddress} " + error.Message);
            }

            return result;
        }

        /// <summary>
        /// Gets a single snapshot.
        /// </summary>
        private async Task<Stream> ReadSnapshotAsStream()
        {
            Stream result = null;
            MemoryStream ms = new MemoryStream();
            string url = CameraUrlTemplate.Replace("{IpAddress}", IpAddress);

            using (HttpResponseMessage response = await this.HttpClient.GetAsync(url))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Debug.WriteLine($"Error while querying '{url}'.");

                    string text = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine(text);
                    throw new Exception(response.ToString());
                }

                result = await response.Content.ReadAsStreamAsync();
                result.Seek(0, SeekOrigin.Begin);

                result.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            }

            return ms;
        }

        public async Task<SoftwareBitmap> ConvertStreamToBitmap(Stream stream)
        {
            SoftwareBitmap result = null;

            stream.Seek(0, 0);

            using (Stream newStream = new MemoryStream())
            {
                stream.CopyTo(newStream);
                newStream.Seek(0, 0);

                using (var randStream = newStream.AsRandomAccessStream())
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randStream);
                    result = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat, BitmapAlphaMode.Premultiplied);

                    randStream.Dispose();
                }
                newStream.Dispose();

            }
            stream.Dispose();

            return SoftwareBitmap.Copy(result);
        }

    }


}