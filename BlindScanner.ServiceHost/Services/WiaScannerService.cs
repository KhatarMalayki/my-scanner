using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlindScanner.Core.Models;
using BlindScanner.Core.Services;

namespace BlindScanner.ServiceHost.Services
{
    public class WiaScannerService : IScannerService
    {
        private readonly List<ScannerDevice> _cachedDevices;
        private readonly Dictionary<Guid, ScanJob> _jobs;
        private readonly Dictionary<string, bool> _deviceBusyStatus;
        private readonly SemaphoreSlim _deviceLock;
        private readonly AppSettings _settings;

        // WIA COM type
        private static readonly Type WiaDeviceManagerType = Type.GetTypeFromProgID("WIA.DeviceManager");
        private static readonly Type WiaCommonDialogType = Type.GetTypeFromProgID("WIA.CommonDialog");

        public WiaScannerService(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cachedDevices = new List<ScannerDevice>();
            _jobs = new Dictionary<Guid, ScanJob>();
            _deviceBusyStatus = new Dictionary<string, bool>();
            _deviceLock = new SemaphoreSlim(1, 1);
        }

        public async Task<List<ScannerDevice>> GetAvailableDevicesAsync()
        {
            await RefreshDevicesAsync();
            return _cachedDevices.ToList();
        }

        public async Task<ScannerDevice> GetDeviceByIdAsync(string deviceId)
        {
            await RefreshDevicesAsync();
            return _cachedDevices.FirstOrDefault(d => d.Id == deviceId);
        }

        public async Task RefreshDevicesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    _cachedDevices.Clear();

                    if (WiaDeviceManagerType == null)
                    {
                        Console.WriteLine("WIA is not available on this system");
                        return;
                    }

                    dynamic deviceManager = Activator.CreateInstance(WiaDeviceManagerType);

