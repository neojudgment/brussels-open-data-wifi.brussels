// •————————————————————————————————————————————————————————————————————————————————————————————————————————————•
// |                                                                                                            |
// |    brussels open data - wifi.brussels is a proof of concept (PoC). <https://github.com/neojudgment/>       |
// |                                                                                                            |
// |    wifi.brussels is a free Wi-Fi network covering certain zones of the territory of                        |
// |    the territory of the Brussels-Capital Region in Belgium.                                                |
// |                                                                                                            |
// |    brussels open data - wifi.brussels uses Microsoft WindowsAPICodePack and GMap.NET to                    |
// |    demonstrate how to retrieve data from Brussels open data Store.                                         |
// |                                                                                                            |
// |    brussels open data - wifi.brussels uses Elysium library that implements Modern UI for                   |
// |    Windows Presentation Foundation.                                                                        |
// |                                                                                                            |
// |    This program is under Microsoft Public License (Ms-RL)                                                  |
// |                                                                                                            |
// |    This program is distributed in the hope that it will be useful but WITHOUT ANY WARRANTY;                |
// |    without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.               |
// |                                                                                                            |
// |    You should have received a copy of the Microsoft Public License (Ms-RL)                                 |
// |    along with this program.  If not, see <http://opensource.org/licenses/MS-RL>.                           |
// |                                                                                                            |
// |    Copyright © Pascal Hubert - Brussels, Belgium 2017. <mailto:pascal.hubert@outlook.com>                  |
// •————————————————————————————————————————————————————————————————————————————————————————————————————————————•

