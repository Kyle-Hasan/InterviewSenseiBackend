namespace API.PDF;

public interface IPDFService
{
    Task<string> GetPdfTranscriptAsync(string pdfPath);


    string GetPdfText(string pdfPath);

}