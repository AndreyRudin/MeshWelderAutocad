using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Settings
{
    internal class Color
    {
        public byte Red { get; set; } = 100;
        public byte Green { get; set; } = 100;
        public byte Blue { get; set; } = 100;
        public Color()
        {
            
        }
        public Color(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }
    }
}
