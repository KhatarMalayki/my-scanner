using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlindScanner.Core.Services;
using Tesseract;

namespace BlindScanner.ServiceHost.Services
{
    public class TesseractOcrService : IOcrService
    {
        private TesseractEngine _engine;
        private string _currentLanguage;
        private string _tessdataPath;
        private readonly object _engineLock = new object();

        public bool IsInitialized => _engine != null;

        public async Task InitializeAsync(string tessdataPath, string language)
        {
            await Task.Run(() =>
            {
                try
                {
                    _tessdataPath = tessdataPath;
                    _currentLanguage = language;

                    // Ensure tessdata directory exists
                    if (!Directory.Exists(tessdataPath))
                    {
                        throw new DirectoryNotFoundException($"Tessdata directory not found: {tessdataPath}");
                    }

                    // Check if language data exists
                    string langFile = Path.Combine(tessdataPath, $"{language}.traineddata");
                    if (!File.Exists(langFile))
                    {
                        throw new FileNotFoundException($"Language data file not found: {langFile}. Please download the '{language}.traineddata' file.");
                    }

                    lock (_engineLock)
                    {
                        _engine?.Dispose();
                        _engine = new TesseractEngine(tessdataPath, language, EngineMode.Default);
                    }

                    Console.WriteLine($"Tesseract OCR initialized with language: {language}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize Tesseract: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<string> RecognizeTextAsync(string imagePath)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("OCR engine is not initialized. Call InitializeAsync first.");

            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            return await Task.Run(() =>
            {
                try
                {
                    lock (_engineLock)
                    {
                        using (var img = Pix.LoadFromFile(imagePath))
                        {
                            using (var page = _engine.Process(img))
                            {
                                string text = page.GetText();
                                float confidence = page.GetMeanConfidence();

                                Console.WriteLine($"OCR completed with confidence: {confidence:P}");

                                return text;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OCR error on {imagePath}: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<List<string>> RecognizeTextBatchAsync(List<string> imagePaths)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("OCR engine is not initialized. Call InitializeAsync first.");

            var results = new List<string>();

            foreach (var imagePath in imagePaths)
            {
                try
                {
                    string text = await RecognizeTextAsync(imagePath);
                    results.Add(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to OCR {imagePath}: {ex.Message}");
                    results.Add($"[OCR Failed: {ex.Message}]");
                }
            }

            return results;
        }

        public async Task<List<string>> GetAvailableLanguagesAsync()
        {
            return await Task.Run(() =>
            {
                var languages = new List<string>();

                if (!Directory.Exists(_tessdataPath))
                {
                    return languages;
                }

                try
                {
                    var trainedDataFiles = Directory.GetFiles(_tessdataPath, "*.traineddata");

                    foreach (var file in trainedDataFiles)
                    {
                        string langCode = Path.GetFileNameWithoutExtension(file);
                        languages.Add(langCode);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning for language files: {ex.Message}");
                }

                return languages.OrderBy(l => l).ToList();
            });
        }

        public async Task SetLanguageAsync(string language)
        {
            if (_currentLanguage == language && IsInitialized)
                return;

            await InitializeAsync(_tessdataPath, language);
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        public void Dispose()
        {
            lock (_engineLock)
            {
                _engine?.Dispose();
                _engine = null;
            }
        }
    }
}
