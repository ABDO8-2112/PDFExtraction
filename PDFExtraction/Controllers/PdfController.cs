using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDFExtraction.Data;
using PDFExtraction.Models;

namespace PDFExtraction.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly MathPdfProcessor _pdfService;
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _env;

        public PdfController(MathPdfProcessor pdfService, AppDbContext dbContext, IWebHostEnvironment env)
        {
            _pdfService = pdfService;
            _dbContext = dbContext;
            _env = env;
        }

        [HttpPost("extract")]
        public async Task<IActionResult> ExtractFromPdf([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("No files uploaded.");
            }

            var results = new List<PdfExtractionResult>();

            foreach (var file in files)
            {
                if (file.ContentType != "application/pdf")
                {
                    continue;
                }


                var result = await _pdfService.ProcessMathPdf(file);
                results.Add(result);

                // Save to database
                var dbRecord = new PdfExtractionData
                {
                    PdfFileName = result.PdfFileName,
                    JsonContent = System.Text.Json.JsonSerializer.Serialize(result)
                };
                _dbContext.PdfExtractionData.Add(dbRecord);
            }

            await _dbContext.SaveChangesAsync();

            return Ok(results);
        }

        [HttpGet("results")]
        public async Task<IActionResult> GetAllResults()
        {
            var results = await _dbContext.PdfExtractionData.ToListAsync();
            return Ok(results);
        }
    }
}
