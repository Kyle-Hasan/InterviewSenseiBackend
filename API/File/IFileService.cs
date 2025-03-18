namespace API.PDF;

public interface IFileService
{
    Task<string> GetPdfTranscriptAsync(string pdfPath);


    string GetPdfText(string pdfPath);

    
    // get pdf name and file path, guaranteed to be stored locally
    Task<(string FileName, string FilePath)> DownloadPdf(string pdfUrl);

    // upload new pdf
    Task<(string FileName, string FilePath)> CreateNewFile(IFormFile file);

}