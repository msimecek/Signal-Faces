using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SignalClient.ViewModels
{
    public class MainPageVM : INotifyPropertyChanged
    {
        private string ipAddress;

        public string IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; OnPropertyChanged(); }
        }

        private string cameraTemplate;

        public string CameraTemplate
        {
            get { return cameraTemplate; }
            set { cameraTemplate = value; OnPropertyChanged(); }
        }

        private string admin;

        public string Admin
        {
            get { return admin; }
            set { admin = value; OnPropertyChanged(); }
        }

        private string pass;

        public string Pass
        {
            get { return pass; }
            set { pass = value; OnPropertyChanged(); }
        }

        private int interval;

        public int Interval
        {
            get { return interval; }
            set { interval = value; OnPropertyChanged(); }
        }

        private int delay;

        public int Delay
        {
            get { return delay; }
            set { delay = value; OnPropertyChanged(); }
        }


        private bool uploadEnabled;

        public bool UploadEnabled
        {
            get { return uploadEnabled; }
            set { uploadEnabled = value; OnPropertyChanged(); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        }
    }
}
