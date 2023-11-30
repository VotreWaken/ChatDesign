using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Serializable]
    public class UserInfo
    {
        public byte[] ImageBytes { set; get; }
        public string Username { set; get; }


    }
}
