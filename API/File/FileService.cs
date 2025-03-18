using System.Text;
using API.AWS;
using API.Extensions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace API.PDF;

public class FileService(IBlobStorageService blobStorageService): IFileService 
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
    
    
  
    public async Task<(string FileName, string FilePath)> DownloadPdf(string pdfUrl)
    {
        string fileName = "";
        string filePath = "";

        if (!AppConfig.UseCloudStorage)
        {
            fileName = pdfUrl.GetStringAfterPattern("/api/Interview/getPdf/");
        }
        else
        {
            fileName = pdfUrl.GetStringAfterPattern("/resumes/");
            fileName = fileName.GetStringBeforePattern("?");
        }

        filePath = Path.Combine("Uploads", fileName);

        // download onto local file system if cloud storage is being used
        if (AppConfig.UseCloudStorage)
        {
            await blobStorageService.DownloadFileAsync(fileName, filePath, "resumes");
        }

        return (fileName, filePath);
    }

    public async Task<(string FileName, string FilePath)> CreateNewFile(IFormFile file)
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

}