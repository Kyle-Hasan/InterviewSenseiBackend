using API.Base;

namespace API.Interviews;

public class InterviewSearchParams : PaginationParams
{
    public string dateSort{ get; set; } = "";

    public string name { get; set; } = "";
    
    public string nameSort { get; set; } = "";
    
    
}