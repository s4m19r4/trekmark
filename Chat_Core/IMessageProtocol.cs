using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Core
{
    public interface IMessageProtocol
    {
        string Serialize(ChatMessage message);
        ChatMessage Deserialize(string rawMessage);
    }
}
