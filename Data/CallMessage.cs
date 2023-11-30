using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Serializable]
    public class CallMessage : Message
    {
        public List<UserInfo> Participants { set; get; } = new List<UserInfo>();

        public CallMessage(User sender, string messageString, List<UserInfo> participants)
        {
            ServerMessage = ServerMessage.CallMessage;
            Sender = sender;
            MessageString = messageString;
            Participants = participants;
        }
    }
}
