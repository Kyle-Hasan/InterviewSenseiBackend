using API.Base;

namespace API.Interviews;

public class PagedInterviewResponse : PagedResponse
{
    public List<Interview> interviews;
}