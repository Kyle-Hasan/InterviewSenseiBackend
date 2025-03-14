using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace API.PDF;

public class PDFService: IPDFService
{
    public async Task<string> GetPdfTranscriptAsync(string pdfPath)
    {
        return await Task.FromResult(GetPdfText(pdfPath));
    }

    public string GetPdfText(string pdfPath)
    {
        using (PdfReader reader = new PdfReader(pdfPath))
        using (PdfDocument pdfDoc = new PdfDocument(reader))
        {
            int pageCount = pdfDoc.GetNumberOfPages();
            StringBuilder result = new StringBuilder();

            for (int i = 1; i <= pageCount; i++) // Ensure all pages are included
            {
                result.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
            }

            return result.ToString();
        }
    }
}