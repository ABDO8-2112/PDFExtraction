using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Components.Sections;
using System.Text.RegularExpressions;
using System.Text;
using PDFExtraction.Models;
using iText.Kernel.Pdf;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using System.Drawing;

public class MathPdfProcessor
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MathPdfProcessor> _logger;

    public MathPdfProcessor(IWebHostEnvironment env, ILogger<MathPdfProcessor> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<PdfExtractionResult> ProcessMathPdf(IFormFile pdfFile)
    {
        var result = new PdfExtractionResult
        {
            PdfFileName = Path.GetFileNameWithoutExtension(pdfFile.FileName),
            Chapters = new List<ChapterContent>()
        };

        var imagesDir = Path.Combine(_env.WebRootPath, "math-images", result.PdfFileName);
        Directory.CreateDirectory(imagesDir);

        using (var stream = new MemoryStream())
        {
            await pdfFile.CopyToAsync(stream);
            stream.Position = 0;

            using (var pdf = new PdfDocument(new PdfReader(stream)))
            {
                var chapter = new ChapterContent
                {
                    Title = "CIRCLES",
                    Sections = new List<PDFExtraction.Models.SectionContent>(),
                    Figures = new List<FigureContent>()
                };

                // First pass: Extract all figures
                var figureExtractor = new MathFigureExtractor(imagesDir, result.PdfFileName);
                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    var processor = new PdfCanvasProcessor(figureExtractor);
                    processor.ProcessPageContent(pdf.GetPage(i));
                }
                chapter.Figures = figureExtractor.GetFigures();

                // Second pass: Extract and structure content
                var textExtractor = new MathTextExtractor(chapter.Figures);
                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    var pageText = PdfTextExtractor.GetTextFromPage(pdf.GetPage(i), textExtractor);
                    textExtractor.ProcessPageText(pageText, i);
                }

                chapter.Sections = textExtractor.GetSections();
                chapter.Exercises = textExtractor.GetExercises();
                result.Chapters.Add(chapter);
            }
        }

        return result;
    }
}

public class MathFigureExtractor : IEventListener
{
    private readonly string _outputDir;
    private readonly string _pdfName;
    private readonly List<FigureContent> _figures = new List<FigureContent>();
    private int _figureCount = 1;

    public MathFigureExtractor(string outputDir, string pdfName)
    {
        _outputDir = outputDir;
        _pdfName = pdfName;
    }

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type == EventType.RENDER_IMAGE)
        {
            var imageData = (ImageRenderInfo)data;
            var image = imageData.GetImage();
            if (image != null)
            {
                var figureNum = _figureCount++;
                var figureName = $"fig_9.{figureNum}";
                var fileName = $"{figureName}.jpg";
                var filePath = Path.Combine(_outputDir, fileName);

                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        fs.Write(image.GetImageBytes());
                    }

                    _figures.Add(new FigureContent
                    {
                        Reference = figureName,
                        ImagePath = $"/math-images/{_pdfName}/{fileName}"
                    });
                }
                catch (Exception ex)
                {
                    //_logger.LogError(ex, $"Failed to save figure {figureName}");
                }
            }
        }
    }

    public List<FigureContent> GetFigures() => _figures;
    public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_IMAGE };
}

public class MathTextExtractor : ITextExtractionStrategy
{
    private readonly List<FigureContent> _figures;
    private readonly List<PDFExtraction.Models.SectionContent> _sections = new List<PDFExtraction.Models.SectionContent>();
    private readonly List<ExerciseContent> _exercises = new List<ExerciseContent>();
    private StringBuilder _currentText = new StringBuilder();
    private readonly string _outputDir;
    private readonly string _pdfFileName;  // Set via constructor
    private readonly List<ImageUrlDto> _imageUrls = new List<ImageUrlDto>();

    public MathTextExtractor(List<FigureContent> figures)
    {
        _figures = figures;
        _outputDir =  "pdf-images"; // Default fallback
        _pdfFileName =  "unknown";       
        Directory.CreateDirectory(_outputDir); // Ensure exists
    }

