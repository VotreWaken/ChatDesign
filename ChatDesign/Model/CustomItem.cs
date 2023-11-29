using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChatDesign.Model
{
    public class CustomItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsGroupChat { get; set; }
        public ImageSource ImagePath { get; set; }
    }
}
