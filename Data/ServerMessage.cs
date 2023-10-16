using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public enum ServerMessage
    {
        None,
        UsersCollection,
        AddUser,
        RemoveUser,
        Message,
        BreakConnection,
        Broadcast,
        WrongUsername
    }
}