using Microsoft.VisualBasic;
using OpenData.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenData
{
    /// <summary>
    /// Logique d'interaction pour Option.xaml
    /// </summary>
    public partial class Option
    {
        #region Variables

        public static bool BingMapApiKeyUpdate;
        public static int ComPortDiscovered;
        public static bool ComPortScan;
        public static bool ComSearch;
        public static bool GeolockUpdate;
        public static bool SaveChange;
        public static bool Update;
        private const int ScMove = 0xF010;
        private const int WmSyscommand = 0x112;
        private static SerialPort _spa;
        private bool CacheOptimization;

        #endregion Variables

        #region New

        public Option()
        {
            InitializeComponent();

            Closing += OptionClosing;
            SourceInitialized += WindowsSourceInitialized;

            ShowComports();

            if (Settings.Default.UseProxy)
            {
                CheckBoxProxyAuthentication.IsChecked = true;
            }
            else
            {
                CheckBoxProxyAuthentication.IsChecked = false;
            }

            if (Settings.Default.ShowTileGridLines)
            {
                CheckBoxShowTiles.IsChecked = true;
            }
            else
            {
                CheckBoxShowTiles.IsChecked = false;
            }

            if (Settings.Default.Routing)
            {
                CheckBoxRoute.IsChecked = true;
            }
            else
            {
                CheckBoxRoute.IsChecked = false;
            }

            if (Settings.Default.ProxyPort > 0)
            {
                TextBoxProxyAdress.Text = Settings.Default.ProxyAdress;
                TextBoxProxyPort.Text = Convert.ToString(Settings.Default.ProxyPort);
                TextBoxUsername.Text = Settings.Default.ProxyUsername;
                PasswordBox.Password = Settings.Default.ProxyPassword;
            }

            if (Settings.Default.UseGpsHarware)
            {
                CheckBoxGpsHardware.IsChecked = true;
            }
            else
            {
                CheckBoxGpsHardware.IsChecked = false;
            }

            if (Settings.Default.AutoUpdate)
            {
                CheckBoxUpdates.IsChecked = true;
            }
            else
            {
                CheckBoxUpdates.IsChecked = false;
            }

            if (Settings.Default.DistanceAccuracy)
            {
                ComboBoxAccuracy.Text = "Online";
            }
            else
            {
                ComboBoxAccuracy.Text = "Offline (default)";
            }

            if (Settings.Default.MyMaps == "BingMap")
            {
                ComboBoxMap.Text = "Bing Maps";
            }
            else if (Settings.Default.MyMaps == "GoogleMap")
            {
                ComboBoxMap.Text = "Google Maps";
            }
            else if (Settings.Default.MyMaps == "OviMap")
            {
                ComboBoxMap.Text = "Nokia Maps";
            }
            else if (Settings.Default.MyMaps == "OpenStreetMap")
            {
                ComboBoxMap.Text = "OpenStreetMap";
            }

            Type colorsType = typeof(Colors);
            PropertyInfo[] colorsTypePropertyInfos = colorsType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (PropertyInfo colorsTypePropertyInfo in colorsTypePropertyInfos)
                ComboBoxRouteColor.Items.Add(colorsTypePropertyInfo.Name);
            ComboBoxRouteColor.Text = Settings.Default.RouteColor;

            ComboBoxComPort.Text = Convert.ToString(Settings.Default.ComPort);

            if (ComboBoxComPort.Text == "999")
            {
                ComboBoxComPort.Text = "Microsoft Location Sensor";
            }

            ComboBoxSpeed.Text = Convert.ToString(Settings.Default.ComSpeed);
        }

        #endregion New

        #region Option_Loaded

        private void Option_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow w = System.Windows.Application.Current.Windows[0] as MainWindow;
            w.Visibility = Visibility.Collapsed;
        }

        #endregion Option_Loaded

        #region OptionClosing

        private void OptionClosing(object sender, CancelEventArgs e)
        {
            MainWindow.newtop = Top;
            MainWindow.newleft = Left;
        }

        #endregion OptionClosing

        #region WindowsSourceInitialized

        private void WindowsSourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);
        }

        #endregion WindowsSourceInitialized

        #region WndProc

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WmSyscommand:
                    int command = wParam.ToInt32() & 0xFFF0;
                    if (command == ScMove)
                    {
                        handled = false;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        #endregion WndProc

        #region WndProc

        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
        }

        #endregion WndProc

        #region SerialPortErrorReceived

        private static void SerialPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // Erreurs de réception
            Console.WriteLine(e.EventType.ToString());
            Trace.WriteLine(DateTime.Now + " " + "SerialPortErrorReceived: " + e.EventType.ToString());
        }

        #endregion SerialPortErrorReceived

        #region ButtonCancelChanges

        private void ButtonCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "ButtonCancelChangesClick");
            CacheOptimization = false;
            MainWindow.ImportDB = false;
            MainWindow.FirstComError = true;
            MainWindow.ReloadMap = false;
            MainWindow.PrimaryCacheOptimization = false;
            MainWindow.ShowTiles = false;
            Close();
        }

        #endregion ButtonCancelChanges

        #region ButtonDeleteCache

        private void ButtonOptimizationCache_Click(Object sender, RoutedEventArgs e)
        {
            CacheOptimization = true;
        }

        #endregion ButtonDeleteCache

        #region ButtonHardwareDetect

        private void ButtonHardwareDetect_Click(object sender, RoutedEventArgs e)
        {
            ComPortScan = true;

            Message newform = new Message()
            {
                // Message de confirmation du scan
                Owner = this
            };

            newform.ShowDialog();

            ComPortScan = false;

            if (Message.MustScan)
            {
                // Message durant le scan
                newform = new Message()
                {
                    Owner = this
                };

                newform.Show();

                // On lance le scan
                string myport = ScanPorts();

                newform.Close();
                Message.MustScan = false;

                // GPS non trouvé
                if (String.Equals(myport, "", StringComparison.CurrentCulture))
                {
                    // Message GPS non trouvé
                    ComPortDiscovered = 2;
                    newform = new Message()
                    {
                        Owner = this
                    };

                    newform.ShowDialog();
                    ComPortDiscovered = 0;
                    return;
                }
                else
                {
                    // GPS trouvé
                    ComPortDiscovered = 1;
                    Settings.Default.ComPort = Convert.ToInt32((myport.Substring(myport.Length - 1)));
                    ComboBoxComPort.Text = Convert.ToInt32((myport.Substring(myport.Length - 1))).ToString();
                    Settings.Default.Save();

                    // Message GPS trouvé + COM
                    newform = new Message()
                    {
                        Owner = this
                    };

                    newform.ShowDialog();
                    ComPortDiscovered = 0;
                }
            }
        }

        #endregion ButtonHardwareDetect

        #region ButtonSaveChanges

        private void ButtonSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "ButtonSaveChangesClick");

            // Le Proxy est utilisé
            if (CheckBoxProxyAuthentication.IsChecked ?? true)
            {
                Settings.Default.UseProxy = true;
                MainWindow.ProxyAuthentification();
            }
            else
            {
                Settings.Default.UseProxy = false;
            }

            // L'adresse proxy contient http:// ou https//
            if (TextBoxProxyAdress.Text.ToLower().Contains("http://") || (TextBoxProxyAdress.Text.ToLower().Contains("https://")))
            {
                // Le port du Proxy n'est pas vide est contient du numérique
                if ((!(string.IsNullOrEmpty(TextBoxProxyPort.Text))) && Information.IsNumeric(TextBoxProxyPort.Text))
                {
                    Settings.Default.ProxyPort = Convert.ToInt32(TextBoxProxyPort.Text);
                    Settings.Default.ProxyAdress = TextBoxProxyAdress.Text;
                    Settings.Default.ProxyUsername = TextBoxUsername.Text;
                    Settings.Default.ProxyPassword = PasswordBox.Password;
                }
                else
                {
                    // Si le port du Proxy est vide on le met à 0
                    if (string.IsNullOrEmpty(TextBoxProxyPort.Text))
                    {
                        Settings.Default.ProxyPort = 0;
                    }
                }
            }

            if (CheckBoxProxyAuthentication.IsChecked ?? true)
            {
                Settings.Default.UseProxy = true;
            }
            else
            {
                Settings.Default.UseProxy = false;
            }

            if (CheckBoxGpsHardware.IsChecked ?? true)
            {
                Settings.Default.UseGpsHarware = true;
                MainWindow.FirstComError = true;
            }
            else
            {
                Settings.Default.UseGpsHarware = false;
            }

            if (CheckBoxRoute.IsChecked ?? true)
            {
                Settings.Default.Routing = true;
            }
            else
            {
                Settings.Default.Routing = false;
                MainWindow.CleanRoute = true;
            }

            if ((ComboBoxAccuracy.Text == "Offline (default)"))
            {
                Settings.Default.DistanceAccuracy = false;
            }
            else
            {
                Settings.Default.DistanceAccuracy = true;
            }

            if ((ComboBoxMap.Text != (Settings.Default.MyMaps)))
            {
                MainWindow.ReloadMap = true;
            }

            if ((ComboBoxMap.Text == "Bing Maps"))
            {
                Settings.Default.MyMaps = "BingMap";
            }
            else if ((ComboBoxMap.Text == "Google Maps"))
            {
                Settings.Default.MyMaps = "GoogleMap";
            }
            else if ((ComboBoxMap.Text == "Nokia Maps"))
            {
                Settings.Default.MyMaps = "OviMap";
            }
            else if ((ComboBoxMap.Text == "OpenStreetMap"))
            {
                Settings.Default.MyMaps = "OpenStreetMap";
            }

            if (CheckBoxUpdates.IsChecked ?? true)
            {
                Settings.Default.AutoUpdate = true;
            }
            else
            {
                Settings.Default.AutoUpdate = false;
            }

            if (ComboBoxComPort.Text == "Microsoft Location Sensor")
            {
                Settings.Default.ComPort = 999;
            }
            else
            {
                Settings.Default.ComPort = Convert.ToInt32(ComboBoxComPort.Text);
            }

            Settings.Default.ComSpeed = Convert.ToInt32(ComboBoxSpeed.Text);
            Settings.Default.RouteColor = ComboBoxRouteColor.Text;

            SaveChange = true;
            Properties.Settings.Default.Save();

            if (CheckBoxProxyAuthentication.IsChecked ?? true)
            {
                MainWindow.ProxyAuthentification();
            }

            if (CheckBoxShowTiles.IsChecked ?? true)
            {
                MainWindow.ShowTiles = true;
                Settings.Default.ShowTileGridLines = true;
            }
            else
            {
                MainWindow.ShowTiles = false;
                Settings.Default.ShowTileGridLines = false;
            }

            if (CacheOptimization)
            {
                CacheOptimization = false;
                MainWindow.PrimaryCacheOptimization = true;
            }

            Close();
        }

        #endregion ButtonSaveChanges

        #region ButtonSoftwareUpdate

        private void ButtonSoftwareUpdate_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.XmlParser();

            Update = true;

            Message newform = new Message()
            {
                Owner = this
            };

            newform.ShowDialog();
        }

        #endregion ButtonSoftwareUpdate

        #region CheckPortForGps

        private bool CheckPortForGps(string port)
        {
            string testData = null;
            int i = 0;
            ComSearch = true;

            try
            {
                _spa = new SerialPort(port, Settings.Default.ComSpeed, Parity.None, 8, StopBits.One);

                if (_spa.IsOpen)
                {
                    return false;
                }

                _spa.ReadBufferSize = 4096;
                _spa.Open();

                if (_spa.IsOpen)
                {
                    // On fait 2 essais du même port
                    for (i = 1; i <= 2; i++)
                    {
                        Thread.Sleep(1000);
                        testData = _spa.ReadExisting();
                        if (testData.Contains("$GPGGA"))
                        {
                            _spa.Close();

                            return true;
                        }
                    }
                    _spa.Close();
                }
                return false;
            }
            catch (IOException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "CheckPortForGps: " + ex);
                return false;
            }
        }

        #endregion CheckPortForGps

        #region ScanPorts

        private string ScanPorts()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    try
                    {
                        if (CheckPortForGps(port))
                        {
                            return port;
                        }
                    }
                    catch (IOException ex)
                    {
                        Trace.WriteLine(DateTime.Now + " " + "ScanPorts: " + ex);
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "ScanPorts: " + ex);
                return "";
            }
        }

        #endregion ScanPorts

        #region ShowComports

        private void ShowComports()
        {
            ComboBoxComPort.Items.Clear();

            ComboBoxComPort.Items.Add("Microsoft Location Sensor");

            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                ComboBoxComPort.Items.Add(port.ToString().Replace("COM", ""));
            }

            if (ComboBoxComPort.Items.Count > 0)
            {
                ComboBoxComPort.SelectedIndex = ComboBoxComPort.Items.Count - 1;
                ComboBoxSpeed.SelectedIndex = 2;
            }
        }

        #endregion ShowComports
    }
}