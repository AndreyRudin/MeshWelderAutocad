using Autodesk.AutoCAD.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Settings
{
    internal class RebarDiameterColor
    {
        public double Diameter { get; set; } = 10;
        public Color Color { get; set; } = new Color();
        public RebarDiameterColor(double diameter, byte red, byte green, byte blue)
        {
            Diameter = diameter;
            Color = new Color(red, green, blue);
        }
    }
}
