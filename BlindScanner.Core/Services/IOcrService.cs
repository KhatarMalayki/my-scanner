using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlindScanner.Core.Services
{
    public interface IOcrService
    {
        /// <summary>
        /// Initializes OCR engine with specified language
        /// </summary>
        Task InitializeAsync(string tessdataPath, string language);

        /// <summary>
        /// Performs OCR on an image file
        /// </summary>
        Task<string> RecognizeTextAsync(string imagePath);

        /// <summary>
        /// Performs OCR on multiple image files
        /// </summary>
        Task<List<string>> RecognizeTextBatchAsync(List<string> imagePaths);

        /// <summary>
        /// Gets available OCR languages
        /// </summary>
        Task<List<string>> GetAvailableLanguagesAsync();

        /// <summary>
        /// Sets the OCR language
        /// </summary>
        Task SetLanguageAsync(string language);

        /// <summary>
        /// Gets current OCR language
        /// </summary>
        string GetCurrentLanguage();

        /// <summary>
        /// Checks if OCR engine is initialized
        /// </summary>
        bool IsInitialized { get; }
    }
}
