using Newtonsoft.Json;
using SignalClient.Models;
using SignalClient.Services;
using SignalClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Networking.Connectivity;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SignalClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainPageVM _vm = new MainPageVM();

        private IpCamera _camera;
        private ThreadPoolTimer _captureTimer;
        private bool _waiting = false;

        private ConfigModel _config;

        public MainPage()
        {
            this.InitializeComponent();
            RegisterNetworkStatusChange();

            _config = ConfigService.LoadConfig();

            _vm.Admin = _config.CameraAdmin;
            _vm.Interval = _config.CaptureInterval;
            _vm.IpAddress = _config.CameraIpAddress;
            _vm.Pass = _config.CameraPassword;
            _vm.UploadEnabled = _config.EnableUpload;
            _vm.Delay = _config.DelayAfterUpload;
            _vm.CameraTemplate = _config.CameraTemplate == null ? "http://{IpAddress}/Streaming/channels/1/picture" : _config.CameraTemplate;

            this.DataContext = _vm;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeCapture();

            base.OnNavigatedTo(e);
        }

        private void InitializeCapture()
        {
            if (!string.IsNullOrEmpty(_config.CameraIpAddress))
            {
                _camera = new IpCamera(_config.CameraIpAddress, _config.CameraAdmin, _config.CameraPassword);
                _camera.CameraUrlTemplate = _vm.CameraTemplate;

            if (_captureTimer != null)
                _captureTimer.Cancel();

            _captureTimer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromSeconds(_config.CaptureInterval));
                Log("Capture running.");
            }
            else
            {
                Log("Camera IP address not configured.");
            }
        }

        private async void Timer_Tick(ThreadPoolTimer timer)
        {
            await Capture();
        }

        private async Task Capture()
        {
            if (_waiting) return;

            Stream imageStream = await _camera.CaptureImageAsync();
            if (imageStream == null)
            {
                Log("Capture failed.");
                return;
            }

            MemoryStream bitmapStream = new MemoryStream();
            imageStream.CopyTo(bitmapStream);
            SoftwareBitmap image = await _camera.ConvertStreamToBitmap(bitmapStream);
            SoftwareBitmap convertedImage = SoftwareBitmap.Convert(image, BitmapPixelFormat.Nv12);

            if (!FaceDetector.IsBitmapPixelFormatSupported(convertedImage.BitmapPixelFormat))
            {
                Log($"Unsupported pixel format! ({convertedImage.BitmapPixelFormat})");
                return;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                var imgSrc = new SoftwareBitmapSource();
                await imgSrc.SetBitmapAsync(image);
                imgCapture.Source = imgSrc;
            });


            var fd = await FaceDetector.CreateAsync();
            var faces = await fd.DetectFacesAsync(convertedImage);
            if (faces.Count == 0)
            {
                // no faces, nothing to do
                Log("No faces detected.", false);
                return;
            }

            Log($"{faces.Count} faces.", false);

            _waiting = true; // block any other processing

            if (_config.EnableUpload)
            {
                // face detected locally, send to Storage
                imageStream.Seek(0, SeekOrigin.Begin);
                string fileName = await StorageService.UploadImageAsync(imageStream);

                Log($"Sent to processing.");

                int serverFacesDetected;
                while ((serverFacesDetected = await CheckService.FacesDetectedAsync(fileName)) == -1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                if (serverFacesDetected > 0)
                {
                    // something detected on server, activate delay
                    Log($"Faces found on server. Waiting {_vm.Delay} seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(_vm.Delay));
                }
                else
                {
                    Log("No faces found on server. Skipping delay.");
                }
            }

            imageStream.Dispose();
            bitmapStream.Dispose();

            _waiting = false;
        }

        private async Task ForceUploadAsync()
        {
            Stream imageStream = await _camera.CaptureImageAsync();
            if (imageStream == null)
            {
                Log("Capture failed.");
                return;
            }

            await StorageService.UploadImageAsync(imageStream);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _config.CameraIpAddress = _vm.IpAddress;
            _config.CaptureInterval = _vm.Interval;
            _config.EnableUpload = _vm.UploadEnabled;
            _config.CameraAdmin = _vm.Admin;
            _config.CameraPassword = _vm.Pass;
            _config.DelayAfterUpload = _vm.Delay;
            _config.CameraTemplate = _vm.CameraTemplate;
           

            ConfigService.SaveConfig(_config);
            Log("New config saved. Restarting capture.");

            InitializeCapture();
        }

        private void Log(string message, bool includeInHistory = true)
        {
            Debug.WriteLine(message);
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                txtStatus.Text = message;
                if (includeInHistory) logHistory.Text += message + "\n";
            });
        }

        private void RegisterNetworkStatusChange()
        {
            try
            {
                var networkStatusCallback = new NetworkStatusChangedEventHandler(OnNetworkStatusChange);
                NetworkInformation.NetworkStatusChanged += networkStatusCallback;
            }
            catch (Exception ex)
            {
                Log("Error when registering for network status change.");
            }
        }

        private void OnNetworkStatusChange(object sender)
        {
            ConnectionProfile internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (internetConnectionProfile == null)
            {
                // not connected
                Log("Internet disconnected.");
                _captureTimer.Cancel();
                _captureTimer = null;
            }
            else
            {
                // connected
                Log("Internet reconnected.");
                InitializeCapture();
            }
        }

        private bool IsOnline
        {
            get
            {
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_captureTimer != null)
                InitializeCapture();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (_captureTimer != null)
            {
                _captureTimer.Cancel();
                _captureTimer = null;
            }
            imgCapture.Source = null;
            Log("Capture stopped.");
        }

        private void ResetTemplate_Click(object sender, RoutedEventArgs e)
        {
            _vm.CameraTemplate = "http://{IpAddress}/Streaming/channels/1/picture";
        }

        private async void ForceUpload_Click(object sender, RoutedEventArgs e)
        {
            await ForceUploadAsync();
        }
    }
}
