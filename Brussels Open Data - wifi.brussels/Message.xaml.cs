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
// |    Copyright © Pascal Hubert - Brussels, Belgium 2022. <mailto:pascal.hubert@outlook.com>                  |
// •————————————————————————————————————————————————————————————————————————————————————————————————————————————•

using System.Diagnostics;
using System.Windows;

namespace OpenData
{
    /// <summary>
    /// Interaction logic for Message.xaml
    /// </summary>
    public partial class Message
    {
        public static bool MustScan;

        public Message()
        {
            InitializeComponent();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Option.ComSearch)
            {
                Option.ComSearch = false;
            }

            Option.ComPortScan = false;
            Option.Update = false;
            Close();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (Option.GeolockUpdate)
            {
                Process.Start("https://github.com/neojudgment");
            }

            if (Option.ComPortScan)
            {
                MustScan = true;
            }

            if (MainWindow.PortError)
            {
                MainWindow.PortError = false;
            }

            Option.ComPortScan = false;
            Option.Update = false;
            Close();
        }

        private void Message_Loaded(object sender, RoutedEventArgs e)
        {
            if (Option.Update)
            {
                if (Option.GeolockUpdate)
                {
                    LabelMessage1.Content = "a new version of GeoLock is available";
                    LabelMessage2.Content = "Do you want to download this version?";
                    ButtonCancel.Visibility = Visibility.Visible;
                }
                else if ((Option.GeolockUpdate) && (!Option.ComPortScan) && (Option.ComPortDiscovered == 0))
                {
                    LabelMessage1.Content = "no update avaible";
                    LabelMessage2.Content = "You have the latest version of GeoLock.";
                    ButtonCancel.Visibility = Visibility.Hidden;
                }
            }

            if (Option.ComPortScan)
            {
                LabelMessage1.Content = "ComPort Scan";
                LabelMessage2.Content = "This will attempt to discover your GPS Hardware.";
                ButtonCancel.Visibility = Visibility.Visible;
            }
            if (Option.ComPortDiscovered == 1)
            {
                LabelMessage1.Content = "ComPort Scan";
                LabelMessage2.Content = "GPS Hardware successfully discovered on COM" + Properties.Settings.Default.ComPort + ".";
                ButtonCancel.Visibility = Visibility.Hidden;
            }
            else if (Option.ComPortDiscovered == 2)
            {
                LabelMessage1.Content = "ComPort Scan";
                LabelMessage2.Content = "GPS Hardware not found!";
                ButtonCancel.Visibility = Visibility.Hidden;
            }
            if (MustScan)
            {
                LabelMessage1.Content = "ComPort Scan";
                LabelMessage2.Content = "Scan in progress. Please wait!";
                ButtonOk.Visibility = Visibility.Hidden;
                ButtonCancel.Visibility = Visibility.Hidden;
            }

            if (MainWindow.PortError)
            {
                LabelMessage1.Content = "ComPort Error or hardware disconnected";
                LabelMessage2.Content = "Please check device options in settings!";
                ButtonOk.Visibility = Visibility.Visible;
                ButtonCancel.Visibility = Visibility.Hidden;
            }

            if (!MainWindow.PortError && !MainWindow.FirstComError)
            {
                LabelMessage1.Content = "ComPort or hardware has been discovered";
                LabelMessage2.Content = "Please check if the device is working properly!";
                ButtonOk.Visibility = Visibility.Visible;
                ButtonCancel.Visibility = Visibility.Hidden;
            }
        }
    }
}