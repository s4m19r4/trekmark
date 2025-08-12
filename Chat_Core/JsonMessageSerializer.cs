using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chat_Core
{
    public class JsonMessageSerializer : IMessageProtocol
    {
        public string Serialize(ChatMessage message)
        {
            return JsonSerializer.Serialize(message);
        }

        public ChatMessage Deserialize(string rawMessage)
        {
            return JsonSerializer.Deserialize<ChatMessage>(rawMessage);
        }
    }
}
