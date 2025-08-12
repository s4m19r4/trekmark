using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Core
{
    public enum MessageType
    {
        Message,
        System,
        Error,
        Ping
    }

    public class ChatMessage
    {
        public MessageType Type { get; set; } = MessageType.System; 
        public string User { get; set; } = String.Empty;    
        public string Text { get; set; }  = String.Empty;        

        public Guid Id { get; set; } = Guid.Empty;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;    

        public ChatMessage() { }

        public ChatMessage(string text)
        {
            Text = text;
            Type = MessageType.System;
            Timestamp = DateTime.Now;
        }

        public ChatMessage(MessageType type, string text)
           : this(text)
        {            
            Timestamp = DateTime.Now;
        }

        public ChatMessage( string user, string text)
            : this(text)
        {            
            User = user;
            Timestamp = DateTime.Now;
        }

        public ChatMessage(MessageType type, string user, string text)
            : this(user, text)
        {
            Type = type;        
            Timestamp = DateTime.Now;
        }

        public ChatMessage( string user, string text, DateTime timestamp)
           : this( user, text)
        {
           
        }

        public ChatMessage(MessageType type, string user, string text, DateTime timestamp)
            : this(type, user, text)
        {                   
            Timestamp = timestamp;
        }

        public ChatMessage(MessageType type, string user, string text, Guid id, DateTime timestamp)
            :this(type, user, text,timestamp)
        {            
            Id = id;           
        }

        public override string ToString()
        {
            switch (Type)
            { 
            case MessageType.Message:
                    return $"[{Timestamp}] {User}: {Text}"; 
            case MessageType.Error:
                    return $"[{Timestamp}] Error: {Text}";
            case MessageType.Ping:
                    return $"[{Timestamp}] Ping";
                default:return $"[{Timestamp}] System: {Text}";
            }
            
        }
    }
}