    public void ProcessPageText(string pageText, int pageNumber)
    {
        // Process theorems and definitions
        var theoremMatches = Regex.Matches(pageText, @"(Theorem \d+\.\d+:)([\s\S]+?)(?=(Theorem|EXERCISE|$))");
        foreach (Match match in theoremMatches)
        {
            _sections.Add(new PDFExtraction.Models.SectionContent
            {
                Title = match.Groups[1].Value.Trim(),
                Content = match.Groups[2].Value.Trim(),
                RelatedFigures = FindFiguresInText(match.Value)
            });
        }

        // Process exercises
        var exerciseMatches = Regex.Matches(pageText, @"(EXERCISE \d+\.\d+)([\s\S]+?)(?=(EXERCISE|Theorem|$))");
        foreach (Match match in exerciseMatches)
        {
            _exercises.Add(new ExerciseContent
            {
                Title = match.Groups[1].Value.Trim(),
                Problems = match.Groups[2].Value.Trim(),
                RelatedFigures = FindFiguresInText(match.Value)
            });
        }
    }

    private List<FigureContent> FindFiguresInText(string text)
    {
        var figureRefs = Regex.Matches(text, @"Fig\.\s?\d+\.\d+")
            .Select(m => m.Value.Replace(" ", ""));
        return _figures.Where(f => figureRefs.Contains(f.Reference)).ToList();
    }

    // Required interface implementation
    public void RenderText(TextRenderInfo renderInfo) => _currentText.Append(renderInfo.GetText());
    public string GetResultantText() => _currentText.ToString();

    public List<PDFExtraction.Models.SectionContent> GetSections() => _sections;
    public List<ExerciseContent> GetExercises() => _exercises;

    public void EventOccurred(IEventData data, EventType type)
    {
        // Only process image rendering events
        if (type != EventType.RENDER_IMAGE) return;

        var renderInfo = (ImageRenderInfo)data;
        var imageObject = renderInfo.GetImage();
        if (imageObject == null) return;

        try
        {
            // Get raw image bytes
            var imageBytes = imageObject.GetImageBytes();
            if (imageBytes == null || imageBytes.Length == 0) return;

            // Generate filename using figure references when available
            var figureRef = GetFigureReference(renderInfo);
            var imageName = string.IsNullOrEmpty(figureRef)
                ? $"{Guid.NewGuid()}.jpg"
                : $"{figureRef}.jpg";

            var imagePath = Path.Combine(_outputDir, imageName);

            // Convert and save as JPEG using System.Drawing
            using (var ms = new MemoryStream(imageBytes))
            using (var image = Image.FromStream(ms))
            {
                image.Save(imagePath, ImageFormat.Jpeg);
            }

            // Add to collected images
            _imageUrls.Add(new ImageUrlDto
            {
                Img = $"/pdf-images/{_pdfFileName}/{imageName}",
                Reference = figureRef
            });
        }
        catch (Exception ex)
        {
            //_logger?.LogError(ex, "Failed to process PDF image");
        }
    }

    public ICollection<EventType> GetSupportedEvents()
    {
        // Only subscribe to image render events
        return new HashSet<EventType> { EventType.RENDER_IMAGE };
    }

    // Helper method to extract figure references (e.g. "Fig.9.1")
    private string GetFigureReference(ImageRenderInfo renderInfo)
    {
        try
        {
            // Get the image's XObject name
            var xObjectName = renderInfo.GetImage().GetPdfObject().GetAsName(PdfName.Name);
            if (xObjectName != null)
            {
                var name = xObjectName.GetValue();
                if (name.StartsWith("Fig") || name.StartsWith("Figure"))
                {
                    return name.Replace(" ", "").Replace(".", "_");
                }
            }
        }
        catch { /* Ignore extraction errors */ }
        return null;
    }
}