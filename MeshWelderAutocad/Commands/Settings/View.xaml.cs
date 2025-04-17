using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MeshWelderAutocad.Commands.Settings
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class View : Window
    {
        public View()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void ColorTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;

            string proposedText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            if (int.TryParse(proposedText, out int value))
            {
                e.Handled = value < 0 || value > 255;
            }
            else
            {
                e.Handled = true;
            }
        }

        //private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    if (int.TryParse(textBox.Text, out int value))
        //    {
        //        if (value < 0 || value > 255)
        //        {
        //            MessageBox.Show("Введите значение от 0 до 255");
        //            textBox.Text = "255";
        //            textBox.SelectionStart = textBox.Text.Length;
        //        }
        //    }
        //}
        private void ColorTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pasteText = e.DataObject.GetData(typeof(string)) as string;
                var textBox = sender as TextBox;

                string proposedText = textBox.Text.Insert(textBox.SelectionStart, pasteText);

                if (!int.TryParse(proposedText, out int value) || value < 0 || value > 255)
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
        private void ColorTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox.Tag = textBox.Text;
        }

        private void ColorTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag?.ToString() ?? "0";
            }
        }
    }
}
