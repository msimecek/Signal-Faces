using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalClient.Models
{
    public class ConfigModel
    {
        public string CameraIpAddress { get; set; }
        public string CameraTemplate { get; set; }
        public string CameraAdmin { get; set; }
        public string CameraPassword { get; set; }
        public int CaptureInterval { get; set; } = 2;
        public int DelayAfterUpload { get; set; }
        public bool EnableUpload { get; set; }
    }
}
