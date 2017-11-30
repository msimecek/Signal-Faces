using SignalClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SignalClient.Services
{
    public class ConfigService
    {
        public static ConfigModel LoadConfig()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var result = new ConfigModel();

            return new ConfigModel()
            {
                CameraIpAddress = GetSettingOrNull<string>(localSettings, "CameraIpAddress"),
                CameraTemplate = GetSettingOrNull<string>(localSettings, "CameraTemplate"),
                CameraAdmin = GetSettingOrNull<string>(localSettings, "CameraAdmin"),
                CameraPassword = GetSettingOrNull<string>(localSettings, "CameraPassword"),
                CaptureInterval = GetSettingOrNull<int>(localSettings, "CaptureInterval"),
                EnableUpload = GetSettingOrNull<bool>(localSettings, "EnableUpload"),
                DelayAfterUpload = GetSettingOrNull<int>(localSettings, "DelayAfterUpload")
            };
        }

        public static void SaveConfig(ConfigModel model)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["CameraIpAddress"] = model.CameraIpAddress;
            localSettings.Values["CameraTemplate"] = model.CameraTemplate;
            localSettings.Values["CameraAdmin"] = model.CameraAdmin;
            localSettings.Values["CameraPassword"] = model.CameraPassword;
            localSettings.Values["CaptureInterval"] = model.CaptureInterval;
            localSettings.Values["EnableUpload"] = model.EnableUpload;
            localSettings.Values["DelayAfterUpload"] = model.DelayAfterUpload;
        }

        private static T GetSettingOrNull<T>(ApplicationDataContainer settingsContainer, string key)
        {
            var v1 = settingsContainer.Values.FirstOrDefault(v => v.Key == key);
            if (v1.Key == null) return default(T);
            
            return (T)settingsContainer.Values.FirstOrDefault(v => v.Key == key).Value;
        }
    }
}
