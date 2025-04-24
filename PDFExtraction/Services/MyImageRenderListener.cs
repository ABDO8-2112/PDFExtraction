using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Drawing.Imaging;
using System.Drawing;
using iText.Kernel.Pdf.Canvas.Parser.Data;

namespace PDFExtraction.Services
{
    public class MyImageRenderListener : IEventListener
    {
        public List<string> ExtractedImages { get; private set; } = new();
        private readonly string _outputDir;
        private readonly string _baseFileName;
        private readonly int _pageIndex;
        private int _imgIndex = 0;

        public MyImageRenderListener(string outputDir, string baseFileName, int pageIndex)
        {
            _outputDir = outputDir;
            _baseFileName = baseFileName;
            _pageIndex = pageIndex;
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type == EventType.RENDER_IMAGE)
            {
                var renderInfo = (ImageRenderInfo)data;
                try
                {
                    var imageObject = renderInfo.GetImage();
                    if (imageObject == null) return;

                    using var ms = new MemoryStream(imageObject.GetImageBytes());
                    using var img = Image.FromStream(ms);

                    var imgFileName = $"{_baseFileName}_page{_pageIndex}_img{_imgIndex++}.jpg";
                    var imgPath = Path.Combine(_outputDir, imgFileName);
                    img.Save(imgPath, ImageFormat.Jpeg);

                    var relativePath = Path.Combine("pdf_data", _baseFileName, imgFileName).Replace("\\", "/");
                    ExtractedImages.Add("/" + relativePath);
                }
                catch { }
            }
        }

        public ICollection<EventType> GetSupportedEvents()
        {
            return new HashSet<EventType> { EventType.RENDER_IMAGE };
        }
    }
}
