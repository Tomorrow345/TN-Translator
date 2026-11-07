using System.Net.Http;
using System.Printing;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Reflection;

namespace TN_Translator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ChooseLang = "";
        private string InputedText = "";
        private string translated = "";
        private string textToTranslate = "";

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9001;

        private string sobPath = "StartOnBoot.txt";

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this.Left = SystemParameters.WorkArea.Width - this.Width;
                this.Top = SystemParameters.WorkArea.Height - this.Height;
            };

            if (File.Exists(sobPath))
            {
                string sob = File.ReadAllText(sobPath);

                if (sob == "True")
                {
                    SobCheck.IsChecked = true;
                } else
                {
                    SobCheck.IsChecked = false;
                }
            }

            InputedText = InputText.Text.Trim();

            ToTrans.Click += async (s, e) =>
            {
                textToTranslate = InputText.Text.Trim();
                if (string.IsNullOrEmpty(textToTranslate))
                {
                    MessageBox.Show("请先输入要翻译的内容！");
                    return;
                }
                await Translate(textToTranslate);
            };
            CopyTrans.Click += async (s, e) => CopyTranslateText();
            CleanButton.Click += async (s, e) => CleanText();
            AppInfo.Click += async (s, e) => MessageBox.Show("TN Translator v1.0\n这是一个轻量且快捷的翻译应用，会在后台运行，如果要彻底关闭此应用，请右击窗口左上角图标。\n此应用运行时，可按下Ctrl+Alt+T打开窗口，输入或拖入html中的文本后点击翻译按钮即可翻译，点击Esc可关闭窗口，点击复制按钮复制翻译的文本", "应用信息");
            CloseButton.Click += async (s, e) => this.Hide();

            this.Closed += (s, e) =>
            {
                var helper = new WindowInteropHelper(this);
                UnregisterHotKey(helper.Handle, HOTKEY_ID);
            };
        }

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            EnableAutoStart();
            File.WriteAllText(sobPath, "True");
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableAutoStart();
            File.WriteAllText(sobPath, "False");
        }

        private void EnableAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    string exePath = Assembly.GetExecutingAssembly().Location;

                    key.SetValue("TN-Translator", $"\"{exePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("开启自启失败：" + ex.Message);
            }
        }

        private void DisableAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key.DeleteValue("TN-Translator", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭自启失败：" + ex.Message);
            }
        }

        private void ShowWindow(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            TrayIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void CleanText()
        {
            InputText.Text = "";
            TransText.Text = "";
        }

        private void CopyTranslateText()
        {
            Clipboard.SetText(translated);
            MessageBox.Show("已复制到剪贴板！");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_MouseRightDownIcon(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                this.Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Hide(); 
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            HwndSource.FromHwnd(helper.Handle)?.AddHook(HwndHook);

            RegisterHotKey(helper.Handle, HOTKEY_ID, 0x0002 | 0x0001, 0x54);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                this.Topmost = true;
                this.Topmost = false; 

                InputText.Focus();
                InputText.SelectAll();

                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ChangeChoose(object sender, SelectionChangedEventArgs e)
        {
            if (TransLangList.SelectedItem is ListBoxItem item)
            {
                ChooseLang = item.Content.ToString() ?? "自动";

                if (ChooseLang == "中文")
                {
                    ChooseLang = "zh-CN";
                } else if (ChooseLang == "英文")
                {
                    ChooseLang = "en";
                }
            }
        }

        private async Task Translate(string text)
        {
            try
            {
                TransText.Text = "...";

                string encodedText = Uri.EscapeDataString(text);

                string url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair=Autodetect|{ChooseLang}";

                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);

                translated = doc.RootElement.GetProperty("responseData").GetProperty("translatedText").GetString() ?? "null";

                if (translated != null && translated.Contains("MYMEMORY WARNING"))
                {
                    translated = "翻译额度超限或请求太频繁啦，休息一下！";
                }

                TransText.Text = translated ?? "翻译失败";

            }
            catch (Exception ex)
            {
                MessageBox.Show($"翻译出错：{ex.Message}");
            }
        }
    }
}