                    try
                    {
                        var deviceInfos = deviceManager.DeviceInfos;
                        int count = deviceInfos.Count;

                        for (int i = 1; i <= count; i++)
                        {
                            try
                            {
                                var deviceInfo = deviceInfos[i];

                                // Filter only scanner devices (Type 1 = Scanner)
                                if (deviceInfo.Type == 1)
                                {
                                    string deviceId = GetPropertyValue(deviceInfo.Properties, "DeviceID") ?? $"scanner_{i}";
                                    string name = GetPropertyValue(deviceInfo.Properties, "Name") ?? "Unknown Scanner";
                                    string manufacturer = GetPropertyValue(deviceInfo.Properties, "Manufacturer") ?? "Unknown";
                                    string description = GetPropertyValue(deviceInfo.Properties, "Description") ?? "Unknown Model";

                                    var scanner = new ScannerDevice
                                    {
                                        Id = deviceId,
                                        Name = name,
                                        Manufacturer = manufacturer,
                                        Model = description,
                                        Status = ScannerStatus.Available,
                                        ConnectionType = "WIA",
                                        LastSeenAt = DateTime.Now,
                                        SupportsDuplex = false,
                                        SupportsColor = true
                                    };

                                    if (_deviceBusyStatus.ContainsKey(scanner.Id) && _deviceBusyStatus[scanner.Id])
                                    {
                                        scanner.Status = ScannerStatus.Busy;
                                    }

                                    _cachedDevices.Add(scanner);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading device {i}: {ex.Message}");
                            }
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(deviceManager);
                    }
                }
                catch (COMException ex)
                {
                    Console.WriteLine($"COM Error refreshing devices: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing devices: {ex.Message}");
                }
            });
        }

        public async Task<ScanJob> StartScanAsync(string deviceId, ScanJob jobSettings)
        {
            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

            if (jobSettings == null)
                throw new ArgumentNullException(nameof(jobSettings));

            // Check if device is busy
            if (await IsDeviceBusyAsync(deviceId))
            {
                jobSettings.Status = ScanJobStatus.Failed;
                jobSettings.ErrorMessage = "Device is currently busy";
                return jobSettings;
            }

            // Add job to tracking
            _jobs[jobSettings.Id] = jobSettings;
            jobSettings.DeviceId = deviceId;
            jobSettings.StartedAt = DateTime.Now;

            // Mark device as busy
            _deviceBusyStatus[deviceId] = true;

            // Start scan in background
            _ = Task.Run(async () => await PerformScanAsync(deviceId, jobSettings));

            return jobSettings;
        }

        private async Task PerformScanAsync(string deviceId, ScanJob job)
        {
            try
            {
                job.Status = ScanJobStatus.Scanning;

                await Task.Run(() =>
                {
                    object device = null;
                    object scannerItem = null;

                    try
                    {
                        if (WiaDeviceManagerType == null)
                        {
                            throw new Exception("WIA is not available on this system");
                        }

                        dynamic deviceManager = Activator.CreateInstance(WiaDeviceManagerType);

                        try
                        {
                            var deviceInfos = deviceManager.DeviceInfos;
                            int count = deviceInfos.Count;

                            // Find the device
                            for (int i = 1; i <= count; i++)
                            {
                                var deviceInfo = deviceInfos[i];
                                string currentDeviceId = GetPropertyValue(deviceInfo.Properties, "DeviceID");

                                if (currentDeviceId == deviceId)
                                {
                                    device = deviceInfo.Connect();
                                    break;
                                }
                            }

                            if (device == null)
                            {
                                throw new Exception("Device not found");
                            }

                            // Get scanner item (usually the first item)
                            dynamic deviceDynamic = device;
                            var items = deviceDynamic.Items;

                            if (items.Count == 0)
                            {
                                throw new Exception("No scanner items found");
                            }

                            scannerItem = items[1];

                            // Set scan properties
                            SetScanProperties(scannerItem, job);

                            // Create output directory
                            string jobFolder = Path.Combine(_settings.OutputFolder, job.Id.ToString());
                            Directory.CreateDirectory(jobFolder);

                            // Perform scan
                            dynamic scannerItemDynamic = scannerItem;
                            const string wiaFormatJPEG = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
                            dynamic image = scannerItemDynamic.Transfer(wiaFormatJPEG);

                            // Save image
                            string imagePath = Path.Combine(jobFolder, $"page_001.jpg");
                            if (File.Exists(imagePath))
                                File.Delete(imagePath);

                            image.SaveFile(imagePath);
                            job.PageCount = 1;

                            job.Status = ScanJobStatus.Processing;
                            job.OutputPath = imagePath;

                            // Copy to shared folder if configured
                            if (_settings.AutoSaveToSharedFolder && !string.IsNullOrEmpty(_settings.SharedFolder))
                            {
                                try
                                {
                                    string sharedPath = Path.Combine(_settings.SharedFolder, Path.GetFileName(imagePath));
                                    File.Copy(imagePath, sharedPath, true);
                                    Console.WriteLine($"File copied to shared folder: {sharedPath}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Warning: Could not copy to shared folder: {ex.Message}");
                                }
                            }

                            job.Status = ScanJobStatus.Completed;
                            job.CompletedAt = DateTime.Now;

                            if (image != null) Marshal.ReleaseComObject(image);
                        }
                        finally
                        {
                            if (deviceManager != null) Marshal.ReleaseComObject(deviceManager);
                        }
                    }
                    catch (COMException ex)
                    {
                        job.Status = ScanJobStatus.Failed;
                        job.ErrorMessage = $"WIA COM Error: {ex.Message} (0x{ex.ErrorCode:X})";
                        job.CompletedAt = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        job.Status = ScanJobStatus.Failed;
                        job.ErrorMessage = $"Scan error: {ex.Message}";
                        job.CompletedAt = DateTime.Now;
                    }
                    finally
                    {
                        if (scannerItem != null) Marshal.ReleaseComObject(scannerItem);
                        if (device != null) Marshal.ReleaseComObject(device);
                    }
                });
            }
            finally
            {
                // Mark device as available
                _deviceBusyStatus[deviceId] = false;
            }
        }

        private void SetScanProperties(object scannerItem, ScanJob job)
        {
            try
            {
                dynamic item = scannerItem;
                var properties = item.Properties;

                // Set horizontal resolution (DPI)
                SetWiaProperty(properties, 6147, job.Dpi); // WIA_IPS_XRES

                // Set vertical resolution (DPI)
                SetWiaProperty(properties, 6148, job.Dpi); // WIA_IPS_YRES

                // Set color mode
                // 1 = Black and White, 2 = Grayscale, 4 = Color
                int colorMode = job.ColorMode ? 4 : 2;
                SetWiaProperty(properties, 4103, colorMode); // WIA_IPA_DATATYPE

                // Set scan intent for quality
                // 0 = Color, 1 = Grayscale, 2 = Text (B&W)
                SetWiaProperty(properties, 6146, job.ColorMode ? 0 : 1); // WIA_IPS_CUR_INTENT
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not set some scan properties: {ex.Message}");
            }
        }

        private void SetWiaProperty(object properties, int propertyId, int value)
        {
            try
            {
                dynamic props = properties;
                int count = props.Count;

                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        var prop = props[i];
                        if (prop.PropertyID == propertyId)
                        {
                            prop.Value = value;
                            return;
                        }
                    }
                    catch
                    {
                        // Property might be read-only or not supported
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not set property {propertyId}: {ex.Message}");
            }
        }

        private string GetPropertyValue(object properties, string propertyName)
        {
            try
            {
                dynamic props = properties;
                int count = props.Count;

                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        var prop = props[i];
                        if (prop.Name == propertyName)
                        {
                            return prop.Value?.ToString();
                        }
                    }
                    catch
                    {
                        // Continue to next property
                    }
                }
            }
            catch
            {
                // Return null if any error
            }
            return null;
        }

        public async Task<bool> CancelScanAsync(Guid jobId)
        {
            await Task.CompletedTask;

            if (_jobs.TryGetValue(jobId, out var job))
            {
                if (job.Status == ScanJobStatus.Scanning || job.Status == ScanJobStatus.Processing)
                {
                    job.Status = ScanJobStatus.Failed;
                    job.ErrorMessage = "Cancelled by user";
                    job.CompletedAt = DateTime.Now;

                    // Free up the device
                    if (!string.IsNullOrEmpty(job.DeviceId))
                    {
                        _deviceBusyStatus[job.DeviceId] = false;
                    }

                    return true;
                }
            }

            return false;
        }

        public async Task<ScanJob> GetJobStatusAsync(Guid jobId)
        {
            await Task.CompletedTask;
            return _jobs.TryGetValue(jobId, out var job) ? job : null;
        }

        public async Task<List<ScanJob>> GetAllJobsAsync()
        {
            await Task.CompletedTask;
            return _jobs.Values.OrderByDescending(j => j.CreatedAt).ToList();
        }

        public async Task<bool> IsDeviceBusyAsync(string deviceId)
        {
            await Task.CompletedTask;
            return _deviceBusyStatus.TryGetValue(deviceId, out var busy) && busy;
        }
    }
}
