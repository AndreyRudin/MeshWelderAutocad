using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using MeshWelderAutocad.Commands.Settings.Models;

namespace MeshWelderAutocad.Commands.Settings
{
    internal partial class ViewModel
    {
        public ICommand AddRowReserveCommand { get; }
        public ICommand DeleteRowReserveCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand SaveCommand { get; }

        private Reserve _selectedReserve;
        public Reserve SelectedReserve
        {
            get => _selectedReserve;
            set
            {
                _selectedReserve = value;
                OnPropertyChanged(nameof(SelectedReserve));
            }
        }
        private Action<object> AddRowReserve()
        {
            return _ =>
            {
                if (SelectedReserve == null)
                {
                    Reserves.Add(new Reserve());
                }
                else
                {
                    int indexSelectedReserve = Reserves.IndexOf(SelectedReserve);
                    Reserves.Insert(indexSelectedReserve + 1, new Reserve());
                }
            };
        }

        private Action<object> DeleteRowReserve()
        {
            return (object parameter) =>
            {
                if (parameter is Reserve reserve)
                    Reserves.Remove(reserve);
            };
        }

        private Action<object> Cancel()
        {
            return _ =>
            {
                View.Close();
            };
        }

        private Action<object> Save()
        {
            return _ =>
            {
                SaveSettings();
            };
        }
    }
}
