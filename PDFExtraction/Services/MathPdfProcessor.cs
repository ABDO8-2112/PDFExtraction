using Docnet.Core.Models;
using Docnet.Core;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PDFExtraction.Models;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text;

namespace PDFExtraction.Services
{
    public class MathPdfProcessor
    {
        private readonly ILogger<MathPdfProcessor> _logger;
        private readonly IWebHostEnvironment _environment;

        public MathPdfProcessor(ILogger<MathPdfProcessor> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task<PdfExtractionResponse> ProcessPdfAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Uploaded file is empty or null");

            var response = new PdfExtractionResponse
            {
                Response = new Models.ResponseData
                {
                    Book = null,
                    Subject = null,
                    Chapters = new List<Models.Chapter>()
                },
                PdfFileName = file.FileName
            };

            var uploadsPath = Path.Combine(_environment.WebRootPath, "pdf_data");
            Directory.CreateDirectory(uploadsPath);

            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var savedPdfPath = Path.Combine(uploadsPath, $"{fileName}_{Guid.NewGuid()}.pdf");

            // Save PDF temporarily
            using (var stream = new FileStream(savedPdfPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imagesFolder = Path.Combine(uploadsPath, fileName);
            Directory.CreateDirectory(imagesFolder);

            using var pdfReader = new PdfReader(savedPdfPath);
            using var pdfDoc = new PdfDocument(pdfReader);
            var totalPages = pdfDoc.GetNumberOfPages();

            var chapter = new Models.Chapter { ChapterName = "Default Chapter" };
            var topic = new Topic { TopicName = "Default Topic" };
            chapter.Topics.Add(topic);
            response.Response.Chapters.Add(chapter);

            var renderedImages = RenderPageAsImage(savedPdfPath, imagesFolder, fileName);
            for (int i = 1; i <= totalPages; i++)
            {
                var page = pdfDoc.GetPage(i);

                var strategy = new SimpleTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);
                text = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text)));

                var section = new Section
                {
                    SectionName = $"Page {i}",
                    Content = text,
                    ImageUrls = new List<ImageUrl>()
                };

                // Match image for this page
                var imageIndex = i - 1;
                if (imageIndex < renderedImages.Count)
                {
                    section.ImageUrls.Add(new ImageUrl { Img = renderedImages[imageIndex] });
                }

                topic.Sections.Add(section);
            }

            return response;
        }

        public List<string> RenderPageAsImage(string pdfPath, string outputDir, string baseFileName)
        {
            var imagePaths = new List<string>();
            using var docReader = DocLib.Instance.GetDocReader(File.ReadAllBytes(pdfPath), new PageDimensions(1080, 1920));
            var pageCount = docReader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                var rawBytes = pageReader.GetImage();
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, bmpData.Scan0, rawBytes.Length);
                bmp.UnlockBits(bmpData);

                var imgPath = Path.Combine(outputDir, $"{baseFileName}_page{i + 1}.jpg");
                bmp.Save(imgPath, ImageFormat.Jpeg);
                imagePaths.Add("/pdf_data/" + Path.GetFileName(outputDir) + "/" + Path.GetFileName(imgPath));
            }

            return imagePaths;
        }
    }
}
