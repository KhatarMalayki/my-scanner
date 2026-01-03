using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlindScanner.Core.Services;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;

namespace BlindScanner.ServiceHost.Services
{
    public class PdfService : IPdfService
    {
        public async Task<string> CreatePdfFromImagesAsync(List<string> imagePaths, string outputPath)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                throw new ArgumentException("Image paths cannot be null or empty", nameof(imagePaths));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            return await Task.Run(() =>
            {
                try
                {
                    // Ensure output directory exists
                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    using (var document = new PdfDocument())
                    {
                        document.Info.Title = "Scanned Document";
                        document.Info.Creator = "BlindScanner";
                        document.Info.CreationDate = DateTime.Now;

                        foreach (var imagePath in imagePaths)
                        {
                            if (!File.Exists(imagePath))
                            {
                                Console.WriteLine($"Warning: Image not found: {imagePath}");
                                continue;
                            }

                            try
                            {
                                // Add a new page
                                var page = document.AddPage();

                                using (var image = XImage.FromFile(imagePath))
                                {
                                    // Set page size to match image aspect ratio
                                    double aspectRatio = (double)image.PixelWidth / image.PixelHeight;

                                    if (aspectRatio > 1)
                                    {
                                        // Landscape
                                        page.Width = XUnit.FromPoint(842); // A4 landscape width
                                        page.Height = XUnit.FromPoint(595); // A4 landscape height
                                    }
                                    else
                                    {
                                        // Portrait
                                        page.Width = XUnit.FromPoint(595); // A4 portrait width
                                        page.Height = XUnit.FromPoint(842); // A4 portrait height
                                    }

                                    using (var gfx = XGraphics.FromPdfPage(page))
                                    {
                                        // Draw image to fit the page
                                        gfx.DrawImage(image, 0, 0, page.Width, page.Height);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error adding image {imagePath} to PDF: {ex.Message}");
                            }
                        }

                        if (document.PageCount == 0)
                        {
                            throw new Exception("No valid images were added to the PDF");
                        }

                        // Save the document
                        document.Save(outputPath);
                        Console.WriteLine($"PDF created successfully: {outputPath} ({document.PageCount} pages)");
                    }

                    return outputPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating PDF: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<string> CreateSearchablePdfAsync(List<string> imagePaths, List<string> ocrTexts, string outputPath)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                throw new ArgumentException("Image paths cannot be null or empty", nameof(imagePaths));

            if (ocrTexts == null || ocrTexts.Count != imagePaths.Count)
                throw new ArgumentException("OCR texts must match the number of images", nameof(ocrTexts));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            return await Task.Run(() =>
            {
                try
                {
                    // Ensure output directory exists
                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    using (var document = new PdfDocument())
                    {
                        document.Info.Title = "Scanned Document (Searchable)";
                        document.Info.Creator = "BlindScanner";
                        document.Info.CreationDate = DateTime.Now;

                        for (int i = 0; i < imagePaths.Count; i++)
                        {
                            var imagePath = imagePaths[i];
                            var ocrText = ocrTexts[i];

                            if (!File.Exists(imagePath))
                            {
                                Console.WriteLine($"Warning: Image not found: {imagePath}");
                                continue;
                            }

                            try
                            {
                                var page = document.AddPage();

                                using (var image = XImage.FromFile(imagePath))
                                {
                                    // Set page size to match image aspect ratio
                                    double aspectRatio = (double)image.PixelWidth / image.PixelHeight;

                                    if (aspectRatio > 1)
                                    {
                                        page.Width = XUnit.FromPoint(842);
                                        page.Height = XUnit.FromPoint(595);
                                    }
                                    else
                                    {
                                        page.Width = XUnit.FromPoint(595);
                                        page.Height = XUnit.FromPoint(842);
                                    }

                                    using (var gfx = XGraphics.FromPdfPage(page))
                                    {
                                        // Draw image
                                        gfx.DrawImage(image, 0, 0, page.Width, page.Height);

                                        // Add OCR text as invisible overlay (simplified approach)
                                        // Note: True searchable PDF would require exact text positioning
                                        if (!string.IsNullOrWhiteSpace(ocrText))
                                        {
                                            var font = new XFont("Arial", 8, XFontStyle.Regular);
                                            var textBrush = new XSolidBrush(XColor.FromArgb(1, 255, 255, 255)); // Nearly transparent

                                            // Draw text at bottom (invisible but searchable)
                                            gfx.DrawString(ocrText, font, textBrush,
                                                new XRect(0, page.Height - 50, page.Width, 50),
                                                XStringFormats.TopLeft);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error adding searchable page {imagePath}: {ex.Message}");
                            }
                        }

                        if (document.PageCount == 0)
                        {
                            throw new Exception("No valid images were added to the searchable PDF");
                        }

                        document.Save(outputPath);
                        Console.WriteLine($"Searchable PDF created: {outputPath} ({document.PageCount} pages)");
                    }

                    return outputPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating searchable PDF: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task AddMetadataAsync(string pdfPath, Dictionary<string, string> metadata)
        {
            if (string.IsNullOrEmpty(pdfPath))
                throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");

            await Task.Run(() =>
            {
                try
                {
                    using (var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify))
                    {
                        foreach (var kvp in metadata)
                        {
                            switch (kvp.Key.ToLower())
                            {
                                case "title":
                                    document.Info.Title = kvp.Value;
                                    break;
                                case "author":
                                    document.Info.Author = kvp.Value;
                                    break;
                                case "subject":
                                    document.Info.Subject = kvp.Value;
                                    break;
                                case "keywords":
                                    document.Info.Keywords = kvp.Value;
                                    break;
                                case "creator":
                                    document.Info.Creator = kvp.Value;
                                    break;
                            }
                        }

                        document.Save(pdfPath);
                    }

                    Console.WriteLine($"Metadata added to PDF: {pdfPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding metadata to PDF: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<string> MergePdfsAsync(List<string> pdfPaths, string outputPath)
        {
            if (pdfPaths == null || pdfPaths.Count == 0)
                throw new ArgumentException("PDF paths cannot be null or empty", nameof(pdfPaths));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            return await Task.Run(() =>
            {
                try
                {
                    using (var outputDocument = new PdfDocument())
                    {
                        foreach (var pdfPath in pdfPaths)
                        {
                            if (!File.Exists(pdfPath))
                            {
                                Console.WriteLine($"Warning: PDF not found: {pdfPath}");
                                continue;
                            }

                            try
                            {
                                using (var inputDocument = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import))
                                {
                                    for (int i = 0; i < inputDocument.PageCount; i++)
                                    {
                                        outputDocument.AddPage(inputDocument.Pages[i]);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error merging PDF {pdfPath}: {ex.Message}");
                            }
                        }

                        if (outputDocument.PageCount == 0)
                        {
                            throw new Exception("No valid PDFs were merged");
                        }

                        outputDocument.Save(outputPath);
                        Console.WriteLine($"PDFs merged successfully: {outputPath} ({outputDocument.PageCount} pages)");
                    }

                    return outputPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error merging PDFs: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<string> CompressPdfAsync(string pdfPath, string outputPath, int quality = 75)
        {
            // PdfSharpCore doesn't have built-in compression beyond what it does automatically
            // For now, just copy the file
            // Advanced compression would require using ImageSharp to recompress images before PDF creation

            await Task.Run(() =>
            {
                if (!File.Exists(pdfPath))
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");

                File.Copy(pdfPath, outputPath, true);
                Console.WriteLine($"PDF copied (compression not implemented): {outputPath}");
            });

            return outputPath;
        }

        public async Task<int> GetPageCountAsync(string pdfPath)
        {
            if (string.IsNullOrEmpty(pdfPath))
                throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");

            return await Task.Run(() =>
            {
                try
                {
                    using (var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.InformationOnly))
                    {
                        return document.PageCount;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting page count: {ex.Message}");
                    throw;
                }
            });
        }
    }
}
