using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Printing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TN_Translator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double version = 1.2;

        private string ChooseLang = "";
        private string InputedText = "";
        private string translated = "";
        private string textToTranslate = "";

        private string ApiKey = "";

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9001;

        private string sobPath = "StartOnBoot.txt";
        private string akPath = "ApiKey.txt";

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this.Left = SystemParameters.WorkArea.Width - this.Width;
                this.Top = SystemParameters.WorkArea.Height - this.Height;

                if (Environment.GetCommandLineArgs().Contains("--silent"))
                {
                    this.Hide();
                    TrayIcon.Visibility = Visibility.Visible; 
                }
            };

            SobCheck.Checked -= AutoStartCheckBox_Checked;
            SobCheck.Unchecked -= AutoStartCheckBox_Unchecked;

            if (File.Exists(sobPath))
            {
                string sob = File.ReadAllText(sobPath).Trim();
                SobCheck.IsChecked = (sob == "True");
            }
            if (File.Exists(akPath))
            {
                ApiKey = File.ReadAllText(akPath).Trim();
            }

            SobCheck.Checked += AutoStartCheckBox_Checked;
            SobCheck.Unchecked += AutoStartCheckBox_Unchecked;

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
            akButton.Click += async (s, e) => OpenApiSettings();
            AppInfo.Click += async (s, e) => MessageBox.Show($"TN Translator v{version}\n这是一个轻量且快捷的翻译应用，使用智谱GLM AI进行翻译，徐点击，会在后台运行，如果要彻底关闭此应用，请右键托盘图标，然后点击退出。\n此应用运行时，可按下Ctrl+Alt+T打开窗口，输入或拖入html中的文本后点击翻译按钮即可翻译，点击Esc可关闭窗口，点击复制按钮复制翻译的文本。\n在卸载此应用前，请先结束进程，不然会留下残留文件。", "应用信息");
            CloseButton.Click += async (s, e) => this.Hide();

            this.Closed += (s, e) =>
            {
                var helper = new WindowInteropHelper(this);
                UnregisterHotKey(helper.Handle, HOTKEY_ID);
            };
        }

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableAutoStart();
                File.WriteAllText(sobPath, "True");
            }
            catch (Exception ex)
            {
                MessageBox.Show("勾选自启失败：" + ex.Message);
                SobCheck.IsChecked = false;
            }
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableAutoStart();
                File.WriteAllText(sobPath, "False");
            }
            catch { }
        }

        private void EnableAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    key.SetValue("TN-Translator", $"\"{exePath}\" --silent");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("开启自启失败，请检查权限：" + ex.Message);
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

        private void OpenApiSettings()
        {
            SetApiPopup sap = new SetApiPopup();
            sap.Owner = this;

            System.Diagnostics.Process.Start("notepad.exe", akPath);
            sap.ShowDialog();
        }

        private void ShowWindow(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.Topmost = true;
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

            RegisterHotKey(helper.Handle, HOTKEY_ID, 0x0001, 0x54);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                if (this.IsVisible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.Topmost = true;

                    InputText.Focus();
                    InputText.SelectAll();

                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ChangeChoose(object sender, SelectionChangedEventArgs e)
        {
            if (TransLangList.SelectedItem is ListBoxItem item)
            {
                ChooseLang = item.Content.ToString() ?? "自动";
            }
        }

        private async Task Translate(string text)
        {
            try
            {
                MessageBox.Show(ChooseLang);

                TransText.Text = "...";

                using HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                var requestData = new
                {
                    model = "glm-4-flash", 
                    messages = new[]
                    {
                        new { role = "system", content = $"你是一个顶级的专业翻译引擎。请直接将用户发给你的文本翻译成{ChooseLang}，如果翻译的语言相同，就不用改。\n要求：\n1. 只输出翻译结果，绝对不要包含任何解释、前缀、后缀或礼貌用语。\n2. 如果是邮件，请保持原有的换行格式和礼貌语境。\n3. 如果文本中包含代码块（Markdown格式），请原样保留代码块内容，不要翻译代码。" },
                        new { role = "user", content = text }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://open.bigmodel.cn/api/paas/v4/chat/completions", content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TransText.Text = $"API 报错：{responseString}";
                    return;
                }

                using JsonDocument doc = JsonDocument.Parse(responseString);
                translated = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "翻译失败";

                TransText.Text = translated.Trim();
            }
            catch (Exception ex)
            {
                TransText.Text = $"翻译出错：{ex.Message}";
            }
        }
    }    
}