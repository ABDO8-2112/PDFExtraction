namespace PDFExtraction.Models
{
    public class PdfExtractionResponse
    {
        public ResponseData Response { get; set; }
        public string PdfFileName { get; set; }
    }

    public class ResponseData
    {
        public string Book { get; set; }
        public string Subject { get; set; }
        public List<Chapter> Chapters { get; set; } = new();
    }

    public class Chapter
    {
        public string ChapterName { get; set; }
        public List<Topic> Topics { get; set; } = new();
        public List<Exercise> Exercises { get; set; } = new();
    }

    public class Topic
    {
        public string TopicName { get; set; }
        public List<ImageUrl> ImageUrls { get; set; } = new();
        public List<Section> Sections { get; set; } = new();
        public List<Exercise> Exercises { get; set; } = new();
    }

    public class Section
    {
        public string SectionName { get; set; }
        public string Content { get; set; }
        public List<ImageUrl> ImageUrls { get; set; } = new();
    }

    public class Exercise
    {
        public string ExerciseName { get; set; }
        public string Content { get; set; }
        public List<ImageUrl> ImageUrls { get; set; } = new();
    }

    public class ImageUrl
    {
        public string Img { get; set; }
    }
}