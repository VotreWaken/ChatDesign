using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatDesign.Model
{
    public class ChatItem : INotifyPropertyChanged
    {
        private bool _isSender;

        public bool IsSender
        {
            get { return _isSender; }
            set
            {
                if (_isSender != value)
                {
                    _isSender = value;
                    OnPropertyChanged("IsSender");
                }
            }
        }
        public string Content { get; set; }

        public string Sender { get; set; }

        public string Date { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
