using System.Collections.Concurrent;
using System.Text;

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
            builder.AppendLine( sender  +  message.Content);
        }
        return builder.ToString();
    }

}