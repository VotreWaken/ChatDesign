using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Serializable]
    public class User
    {
        public string Username { set; get; }
        [NonSerialized]
        private TcpClient client;
        public TcpClient Client
        {
            set => client = value;
            get => client;
        }
        public bool Online { set; get; } = true;

        public override string ToString() => $"{Username}";
    }
}
