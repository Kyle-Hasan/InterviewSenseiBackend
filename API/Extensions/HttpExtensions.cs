using API.Base;



using System.Text.Json;



namespace API.Extensions
{
    public static class HttpExtensions
    {
        public static void AddPaginationHeader(this HttpResponse response, int total, int startIndex, int size)
        {
            var paginationHeader = new PaginationHeader
            {
                total = total,
                startIndex = startIndex,
                size = size
            };

            var jsonOptions = new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
            response.Headers.Append("Pagination",JsonSerializer.Serialize(paginationHeader,jsonOptions));
            response.Headers.Append("Access-Control-Expose-Headers","Pagination");

        }
    }
};

public class HttpExtensions
{
    
}