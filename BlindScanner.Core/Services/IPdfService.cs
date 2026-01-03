using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlindScanner.Core.Services
{
    public interface IPdfService
    {
        /// <summary>
        /// Creates a PDF document from a list of image files
        /// </summary>
        Task<string> CreatePdfFromImagesAsync(List<string> imagePaths, string outputPath);

        /// <summary>
        /// Creates a searchable PDF with OCR text overlay
        /// </summary>
        Task<string> CreateSearchablePdfAsync(List<string> imagePaths, List<string> ocrTexts, string outputPath);

        /// <summary>
        /// Adds metadata to an existing PDF
        /// </summary>
        Task AddMetadataAsync(string pdfPath, Dictionary<string, string> metadata);

        /// <summary>
        /// Merges multiple PDF files into one
        /// </summary>
        Task<string> MergePdfsAsync(List<string> pdfPaths, string outputPath);

        /// <summary>
        /// Compresses a PDF file
        /// </summary>
        Task<string> CompressPdfAsync(string pdfPath, string outputPath, int quality = 75);

        /// <summary>
        /// Gets page count of a PDF
        /// </summary>
        Task<int> GetPageCountAsync(string pdfPath);
    }
}
