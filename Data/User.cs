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
    }
}
