namespace PDFExtraction.Models
{
    public class PdfExtractionData
    {
        public int Id { get; set; }
        public string PdfFileName { get; set; }
        public string JsonContent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
