using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalFunc
{
    public class Utils
    {
        public static string EncodeStreamToBase64(Stream input)
        {
            MemoryStream output = new MemoryStream();
            input.Seek(0, SeekOrigin.Begin);
            input.CopyTo(output);
            input.Seek(0, SeekOrigin.Begin);
            return Convert.ToBase64String(output.ToArray());
        }
    }
}
