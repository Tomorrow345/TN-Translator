using H.NotifyIcon.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TN_Translator
{
    /// <summary>
    /// SetApiPopup.xaml 的交互逻辑
    /// </summary>
    public partial class SetApiPopup : Window
    {
        public SetApiPopup()
        {
            InitializeComponent();

            CloseButton.Click += async (s, e) => CloseWindow();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseWindow()
        {
            this.Hide();
        }
    }
}
