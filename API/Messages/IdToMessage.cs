using System.Collections.Concurrent;
using System.Text;
using API.Interviews;

namespace API.Messages;

public class IdToMessage
{
    public ConcurrentDictionary<int,CachedMessageAndResume> map = new ConcurrentDictionary<int, CachedMessageAndResume>();
    
    public string ConvertMessagesToString(List<Message> messages)
    {
        StringBuilder builder = new StringBuilder();

        foreach (Message message in messages)
        {
            string sender = message.FromAI ? "AI: " :  "User: ";
            string messageAsString = sender + message.Content;
            
           
            builder.AppendLine( messageAsString );
        }
        return builder.ToString();
    }

}