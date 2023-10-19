using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace ChatDesign.Control
{
    public static class ListBoxDoubleClickBehavior
    {
        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommand", typeof(ICommand), typeof(ListBoxDoubleClickBehavior), new PropertyMetadata(null, DoubleClickCommandChanged));

        public static ICommand GetDoubleClickCommand(ListBox listBox)
        {
            return (ICommand)listBox.GetValue(DoubleClickCommandProperty);
        }

        public static void SetDoubleClickCommand(ListBox listBox, ICommand value)
        {
            listBox.SetValue(DoubleClickCommandProperty, value);
        }

        private static void DoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if (e.NewValue is ICommand command)
                {
                    listBox.MouseDoubleClick += (sender, args) =>
                    {
                        if (listBox.SelectedItem != null)
                        {
                            command.Execute(listBox.SelectedItem);
                        }
                    };
                }
            }
        }
    }
}
