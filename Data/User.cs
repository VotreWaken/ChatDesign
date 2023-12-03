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

        public byte[] ImageBytes { set; get; }
        public byte[] GetUserImageBytes()
        {
            // Ваша логика для получения байтов изображения пользователя.
            // Замените этот комментарий реальной логикой.
            // Например, если у вас есть свойство ImageBytes, возвращайте его:
            return this.ImageBytes;
        }

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
    }
    public class UserEventArgs : EventArgs
    {
        public User ConnectedUser { get; set; }
    }
}
