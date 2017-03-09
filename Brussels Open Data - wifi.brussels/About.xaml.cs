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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace OpenData
{
    public partial class About
    {
        #region Variables

        private const int ScMove = 0xF010;
        private const int WmSyscommand = 0x112;

        #endregion Variables

        #region New

        public About()
        {
            InitializeComponent();
            Closing += AboutClosing;
            SourceInitialized += WindowsSourceInitialized;

            // version
            Version maVersion = Assembly.GetExecutingAssembly().GetName().Version;

            // Architecture 32 ou 64 bits
            string process = null;
            if (Environment.Is64BitProcess)
            {
                process = "64-bit";
            }
            else
            {
                process = "32-bit";
            }

            LabelVersion.Content = "brussels open data - wifi.brussels v" + maVersion.ToString() + " " + process + " Edition (alpha)";
        }

        #endregion New

        #region About_Loaded

        private void About_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow w = System.Windows.Application.Current.Windows[0] as MainWindow;
            w.Visibility = Visibility.Collapsed;
        }

        #endregion About_Loaded

        #region AboutClosing

        private void AboutClosing(object sender, CancelEventArgs e)
        {
            MainWindow.newtop = Top;
            MainWindow.newleft = Left;
        }

        #endregion AboutClosing

        #region HyperlinkRequestNavigate

        private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion HyperlinkRequestNavigate

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
    }
}