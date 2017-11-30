using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SignalClient.Services
{
    public class CheckService
    {
        //private const string CHECK_API = "http://localhost:7071/api/DetectsCheck/";
        private const string CHECK_API = "https://[your-api].azurewebsites.net/api/DetectsCheck/";

        public static async Task<int> FacesDetectedAsync(string imageName)
        {
            int result = -1; // not processed

            using (var hc = new HttpClient())
            {
                var res = await hc.GetAsync(CHECK_API + imageName);
                if (res.IsSuccessStatusCode)
                {
                    result = int.Parse(await res.Content.ReadAsStringAsync());
                }
            }

            return result;
        }
    }
}
