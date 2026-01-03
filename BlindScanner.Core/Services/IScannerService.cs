using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlindScanner.Core.Models;

namespace BlindScanner.Core.Services
{
    public interface IScannerService
    {
        /// <summary>
        /// Gets all available scanner devices
        /// </summary>
        Task<List<ScannerDevice>> GetAvailableDevicesAsync();

        /// <summary>
        /// Gets a specific scanner device by ID
        /// </summary>
        Task<ScannerDevice> GetDeviceByIdAsync(string deviceId);

        /// <summary>
        /// Refreshes the list of available devices
        /// </summary>
        Task RefreshDevicesAsync();

        /// <summary>
        /// Initiates a scan job
        /// </summary>
        Task<ScanJob> StartScanAsync(string deviceId, ScanJob jobSettings);

        /// <summary>
        /// Cancels a running scan job
        /// </summary>
        Task<bool> CancelScanAsync(Guid jobId);

        /// <summary>
        /// Gets the status of a scan job
        /// </summary>
        Task<ScanJob> GetJobStatusAsync(Guid jobId);

        /// <summary>
        /// Gets all scan jobs
        /// </summary>
        Task<List<ScanJob>> GetAllJobsAsync();

        /// <summary>
        /// Checks if a device is currently busy
        /// </summary>
        Task<bool> IsDeviceBusyAsync(string deviceId);
    }
}
