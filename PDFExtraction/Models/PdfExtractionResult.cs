namespace PDFExtraction.Models
{
    public class PdfExtractionResult
    {
        public string PdfFileName { get; set; }
        public List<ChapterContent> Chapters { get; set; }
    }

    public class ChapterContent
    {
        public string Title { get; set; }
        public List<SectionContent> Sections { get; set; }
        public List<ExerciseContent> Exercises { get; set; }
        public List<FigureContent> Figures { get; set; }
    }

    public class SectionContent
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public List<FigureContent> RelatedFigures { get; set; }
    }

    public class ExerciseContent
    {
        public string Title { get; set; }
        public string Problems { get; set; }
        public List<FigureContent> RelatedFigures { get; set; }
    }

    public class FigureContent
    {
        public string Reference { get; set; }  // e.g. "fig_9.1"
        public string ImagePath { get; set; }  // e.g. "/math-images/filename/fig_9.1.jpg"
    }
    public class ImageUrlDto
    {
        public string Img { get; set; }  
        public string Reference { get; set; } // Original PDF figure reference (e.g., "Fig.9.1")
    }
}
