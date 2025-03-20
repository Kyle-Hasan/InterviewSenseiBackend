using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace API.PDF;

public interface IFileService
{
    

    
    // get pdf name and file path, guaranteed to be stored locally
    Task<(string FileName, string FilePath)> DownloadPdf(string pdfUrl);

    
    
    
    static async Task<(string FileName, string FilePath)> CreateNewFile(IFormFile file)
    {
        string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine("Uploads",
            fileName);
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            await file.CopyToAsync(stream);
        }
        return (fileName, filePath);
    }
    
    static async Task<string> GetPdfTranscriptAsync(string pdfPath)
    {
        return await Task.FromResult(GetPdfText(pdfPath));
    }

    static string GetPdfText(string pdfPath)
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

    static Task DeleteFileAsync(string filePath)
    {
        return Task.Run(()=>File.Delete(filePath));
    }

}