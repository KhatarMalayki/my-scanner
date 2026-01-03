using System;

namespace BlindScanner.Core.Models
{
    public enum ScannerStatus
    {
        Available,
        Busy,
        Offline,
        Error
    }

    public class ScannerDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public ScannerStatus Status { get; set; }
        public bool SupportsDuplex { get; set; }
        public bool SupportsColor { get; set; }
        public int[] SupportedResolutions { get; set; } = Array.Empty<int>();
        public string ConnectionType { get; set; } = string.Empty;
        public DateTime LastSeenAt { get; set; }

        public ScannerDevice()
        {
            Status = ScannerStatus.Available;
            SupportsDuplex = false;
            SupportsColor = true;
            SupportedResolutions = new[] { 150, 200, 300, 600 };
            ConnectionType = "USB";
            LastSeenAt = DateTime.Now;
        }
    }
}
