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

        // Событие подключения пользователя
        public event EventHandler<UserEventArgs> OnUserConnected;

        // Событие отключения пользователя
        public event EventHandler<UserEventArgs> OnUserDisconnected;

        // Метод для вызова события подключения пользователя
        protected virtual void RaiseUserConnected(User connectedUser)
        {
            OnUserConnected?.Invoke(this, new UserEventArgs { ConnectedUser = connectedUser });
        }

        // Метод для вызова события отключения пользователя
        protected virtual void RaiseUserDisconnected(User connectedUser)
        {
            OnUserDisconnected?.Invoke(this, new UserEventArgs { ConnectedUser = connectedUser });
        }

        // Метод для подключения пользователя
        public void Connect(User connectedUser)
        {
            RaiseUserConnected(connectedUser);
        }

        // Метод для отключения пользователя
        public void Disconnect(User connectedUser)
        {
            RaiseUserDisconnected(connectedUser);
        }

        public class UserEventArgs : EventArgs
        {
            public User ConnectedUser { get; set; }
        }
    }
}
