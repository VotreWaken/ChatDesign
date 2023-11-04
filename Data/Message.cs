using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Serializable]
    public class Message
    {
        public User Sender { set; get; }
        public User Reciever { set; get; }
        //public bool Broadcast { set; get; }
        public string MessageString { set; get; }
        //public bool BreakConnection { set; get; }
        // public bool ServerMessage { set; get; }
        public ServerMessage ServerMessage { set; get; } = ServerMessage.None;
        public List<User> Users { set; get; } = new List<User>();

        public string ChatName { set; get; }
    }
}
