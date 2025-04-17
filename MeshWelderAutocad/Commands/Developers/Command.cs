using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Windows;
using MeshWelderAutocad.Commands.Settings;

namespace MeshWelderAutocad.Commands.Developers
{
    public class Command
    {
        [CommandMethod("ShowDevelopers")]
        public static void ShowDevelopers()
        {
            try
            {
                new View().ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна настроек:\n" + ex.Message, "Ошибка");
            }
        }
    }
}
