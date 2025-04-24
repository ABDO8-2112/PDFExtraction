using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PDFExtraction.Models;
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

            for (int i = 1; i <= totalPages; i++)
            {
                var page = pdfDoc.GetPage(i);

                // Extract text
                var strategy = new SimpleTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);
                text = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text)));

                var section = new PDFExtraction.Models.Section
                {
                    SectionName = $"Page {i}",
                    Content = text
                };

                var imageUrls = ExtractImagesFromPage(page, imagesFolder, fileName, i);
                section.ImageUrls.AddRange(imageUrls.Select(path => new ImageUrl { Img = path }));

                topic.Sections.Add(section);
            }

            return response;
        }

        private List<string> ExtractImagesFromPage(PdfPage page, string outputDir, string baseFileName, int pageIndex)
        {
            var imagePaths = new List<string>();
            var renderer = new MyImageRenderListener(outputDir, baseFileName, pageIndex);
            var parser = new PdfCanvasProcessor(renderer);
            parser.ProcessPageContent(page);
            imagePaths.AddRange(renderer.ExtractedImages);
            return imagePaths;
        }
    }
}
