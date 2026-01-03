using System;

namespace BlindScanner.Core.Models
{
    public enum ScanJobStatus
    {
        Queued,
        Scanning,
        Processing,
        Completed,
        Failed
    }

    public class ScanJob
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public ScanJobStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int PageCount { get; set; }
        public string? OutputPath { get; set; }
        public string? ErrorMessage { get; set; }
        public int Dpi { get; set; }
        public bool ColorMode { get; set; }
        public bool Duplex { get; set; }
        public string OcrLanguage { get; set; } = "eng";

        public ScanJob()
        {
            Id = Guid.NewGuid();
            Status = ScanJobStatus.Queued;
            CreatedAt = DateTime.Now;
            Dpi = 300;
            ColorMode = true;
            Duplex = false;
            PageCount = 0;
        }
    }
}
