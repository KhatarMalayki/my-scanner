using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using BlindScanner.Core.Models;
using BlindScanner.Core.Services;
using BlindScanner.ServiceHost.Services;

namespace BlindScanner.ServiceHost
{
    public class Program
    {
        private static NotifyIcon? _trayIcon;
        private static IHost? _webHost;
        private static AppSettings _settings = new AppSettings();
        private static IScannerService? _scannerService;
        private static IOcrService? _ocrService;
        private static IPdfService? _pdfService;

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load settings
            LoadSettings();

            // Initialize services
            InitializeServices();

            // Start web server
            StartWebServer();

            // Create tray icon
            CreateTrayIcon();

            // Run application
            Application.Run();
        }

        private static void LoadSettings()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                    Console.WriteLine("Settings loaded from appsettings.json");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading settings: {ex.Message}");
                    _settings = new AppSettings();
                }
            }
            else
            {
                _settings = new AppSettings();
                SaveSettings();
            }

            // Ensure directories exist
            Directory.CreateDirectory(_settings.OutputFolder);
            if (!string.IsNullOrEmpty(_settings.SharedFolder))
            {
                try
                {
                    Directory.CreateDirectory(_settings.SharedFolder);
                }
                catch
                {
                    // Shared folder might be network path
                }
            }
        }

        private static void SaveSettings()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
                Console.WriteLine("Settings saved to appsettings.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static void InitializeServices()
        {
            _scannerService = new WiaScannerService(_settings);
            _ocrService = new TesseractOcrService();
            _pdfService = new PdfService();

            // Initialize OCR if enabled
            if (_settings.EnableOcr)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _ocrService.InitializeAsync(_settings.TessdataPath, _settings.DefaultOcrLanguage);
                        Console.WriteLine("OCR service initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: OCR initialization failed: {ex.Message}");
                    }
                });
            }
        }

        private static void StartWebServer()
        {
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Configure services
                builder.Services.AddCors();

                builder.WebHost.UseUrls($"http://localhost:{_settings.WebServerPort}");

                var app = builder.Build();

                // Serve static files from WebUI folder
                string webUIPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebUI");
                if (Directory.Exists(webUIPath))
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(webUIPath),
                        RequestPath = ""
                    });
                }

                // Default route to index.html
                app.MapGet("/", async context =>
                {
                    string indexPath = Path.Combine(webUIPath, "index.html");
                    if (File.Exists(indexPath))
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.SendFileAsync(indexPath);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Web UI not found");
                    }
                });

                // API endpoints
                // Get all devices
                app.MapGet("/api/devices", async (HttpContext context) =>
                {
                    if (_scannerService == null) return;
                    var devices = await _scannerService.GetAvailableDevicesAsync();
                    await context.Response.WriteAsJsonAsync(devices);
                });

                // Refresh devices
                app.MapPost("/api/devices/refresh", async (HttpContext context) =>
                {
                    if (_scannerService == null) return;
                    await _scannerService.RefreshDevicesAsync();
                    var devices = await _scannerService.GetAvailableDevicesAsync();
                    await context.Response.WriteAsJsonAsync(devices);
                });

                // Start scan
                app.MapPost("/api/scan", async (HttpContext context) =>
                {
                    var scanRequest = await context.Request.ReadFromJsonAsync<ScanJob>();
                    if (scanRequest == null || string.IsNullOrEmpty(scanRequest.DeviceId) || _scannerService == null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid scan request" });
                        return;
                    }

                    var job = await _scannerService.StartScanAsync(scanRequest.DeviceId, scanRequest);
                    await context.Response.WriteAsJsonAsync(job);
                });

                // Get all jobs
                app.MapGet("/api/jobs", async (HttpContext context) =>
                {
                    if (_scannerService == null) return;
                    var jobs = await _scannerService.GetAllJobsAsync();
                    await context.Response.WriteAsJsonAsync(jobs);
                });

                // Get job status
                app.MapGet("/api/jobs/{id}", async (HttpContext context) =>
                {
                    if (_scannerService == null) return;
                    var idStr = context.Request.RouteValues["id"]?.ToString();
                    if (Guid.TryParse(idStr, out var jobId))
                    {
                        var job = await _scannerService.GetJobStatusAsync(jobId);
                        if (job != null)
                        {
                            await context.Response.WriteAsJsonAsync(job);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsJsonAsync(new { error = "Job not found" });
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid job ID" });
                    }
                });

                // Cancel job
                app.MapPost("/api/jobs/{id}/cancel", async (HttpContext context) =>
                {
                    if (_scannerService == null) return;
                    var idStr = context.Request.RouteValues["id"]?.ToString();
                    if (Guid.TryParse(idStr, out var jobId))
                    {
                        var success = await _scannerService.CancelScanAsync(jobId);
                        await context.Response.WriteAsJsonAsync(new { success });
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid job ID" });
                    }
                });

                // Get settings
                app.MapGet("/api/settings", async (HttpContext context) =>
                {
                    await context.Response.WriteAsJsonAsync(_settings);
                });

                // Update settings
                app.MapPost("/api/settings", async (HttpContext context) =>
                {
                    var newSettings = await context.Request.ReadFromJsonAsync<AppSettings>();
                    if (newSettings != null)
                    {
                        _settings = newSettings;
                        SaveSettings();
                        await context.Response.WriteAsJsonAsync(new { success = true });
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid settings" });
                    }
                });

                // Get OCR languages
                app.MapGet("/api/ocr/languages", async (HttpContext context) =>
                {
                    if (_ocrService == null) return;
                    var languages = await _ocrService.GetAvailableLanguagesAsync();
                    await context.Response.WriteAsJsonAsync(languages);
                });

                // Download result file
                app.MapGet("/api/download/{jobId}", async (HttpContext context) =>
                {
                    if (_scannerService == null) return;
                    var jobIdStr = context.Request.RouteValues["jobId"]?.ToString();
                    if (Guid.TryParse(jobIdStr, out var jobId))
                    {
                        var job = await _scannerService.GetJobStatusAsync(jobId);
                        if (job != null && !string.IsNullOrEmpty(job.OutputPath) && File.Exists(job.OutputPath))
                        {
                            var fileBytes = await File.ReadAllBytesAsync(job.OutputPath);
                            var fileName = Path.GetFileName(job.OutputPath);
                            context.Response.ContentType = "application/octet-stream";
                            context.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                            await context.Response.Body.WriteAsync(fileBytes);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsJsonAsync(new { error = "File not found" });
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid job ID" });
                    }
                });

                // Start the web host in background
                Task.Run(() =>
                {
                    _webHost = app;
                    app.Run();
                });

                Console.WriteLine($"Web server started on http://localhost:{_settings.WebServerPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start web server: {ex.Message}");
                MessageBox.Show($"Failed to start web server: {ex.Message}", "BlindScanner Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "BlindScanner",
                Visible = true
            };

            // Try to load icon from embedded resource or use default
            try
            {
                _trayIcon.Icon = SystemIcons.Application;
            }
            catch
            {
                _trayIcon.Icon = SystemIcons.Application;
            }

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            var openWebUIItem = new ToolStripMenuItem("Open Web UI");
            openWebUIItem.Click += (s, e) => OpenWebUI();
            contextMenu.Items.Add(openWebUIItem);

            var refreshDevicesItem = new ToolStripMenuItem("Refresh Scanners");
            refreshDevicesItem.Click += async (s, e) =>
            {
                if (_scannerService != null)
                    await _scannerService.RefreshDevicesAsync();
            };
            contextMenu.Items.Add(refreshDevicesItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => OpenSettings();
            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Double-click to open web UI
            _trayIcon.DoubleClick += (s, e) => OpenWebUI();

            // Show balloon tip on startup
            _trayIcon.ShowBalloonTip(3000, "BlindScanner Started",
                $"Web UI available at http://localhost:{_settings.WebServerPort}",
                ToolTipIcon.Info);
        }

        private static void OpenWebUI()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"http://localhost:{_settings.WebServerPort}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open web UI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void OpenSettings()
        {
            MessageBox.Show($"Settings Path: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")}\n\n" +
                $"Output Folder: {_settings.OutputFolder}\n" +
                $"Shared Folder: {_settings.SharedFolder}\n" +
                $"Web Port: {_settings.WebServerPort}\n" +
                $"Default DPI: {_settings.DefaultDpi}\n\n" +
                "Edit appsettings.json to change settings, then restart the application.",
                "BlindScanner Settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ExitApplication()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            if (_webHost != null)
            {
                try
                {
                    Task.Run(async () => await _webHost.StopAsync()).Wait(TimeSpan.FromSeconds(5));
                    _webHost.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping web host: {ex.Message}");
                }
            }

            Application.Exit();
        }
    }
}
