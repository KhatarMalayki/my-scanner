using System;

namespace BlindScanner.Core.Models
{
    public class AppSettings
    {
        public string OutputFolder { get; set; }
        public string SharedFolder { get; set; }
        public string TessdataPath { get; set; }
        public string DefaultOcrLanguage { get; set; }
        public int WebServerPort { get; set; }
        public int DefaultDpi { get; set; }
        public bool AutoSaveToSharedFolder { get; set; }
        public bool EnableOcr { get; set; }
        public bool EnableTextToSpeech { get; set; }
        public int MaxConcurrentJobs { get; set; }

        public AppSettings()
        {
            OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BlindScanner", "Output");
            SharedFolder = string.Empty;
            TessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            DefaultOcrLanguage = "eng";
            WebServerPort = 8080;
            DefaultDpi = 300;
            AutoSaveToSharedFolder = false;
            EnableOcr = true;
            EnableTextToSpeech = false;
            MaxConcurrentJobs = 3;
        }
    }
}
