using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatDesign.Model
{
    public class Chat
    {
        public int ID { get; set; }
        public string ChatName { get; set; }
        public bool ChatType { get; set; }
        public byte[] Avatar { get; set; }
        // Add more properties as needed

        // You might also consider adding a List of participants or messages associated with the chat
        // public List<User> Participants { get; set; }
        // public List<Message> Messages { get; set; }
    }
}
