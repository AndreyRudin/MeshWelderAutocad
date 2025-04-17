using Autodesk.AutoCAD.Colors;
using MeshWelderAutocad.Commands.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

internal class SettingStorage
{
    public List<RebarDiameterColor> RebarDiameterColors { get; set; } = new List<RebarDiameterColor>();
    
    public static SettingStorage CreateDefaultSettings()
    {
        return new SettingStorage
        {
            RebarDiameterColors = new List<RebarDiameterColor>
            {
                new RebarDiameterColor(6, 255, 0, 0),
                new RebarDiameterColor(8, 255, 255, 0),
                new RebarDiameterColor(10, 0, 128, 0),
                new RebarDiameterColor(12, 0, 255, 255),
                new RebarDiameterColor(28, 0, 255, 0)
            }
        };
    }
}
