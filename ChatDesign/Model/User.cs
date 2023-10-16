using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChatDesign.Model
{
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public byte[] ImagePath { get; set; }
    }
}
