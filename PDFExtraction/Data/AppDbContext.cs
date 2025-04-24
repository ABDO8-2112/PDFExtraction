using Microsoft.EntityFrameworkCore;
using PDFExtraction.Models;
namespace PDFExtraction.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PdfExtractionData> PdfExtractionData { get; set; }
    }
}
