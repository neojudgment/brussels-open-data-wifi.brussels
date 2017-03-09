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

using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using Newtonsoft.Json;
using OpenData.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using static System.Environment;
using ThreadState = System.Threading.ThreadState;

namespace OpenData
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    [Guid("44C28944-AFFA-443B-978E-226C874570CE")]
    public partial class MainWindow
    {
        #region Variables

        public static bool CleanRoute;
        public static bool FirstComError = true;
        public static bool ImportDB;
        public static double newleft;
        public static double newtop;
        public static bool PortError;
        public static bool PrimaryCacheOptimization;
        public static bool ReloadMap;
        public static bool ShowTiles;
        public string Coordinates0;
        public string Coordinates1;
        private const double BxlLatitude = 50.844;
        private const double BxlLongitude = 4.360;
        private const int ScMove = 0xF010;
        private const int WmSyscommand = 0x112;
        private static bool _InternetscanState;
        private static SerialPort _spa;
        private static StreamWriter Sw;
        private readonly Image _mePushPin = new Image();
        private readonly DispatcherTimer _timerBattery;
        private readonly DispatcherTimer _timerGpsScan;
        private readonly DispatcherTimer _timerInternetIcone;
        private readonly TextWriterTraceListener _tl;
        private string _batteryState;
        private double _bearing;
        private double _distance;
        private bool _firststart = true;
        private Thread _InternetScanThread = new Thread(InternetScanIcon);
        private double _latitude;
        private double _latitudeChanged;
        private string _latitudeindicateur;
        private double _latitudePushpinClicked;
        private double _longitude;
        private double _longitudeChanged;
        private string _longitudeindicateur;
        private double _longitudePushpinClicked;
        private int _mytag;
        private string _utcTime;
        private GeoCoordinateWatcher _watcher;
        private bool _watcherSender;
        private bool _watcherState;
        private GMapMarker marker;
        private GMapRoute mRoute;
        private BitmapImage myIcon;
        private bool newmroute;
        private Stopwatch stopWatch = new Stopwatch();

        #endregion Variables

        #region New

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += WindowsSourceInitialized;

            CreateDirectory();

            ProfileOptimization.SetProfileRoot(GetFolderPath(SpecialFolder.ApplicationData) + "\\brussels open data - wifi.brussels" + "\\profiles");
            ProfileOptimization.StartProfile("profile");

            // Initialisation du Trace.Listeners
            Sw = new StreamWriter(
               GetFolderPath(SpecialFolder.ApplicationData) +
               "\\brussels open data - wifi.brussels\\log\\OpenData-wifi.brussels.log", false);

            _tl = new TextWriterTraceListener(Sw);
            InitialisationTraceListener();
            Trace.WriteLine(DateTime.Now + " " + "Démarrage de OpenData - wifi.brussels...");

            // Calcul du temps de démarrage
            stopWatch.Start();

            MainMap.MouseLeftButtonDown += new MouseButtonEventHandler(PushpinMouseDown);
            MainMap.OnSelectionChange += new SelectionChange(MainMapPrefetch);

            Closing += MainWindowClosing;

            // On enregistre l'application afin que celle-ci redémarre automatiquement si elle c'est
            // terminée pour une raison autre qu'un redémarrage du système ou une mise à jour du système.
            ApplicationRestartRecoveryManager.RegisterForApplicationRestart(new RestartSettings("/restart",
                RestartRestrictions.NotOnReboot | RestartRestrictions.NotOnPatch));

            // On va rechercher l'ancien user.config si le num de version est différent
            string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            if (!string.Equals(Settings.Default.CurrentVersion, version, StringComparison.CurrentCulture))
            {
                Settings.Default.Upgrade();
                Settings.Default.CurrentVersion = version;
                Settings.Default.Save();
            }

            // Configuration du proxy
            if (Settings.Default.UseProxy)
            {
                ProxyAuthentification();
            }
            else
            {
                Trace.WriteLine(DateTime.Now + " " + "Proxy par défaut");
                GMapProvider.WebProxy = WebRequest.DefaultWebProxy;
            }

            // Passe en mode cache si pas de connection internet
            if (CheckNet())
            {
                Trace.WriteLine(DateTime.Now + " " + "ServerAndCache");
                MainMap.Manager.Mode = AccessMode.ServerAndCache;
            }
            else
            {
                Trace.WriteLine(DateTime.Now + " " + "AccessMode.CacheOnly");
                MainMap.Manager.Mode = AccessMode.CacheOnly;
            }

            // Configuration de la carte
            foreach (GMapProvider mp in GMapProviders.List)
            {
                if (mp.Name == Settings.Default.MyMaps)
                {
                    MainMap.MapProvider = mp;
                }
            }

            // On affiche les tuiles
            if (Settings.Default.ShowTileGridLines)
            {
                MainMap.ShowTileGridLines = true;
            }
            else
            {
                MainMap.ShowTileGridLines = false;
            }

            GMapProvider.Language = LanguageType.English;
            MainMap.MaxZoom = 18;
            MainMap.MinZoom = 10;
            MainMap.Zoom = 13;
            MainMap.CacheLocation = GetFolderPath(SpecialFolder.CommonApplicationData) + "\\Brussels Open Data - wifi.brussels\\cache\\";
            MainMap.IgnoreMarkerOnMouseWheel = true;
            MainMap.DragButton = MouseButton.Right;
            MainMap.ShowCenter = false;
            MainMap.Manager.UseRouteCache = true;
            MainMap.Manager.UseGeocoderCache = true;
            MainMap.Manager.UsePlacemarkCache = true;
            MainMap.Position = new PointLatLng(BxlLatitude, BxlLongitude);

            // Initialisation des Timers
            _timerBattery = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timerInternetIcone = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timerGpsScan = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromSeconds(Settings.Default.GpsScanInterval)
            };

            // Etat de l'alimentation au démarage
            if (PowerManager.IsBatteryPresent)
            {
                // Le niveau de la batterie est bas
                if (PowerManager.BatteryLifePercent < 20)
                {
                    _timerBattery.Start();
                }
            }
            else
            {
                _batteryState = "high";
            }

            _timerInternetIcone.Start();

            // On surveille l'état de l'alimentation
            PowerManager.BatteryLifePercentChanged += PowerModeChanged;

            _timerInternetIcone.Tick += TimerInternetDetection;
            _timerBattery.Tick += TimerBatteryState;
            _timerGpsScan.Tick += TimerGpsScanTick;
        }

        #endregion New

        #region Loaded

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "MainWindow_Loaded");
            SelectSensorOrGps();

            if (
               !File.Exists(GetFolderPath(SpecialFolder.ApplicationData) +
                            "\\brussels open data - wifi.brussels\\json\\geoserver-GetFeature.json"))
            {
                if (CheckNet())
                {
                    DownloadClient();
                }
            }
            else
            {
                PopulateBingMap();
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Trace.WriteLine(DateTime.Now + " " + "Temps de démarrage: " + elapsedTime);
            Console.WriteLine(DateTime.Now + " " + "Temps de démarrage: " + elapsedTime);
        }

        #endregion Loaded

        #region SensorOrGps

        private void SelectSensorOrGps()
        {
            try
            {
                Trace.WriteLine(DateTime.Now + " " + "SelectSensorOrGps");

                if (Settings.Default.ComPort != 999 && Settings.Default.UseGpsHarware)
                {
                    if (_spa == null || (!_timerGpsScan.IsEnabled))
                    {
                        // Overture du port GPS
                        OpenComPort();
                        _timerGpsScan.Start();

                        if (_watcherSender)
                        {
                            Trace.WriteLine(DateTime.Now + " " + "On switch des sensors vers GPS");
                            _watcher.PositionChanged -= WatcherPositionChanged;
                            _watcherSender = false;
                        }
                    }
                }
                else
                {
                    if (_spa != null || _timerGpsScan.IsEnabled)
                    {
                        Trace.WriteLine(DateTime.Now + " " + "On switch du GPS vers Sensors");
                        CloseComport();
                        _timerGpsScan.Stop();
                    }

                    // On crée le watcher en précision Haute.
                    _watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                    _watcher.TryStart(false, TimeSpan.FromMilliseconds(1000));
                    // 1 mètres de déplacement pour mise à jour du déplacement
                    _watcher.MovementThreshold = 1.0;
                    _watcher.PositionChanged += WatcherPositionChanged;
                    _watcherSender = true;
                }
            }
            catch (COMException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "GPS ComPort Error: " + ex.ToString());
                PortError = true;
                return;
            }
        }

        #endregion SensorOrGps

        #region ProgressChanged

        private static void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
        }

        #endregion ProgressChanged

        #region DownloadClient

        private void DownloadClient()
        {
            Trace.WriteLine(DateTime.Now + " " + "Entrée dans DownloadClient");
            string TempPath = Path.GetTempPath();

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += DownloadCompleted;
                webClient.DownloadProgressChanged += ProgressChanged;
                webClient.DownloadFileAsync(
                    new Uri("https://gis.irisnet.be/geoserver/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=URBIS_AAS:coverage_points_diffusion&srsName=EPSG:4326&outputFormat=json"),
                   TempPath + "geoserver-GetFeature.json");

                webClient.Dispose();

                Trace.WriteLine(DateTime.Now + " " + "Sortie de DownloadClient");
            }
            catch (WebException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "WebException dans DownloadClient " + ex);
            }
        }

        #endregion DownloadClient

        #region PopulateBingMap

        private void PopulateBingMap()
        {
            Trace.WriteLine(DateTime.Now + " " + "Entrée dans PopulateBingMap");

            try
            {
                string json =
                    File.ReadAllText(GetFolderPath(SpecialFolder.ApplicationData) +
                                     "\\brussels open data - wifi.brussels\\json\\geoserver-GetFeature.json");

                Json2Csharp.RootObject ab = JsonConvert.DeserializeObject<Json2Csharp.RootObject>(json);
                int i = 1;

                do
                {
                    if (!string.IsNullOrEmpty(ab.Features[i].Geometry.Coordinates[0].ToString(CultureInfo.InvariantCulture)))
                    {
                        Coordinates0 = ab.Features[i].Geometry.Coordinates[0].ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(ab.Features[i].Geometry.Coordinates[1].ToString(CultureInfo.InvariantCulture)))
                    {
                        Coordinates1 = ab.Features[i].Geometry.Coordinates[1].ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        continue;
                    }

                    // Position du marker
                    marker = new GMapMarker(new PointLatLng(ab.Features[i].Geometry.Coordinates[1], ab.Features[i].Geometry.Coordinates[0]));

                    if (ab.Features[i].Properties.STATUT == "OPERATIONNEL")
                    {
                        if (_mytag == 0)
                        {
                            _mytag = 26;
                        }

                        if (_mytag == i)
                        {
                            myIcon = new BitmapImage(new Uri("Resources\\wi-fi-6.png", UriKind.Relative));
                        }
                        else
                        {
                            myIcon = new BitmapImage(new Uri("Resources\\wi-fi-2.png", UriKind.Relative));
                        }
                    }
                    else
                    {
                        myIcon = new BitmapImage(new Uri("Resources\\wi-fi-3.png", UriKind.Relative));
                    }

                    marker.Shape = new Image
                    {
                        Source = myIcon,
                        Width = 25,
                        Cursor = Cursors.Hand,
                        Height = 29,
                        IsHitTestVisible = true
                    };

                    // Ajout du Custom PushPin sur le MapLayer
                    marker.Offset = new Point(-12.5, -29);
                    marker.ZIndex = i;
                    MainMap.Markers.Add(marker);
                    i += 1;
                }

                while (!(i == ab.Features.Count - 1));

                Trace.WriteLine(DateTime.Now + " " + "Nombre de points d'accès: " + (i -= 1));
                Console.WriteLine(DateTime.Now + " " + "Nombre de points d'accès: " + (i -= 1));

                if (!Settings.Default.UseGpsHarware)
                {
                    _watcher.TryStart(false, TimeSpan.FromMilliseconds(3000));
                }

                if (_firststart)
                {
                    PushpinMouseDown(null, null);
                }
                else
                {
                    MovePushpin();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "Exception dans PopulateBingMap " + ex);
            }
            Trace.WriteLine(DateTime.Now + " " + "Sortie de PopulateBingMap");
        }

        #endregion PopulateBingMap

        #region Completed

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string TempPath = Path.GetTempPath();
                FileInfo fi = new FileInfo(TempPath + "geoserver-GetFeature.json");
                long fiLength = fi.Length;

                // taille du fichier json en octets
                if (fiLength > 20000)
                {
                    File.Delete(GetFolderPath(SpecialFolder.ApplicationData) +
                    "\\brussels open data - wifi.brussels\\json\\geoserver-GetFeature.json");

                    File.Move(TempPath + "geoserver-GetFeature.json", GetFolderPath(SpecialFolder.ApplicationData) +
                    "\\brussels open data - wifi.brussels\\json\\geoserver-GetFeature.json");

                    PopulateBingMap();
                }
            }
            catch (IOException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "DownloadCompleted: " + ex);
            }
        }

        #endregion Completed

        #region PushpinMouseDown

        private void PushpinMouseDown(object sender, MouseButtonEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "Entrée dans PushpinMouseDown");
            int tag = 0;
            try
            {
                try
                {
                    if (_firststart)
                    {
                        tag = 26;
                        _firststart = false;
                    }
                    else
                    {
                        tag = ((GMapMarker)((FrameworkElement)e.OriginalSource).DataContext).ZIndex;
                    }

                    _mytag = tag;
                    Trace.WriteLine(DateTime.Now + " " + "ZIndex: " + tag);
                    Console.WriteLine("ZIndex: " + tag);

                    if (tag == 999)
                    {
                        return;
                    }
                }
                catch (NullReferenceException ex)
                {
                    // tag = 25;
                    Trace.WriteLine(DateTime.Now + " " + "ZIndex: " + tag);
                    Console.WriteLine(DateTime.Now + " " + "ZIndex: " + ex);
                    return;
                }

                string json =
                File.ReadAllText(GetFolderPath(SpecialFolder.ApplicationData) +
                                 "\\brussels open data - wifi.brussels\\json\\geoserver-GetFeature.json");

                Json2Csharp.RootObject ab = JsonConvert.DeserializeObject<Json2Csharp.RootObject>(json);

                string mydateTime = DateTime.UtcNow.ToLongTimeString();
                LabelTimeStamp.Content = "TimeStamp: " + mydateTime + " UTC";

                if (ab.Features[Convert.ToInt32(tag)].Type != null)
                {
                    LabelType.Content = "Type: " + ab.Features[Convert.ToInt32(tag)].Type;
                }
                else
                {
                    LabelType.Content = "Type: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Geometry.Type != null)
                {
                    LabelGeometryType.Content = "Geometry Type: " +
                                                ab.Features[Convert.ToInt32(tag)].Geometry.Type;
                }
                else
                {
                    LabelGeometryType.Content = "Geometry Type: no data";
                }

                if (!string.IsNullOrEmpty(ab.Features[Convert.ToInt32(tag)].Geometry.Coordinates[1].ToString(CultureInfo.InvariantCulture)))
                {
                    LabelLatitudeDegrees.Content = "Latitude Degrees: " +
                                                   ab.Features[Convert.ToInt32(tag)].Geometry.Coordinates[1];

                    _latitudePushpinClicked = ab.Features[Convert.ToInt32(tag)].Geometry.Coordinates[1];
                }
                else
                {
                    LabelLatitudeDegrees.Content = "Latitude Degrees: no data";
                }

                if (!string.IsNullOrEmpty(ab.Features[Convert.ToInt32(tag)].Geometry.Coordinates[0].ToString(CultureInfo.InvariantCulture)))
                {
                    LabelLongitudeDegrees.Content = "Longitude Degrees: " +
                                                    ab.Features[Convert.ToInt32(tag)].Geometry.Coordinates[0];

                    _longitudePushpinClicked = ab.Features[Convert.ToInt32(tag)].Geometry.Coordinates[0];
                }
                else
                {
                    LabelLongitudeDegrees.Content = "Longitude Degrees: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Geometry_name != null)
                {
                    LabelGeometryName.Content = "Geometry Name: " +
                                                ab.Features[Convert.ToInt32(tag)].Geometry_name;
                }
                else
                {
                    LabelGeometryName.Content = "Geometry Name: no data";
                }

                if (!string.IsNullOrEmpty(ab.Features[Convert.ToInt32(tag)].Properties.ID.ToString(CultureInfo.InvariantCulture)))
                {
                    LabelId.Content = "Id: " + ab.Features[Convert.ToInt32(tag)].Properties.ID;
                }
                else
                {
                    LabelId.Content = "Id: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.ID_EXTERNE != null)
                {
                    LabelExternID.Content = "Extern Id: " +
                                            ab.Features[Convert.ToInt32(tag)].Properties.ID_EXTERNE;
                }
                else
                {
                    LabelExternID.Content = "Extern Id: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.CLIENT != null)
                {
                    LabelClient.Content = "Client: " + ab.Features[Convert.ToInt32(tag)].Properties.CLIENT;
                }
                else
                {
                    LabelClient.Content = "Client: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.TYPE_CLIENT != null)
                {
                    LabelClientType.Content = "Client Type: " +
                                              ab.Features[Convert.ToInt32(tag)].Properties.TYPE_CLIENT;
                }
                else
                {
                    LabelClientType.Content = "Client Type: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.AP_COUNT != null)
                {
                    LabelAPCount.Content = "AP Count: " + ab.Features[Convert.ToInt32(tag)].Properties.AP_COUNT;
                }
                else
                {
                    LabelAPCount.Content = "AP Count: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.INTERV_CIRB != null)
                {
                    LabelIntervCIRB.Content = "Interv CIRB: " +
                                              ab.Features[Convert.ToInt32(tag)].Properties.INTERV_CIRB;
                }
                else
                {
                    LabelIntervCIRB.Content = "Interv CIRB: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.LIEU_INSTALLATION != null)
                {
                    LabelInstallationSite.Content = "Installation Site: " +
                                                    ab.Features[Convert.ToInt32(tag)].Properties
                                                        .LIEU_INSTALLATION;
                }
                else
                {
                    LabelInstallationSite.Content = "Installation Site: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.NOM_SITE != null)
                {
                    LabelSiteName.Content = "Site Name: " +
                                            ab.Features[Convert.ToInt32(tag)].Properties.NOM_SITE;
                }
                else
                {
                    LabelSiteName.Content = "Site Name: no data";
                }

                if (ab.Features[Convert.ToInt32(tag)].Properties.STATUT != null)
                {
                    if (ab.Features[Convert.ToInt32(tag)].Properties.STATUT == "OPERATIONNEL")
                    {
                        LabelRealTimeStatus.Content = "Status: " + "Operational";
                    }
                    else
                    {
                        LabelRealTimeStatus.Content = "Status: " +
                                                      ab.Features[Convert.ToInt32(tag)].Properties.STATUT;
                    }
                }
                else
                {
                    LabelRealTimeStatus.Content = "Status: no data";
                }

                _distance = CalculDistance(_latitudePushpinClicked, _longitudePushpinClicked, _latitudeChanged,
                    _longitudeChanged);

                if (!(_distance > 15))
                {
                    double miles = ConvertKilometersToMiles(_distance);

                    LabelDistance.Content = "Approx. Distance: " + string.Format("{0:0.0}", _distance) +
                                            " km" + " - " +
                                            string.Format("{0:0.0}", miles) + " miles.";
                }
                else
                {
                    LabelDistance.Content = "Approx. Distance: no data";
                }

                CurrentBearing();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "Exception dans PushpinMouseDown: " + ex);
            }

            Trace.WriteLine(DateTime.Now + " " + "Sortie de PushpinMouseDown");

            MainMap.Markers.Clear();
            PopulateBingMap();
        }

        #endregion PushpinMouseDown

        #region CurrentBearing

        private void CurrentBearing()
        {
            // déviation en degré
            if (!Settings.Default.Routing)
            {
                _bearing = CalculBearing(_latitudePushpinClicked, _longitudePushpinClicked, _latitudeChanged,
                    _longitudeChanged);
            }

            if ((_latitudeChanged > 0) && (_longitudeChanged > 0))
            {
                Console.WriteLine("Direction: " + string.Format("{0:0}", _bearing) + " deg");
                Trace.WriteLine(DateTime.Now + " " + "Direction: " + string.Format("{0:0}", _bearing) + " deg");
            }
        }

        #endregion CurrentBearing

        #region CheckNet

        private static bool CheckNet()
        {
            bool returnValue = false;
            try
            {
                returnValue = InternetGetConnectedState(out int Desc, 0);
            }
            catch
            {
                returnValue = false;
            }
            return returnValue;
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        #endregion CheckNet

        #region ButtonPlusMouseDown

        private void ButtonPlusMouseDown(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "ButtonPlusMouseDown");

            try
            {
                if (MainMap.IsLoaded)
                {
                    if (MainMap.Zoom < 18)
                    {
                        // ZoomLevel plus
                        MainMap.Zoom += 1;
                    }
                }

                Trace.WriteLine(DateTime.Now + " " + "Le ZoomLevel est a" + " " + MainMap.Zoom + "x");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "BoutonPlusMouseDown Error: " + ex);
            }
        }

        #endregion ButtonPlusMouseDown

        #region ButtonMinusMouseDown

        private void ButtonMinusMouseDown(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "ButtonMinusMouseDown");

            try
            {
                if (MainMap.IsLoaded)
                {
                    if (MainMap.Zoom > 11)
                    {
                        // ZoomLevel minus
                        MainMap.Zoom -= 1;
                    }
                }

                Trace.WriteLine(DateTime.Now + " " + "Le ZoomLevel est a" + " " + MainMap.Zoom + "x");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "BoutonMinusMouseDown Error: " + ex);
            }
        }

        #endregion ButtonMinusMouseDown

        #region ButtonRefreshMouseDown

        private void ButtonRefreshMouseDown(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "ButtonRefreshMouseDown");

            try
            {
                // C'est un refresh. On déplace la carte (pas besoin de request).
                MainMap.Position = new PointLatLng(BxlLatitude, BxlLongitude);
                MainMap.Zoom = 12;
                Trace.WriteLine(DateTime.Now + " " + "Déplacement de la carte après un refresh (pas de request)");

                LinearProgressBar.Visibility = Visibility.Hidden;
                LinearProgressBar.Value = 0;
                _watcherState = false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "ButtonRefreshMouseDown Error: " + ex);
            }
        }

        #endregion ButtonRefreshMouseDown

        #region InternetScanIcon

        private static void InternetScanIcon()
        {
            try
            {
                if (CheckNet())
                {
                    // WiFi disponible
                    _InternetscanState = true;
                }
                else
                {
                    // WiFi non disponible
                    _InternetscanState = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "InternetScanIcon: " + ex);
                _InternetscanState = false;
            }
        }

        #endregion InternetScanIcon

        #region PowerModeChanged

        private void PowerModeChanged(object sender, EventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "PowerModeChanged");

            try
            {
                if (PowerManager.IsBatteryPresent)
                {
                    // Le niveau de la batterie est bas
                    if (PowerManager.BatteryLifePercent < 20)
                    {
                        if (!_timerBattery.IsEnabled)
                        {
                            _timerBattery.Start();
                        }
                    }
                }
                else
                {
                    // La batterie est ok
                    if (_timerBattery.IsEnabled)
                    {
                        _timerBattery.Stop();
                        BouttonBattery.Source = new BitmapImage(new Uri("Resources\\batteryhigh.gif", UriKind.Relative));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "PowerModeChanged: " + ex);
            }
        }

        #endregion PowerModeChanged

        #region MainWindowClosing

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "MainWindowClosing");
            Trace.WriteLine(DateTime.Now + " " + "On ferme l'application");

            _timerGpsScan.Stop();

            if (_watcher != null)
            {
                Console.WriteLine("Arrêt du GeoCoordonateWatcher");
                _watcher.PositionChanged -= WatcherPositionChanged;
            }

            Trace.WriteLine(DateTime.Now + " " + "On désenregistre et désactive application Recovery & Restart");
            ApplicationRestartRecoveryManager.UnregisterApplicationRestart();
            ApplicationRestartRecoveryManager.UnregisterApplicationRecovery();

            Settings.Default.Save();
            Trace.WriteLine(DateTime.Now + " " + "On ferme Comport et on ferme l'application");
            CloseComport();

            Trace.Flush();
            Trace.Close();

            System.Windows.Application.Current.Shutdown();
        }

        #endregion MainWindowClosing

        #region TimerInternetDetection

        private void TimerInternetDetection(object sender, EventArgs e)
        {
            try
            {
                // On scanne les réseaux WiFi disponibles
                switch (_InternetScanThread.ThreadState)
                {
                    case ThreadState.Unstarted:
                        _InternetScanThread.Start();
                        break;

                    case ThreadState.Stopped:
                        _InternetScanThread = new Thread(InternetScanIcon);
                        _InternetScanThread.Start();
                        break;

                    default:
                        return;
                }

                if (_InternetscanState)
                {
                    MainMap.Manager.Mode = AccessMode.ServerAndCache;
                    ButtonWifi.Source = new BitmapImage(new Uri("Resources\\wifiactive.gif", UriKind.Relative));
                }
                else
                {
                    MainMap.Manager.Mode = AccessMode.CacheOnly;
                    ButtonWifi.Source = new BitmapImage(new Uri("Resources\\wifiinactive.gif", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "TimerWiFiDetection: " + ex);
            }
        }

        #endregion TimerInternetDetection

        #region TimerBatteryState

        private void TimerBatteryState(object sender, EventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "TimerBatteryState");

            try
            {
                // On fait clignoter si le niveau la batterie est basse ou critique
                if (string.Equals(_batteryState, "high", StringComparison.CurrentCulture))
                {
                    BouttonBattery.Source = new BitmapImage(new Uri("Resources\\batterylow.gif", UriKind.Relative));
                    _batteryState = "low";
                }
                else
                {
                    BouttonBattery.Source = new BitmapImage(new Uri("Resources\\batteryhigh.gif", UriKind.Relative));
                    _batteryState = "high";
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "TimerBatteryState: " + ex);
            }
        }

        #endregion TimerBatteryState

        #region ButtonDownloadMouseDown

        private void ButtonDownloadMouseDown(object sender, RoutedEventArgs e)
        {
            if (CheckNet())
            {
                MainMap.Markers.Clear();

                if (_watcher != null)
                {
                    _watcher.Stop();
                }

                DownloadClient();
            }
        }

        #endregion ButtonDownloadMouseDown

        #region ButtonOptionMouseDown

        private void ButtonOptionMouseDown(object sender, RoutedEventArgs e)
        {
            Option window = new Option
            {
                ShowInTaskbar = true,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                Owner = System.Windows.Application.Current.MainWindow,
                Top = Top,
                Left = Left
            };

            window.ShowDialog();
            Top = newtop;
            Left = newleft;
            Visibility = Visibility.Visible;

            if (ReloadMap)
            {
                // Configuration de la carte
                foreach (GMapProvider mp in GMapProviders.List)
                {
                    if (mp.Name == Settings.Default.MyMaps)
                    {
                        MainMap.MapProvider = mp;
                    }
                }
            }

            if (PrimaryCacheOptimization)
            {
                Trace.WriteLine(DateTime.Now + " " + "On appele le Thread d'optimisation de la bdd");

                try
                {
                    Thread Optimization = new Thread(DBOptimization)
                    {
                        IsBackground = true
                    };

                    Optimization.Start();
                }
                catch (ThreadStateException ex)
                {
                    Trace.WriteLine(DateTime.Now + " " + "Erreur de Thread lors de du lancement de l'optimisation de la bdd: " + ex);
                }
            }

            if (ShowTiles)
            {
                MainMap.ShowTileGridLines = true;
            }
            else
            {
                MainMap.ShowTileGridLines = false;
            }

            if (CleanRoute)
            {
                // On efface l'itinéraire
                if (!(mRoute == null))
                {
                    mRoute.Clear();
                    newmroute = false;
                    ButtonRefreshMouseDown(null, null);
                }
            }

            SelectSensorOrGps();

            if (ReloadMap || PrimaryCacheOptimization || ShowTiles)
            {
                ShowTiles = false;
                ReloadMap = false;
                PrimaryCacheOptimization = false;
                MainMap.ReloadMap();
            }

            Option.BingMapApiKeyUpdate = false;
        }

        #endregion ButtonOptionMouseDown

        #region DBOptimization

        private void DBOptimization()
        {
            Trace.WriteLine(DateTime.Now + " " + "On entre dans le Thread d'optimisation de la bdd");

            try
            {
                if (File.Exists(GetFolderPath(SpecialFolder.CommonApplicationData) +
                "\\Brussels Open Data - wifi.brussels\\cache\\TileDBv5\\en\\Data.gmdb"))
                {
                    lock (this)
                    {
                        MainMap.Manager.OptimizeMapDb(GetFolderPath(SpecialFolder.CommonApplicationData) +
                        "\\Brussels Open Data - wifi.brussels\\cache\\TileDBv5\\en\\Data.gmdb");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "Erreur dans le Thread d'optimisation de la bdd: " + ex);
            }
        }

        #endregion DBOptimization

        #region ButtonWatcher

        private void ButtonWatcherMouseDown(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine(DateTime.Now + " " + "Entrée dans ButtonWatcherMouseDown");

            if (!_watcherState)
            {
                _watcherState = true;
                LinearProgressBar.Visibility = Visibility.Visible;

                MainMap.Zoom = 16;
                MainMap.Position = new PointLatLng(_latitudeChanged, _longitudeChanged);
            }
            else
            {
                _watcherState = false;
                LinearProgressBar.Visibility = Visibility.Hidden;

                MainMap.Zoom = 12;
                MainMap.Position = new PointLatLng(BxlLatitude, BxlLongitude);
            }

            Trace.WriteLine(DateTime.Now + " " + "Sortie de ButtonWatcherMouseDown");
        }

        #endregion ButtonWatcher

        #region ButtonInfoMouseDown

        private void ButtonInfoMouseDown(object sender, RoutedEventArgs e)
        {
            About window = new About
            {
                ShowInTaskbar = true,
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                Owner = System.Windows.Application.Current.MainWindow,
                Top = Top,
                Left = Left
            };

            window.ShowDialog();
            Top = newtop;
            Left = newleft;
            Visibility = Visibility.Visible;
        }

        #endregion ButtonInfoMouseDown

        #region WatcherPositionChanged

        private void WatcherPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Trace.WriteLine("");
            Trace.WriteLine(DateTime.Now + " " + "On entre dans WatcherPositionChanged");
            Console.WriteLine(DateTime.Now + " " + "On entre dans WatcherPositionChanged");

            try
            {
                // Pas de connection internet
                if (!CheckNet())
                {
                    Trace.WriteLine(DateTime.Now + " " + "Pas de connection internet");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now + " " + "Pas de connection internet: " + ex);
                Trace.WriteLine(DateTime.Now + " " + "Pas de connection internet: " + ex);
                return;
            }

            try
            {
                // Latitude et longitude actuelle.
                if (_watcher.Position.Location.IsUnknown)
                {
                    return;
                }
                else
                {
                    // Mise à jour du dernier TimeStamp
                    string mydateTime = DateTime.UtcNow.ToLongTimeString();
                    LabelTimeStamp.Content = "TimeStamp: " + mydateTime + " UTC";

                    // Le watcher contient le position actuelle
                    _latitudeChanged = _watcher.Position.Location.Latitude;
                    _longitudeChanged = _watcher.Position.Location.Longitude;

                    Console.WriteLine(DateTime.Now + " " + "Latitude: " + _watcher.Position.Location.Latitude);
                    Console.WriteLine(DateTime.Now + " " + "Longitude: " + _watcher.Position.Location.Longitude);

                    CurrentBearing();
                    MovePushpin();
                }

                _distance = CalculDistance(_latitudePushpinClicked, _longitudePushpinClicked, _latitudeChanged,
                 _longitudeChanged);

                if (_watcherState)
                {
                    MainMap.Position = new PointLatLng(_latitudeChanged, _longitudeChanged);
                }

                if (!(_distance > 15))
                {
                    double miles = ConvertKilometersToMiles(_distance);

                    LabelDistance.Content = "Approx. Distance: " + string.Format("{0:0.0}", _distance) +
                                            " km" + " - " +
                                            string.Format("{0:0.0}", miles) + " mi";

                    Trace.WriteLine(DateTime.Now + " " + "Approx. Distance: " + string.Format("{0:0.0}", _distance) +
                                            " km" + " - " +
                                            string.Format("{0:0.0}", miles) + " mi");
                }
                else
                {
                    LabelDistance.Content = "Approx. Distance: no data";
                }

                // Horizontal dilution of precision
                double hdop = (_watcher.Position.Location.HorizontalAccuracy);
                Trace.WriteLine(DateTime.Now + " " + "Horizontal dilution of precision: " + hdop);

                // Vitesse
                double speed = e.Position.Location.Speed;
                Trace.WriteLine(DateTime.Now + " " + "Speed: " + speed);

                Trace.WriteLine(DateTime.Now + " " + "WatcherPositionChanged latitude: " +
                                _watcher.Position.Location.Latitude);
                Trace.WriteLine(DateTime.Now + " " + "WatcherPositionChanged longitude: " +
                                _watcher.Position.Location.Longitude);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "Erreur dans WatcherPositionChanged: " + ex);
            }
        }

        #endregion WatcherPositionChanged

        #region MovePushpinWatcher

        private void MovePushpin()
        {
            try
            {
                Trace.WriteLine(DateTime.Now + " " + "On entre dans MovePushpinWatcher");
                Console.WriteLine(DateTime.Now + " " + "On entre dans MovePushpinWatcher");

                // Suppression du PushPin
                marker.ZIndex = 999;
                MainMap.Markers.Remove(marker);

                if (!Settings.Default.UseGpsHarware)
                {
                    marker = new GMapMarker(new PointLatLng(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude))
                    {
                        // Ajout du pushpin déplacé
                        Shape = new Image
                        {
                            Source = new BitmapImage(new Uri("Resources\\flag-export.png", UriKind.Relative)),
                            Width = 25,
                            Cursor = Cursors.Arrow,
                            Height = 29
                        },
                        Offset = new Point(-12.5, -29),
                        ZIndex = 999
                    };

                    MainMap.Markers.Add(marker);
                }
                else
                {
                    marker = new GMapMarker(new PointLatLng(_latitude, _longitude))
                    {
                        Shape = new Image
                        {
                            Source = new BitmapImage(new Uri("Resources\\flag-export.png", UriKind.Relative)),
                            Width = 25,
                            Cursor = Cursors.Arrow,
                            Height = 29
                        },
                        Offset = new Point(-12.5, -29),
                        ZIndex = 999
                    };

                    MainMap.Markers.Add(marker);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "Erreur dans MovePushpinWatcher: " + ex);
            }
        }

        #endregion MovePushpinWatcher

        #region StripHTML

        private string StripHTML(string HTMLText, bool decode = true)
        {
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            var stripped = reg.Replace(HTMLText, "");
            return decode ? HttpUtility.HtmlDecode(stripped) : stripped;
        }

        #endregion StripHTML

        #region CalculDistance

        private double CalculDistance(double lat1, double long1, double lat2, double long2)
        {
            GMapRoute r = null;
            double dist = 0;
            PointLatLng start = new PointLatLng(lat2, long2);
            PointLatLng end = new PointLatLng(lat1, long1);
            int i = 0;

            try
            {
                if (CheckNet() && lat2 > 0)
                {
                    if (Settings.Default.Routing || Settings.Default.DistanceAccuracy)
                    {
                        DirectionsStatusCode xx = GMapProviders.GoogleMap.GetDirections(out GDirections ss, start, end, true, true, true, false, true);
                        r = new GMapRoute(ss.Route);

                        Trace.WriteLine(DateTime.Now + " " + "Temps total de trajet: " + ss.Duration);
                        Console.WriteLine("Temps total de trajet: " + ss.Duration);

                        // Direction à suivre
                        string html = StripHTML(Convert.ToString(ss.Steps[0]));
                        Trace.WriteLine(DateTime.Now + " " + "Direction: " + html);
                        Console.WriteLine("Direction: " + html);

                        if (Settings.Default.Routing)
                        {
                            if (!(mRoute == null))
                            {
                                // On efface l'itinéraire précédent
                                mRoute.Clear();
                            }

                            if (ss != null)
                            {
                                List<PointLatLng> track = new List<PointLatLng>();

                                do
                                {
                                    track.Add(r.Points[i]);
                                    i += 1;
                                }
                                while (!(i == r.Points.Count));

                                try
                                {
                                    mRoute = new GMapRoute(track);
                                    {
                                        // On dessine l'itinéraire
                                        mRoute.ZIndex = 9999;
                                        mRoute.RegenerateShape(MainMap);
                                        SolidColorBrush NewBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(Settings.Default.RouteColor);
                                        (mRoute.Shape as System.Windows.Shapes.Path).Stroke = NewBrush;
                                        (mRoute.Shape as System.Windows.Shapes.Path).StrokeThickness = 2;
                                        mRoute.RegenerateShape(MainMap);
                                        MainMap.Markers.Add(mRoute);

                                        // C'est la première fois que l'on dessine l'itinéraire on zoom
                                        if (!newmroute)
                                        {
                                            ButtonWatcherMouseDown(null, null);
                                        }
                                        newmroute = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(DateTime.Now + " " + "Erreur dans MovePushpinWatcher: " + ex);
                                }

                                MovePushpin();
                            }
                        }

                        // Calcule la distance en ligne
                        dist = Convert.ToDouble((ss.Distance).Remove((ss.Distance).Length - 3));
                        Trace.WriteLine(DateTime.Now + " " + "Calcul distance en ligne: " + string.Format("{0:0.0}", dist) + " km");
                        return dist;
                    }
                }

                if (!(Settings.Default.DistanceAccuracy))
                {
                    // Calcule la distance hors ligne
                    double theta = long1 - long2;
                    dist = Math.Sin(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2)) +
                           Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(Deg2Rad(theta));
                    dist = Math.Acos(dist);
                    dist = Rad2Deg(dist);
                    dist = dist * 60 * 1.1515;
                    dist = dist * 1.609344;

                    if (Math.Round(dist, 1) > 0.0)
                    {
                        Trace.WriteLine(DateTime.Now + " " + "Calcul distance hors ligne: " + string.Format("{0:0.0}", dist) + " km");
                    }
                    return dist;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "CalculDistance " + ex);
            }
            return dist;
        }

        #endregion CalculDistance

        #region CalculBearing

        private double CalculBearing(double latitude2, double longitude2, double latitude1, double longitude1)
        {
            double lat1 = Deg2Rad(latitude1);
            double lat2 = Deg2Rad(latitude2);
            double dLon = Deg2Rad(longitude2 - longitude1);
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double brng = (Rad2Deg(Math.Atan2(y, x)) + 360) % 360;
            return brng;
        }

        #endregion CalculBearing

        #region Deg2Rad

        private static double Deg2Rad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        #endregion Deg2Rad

        #region Rad2Deg

        private static double Rad2Deg(double rad)
        {
            return rad / Math.PI * 180.0;
        }

        #endregion Rad2Deg

        #region ConvertKilometersToMiles

        public double ConvertKilometersToMiles(double kilometers)
        {
            return kilometers * 0.621371192;
        }

        #endregion ConvertKilometersToMiles

        #region XmlParser

        public static void XmlParser()
        {
            // Force l'acceptation du certificat SSL
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            if (CheckNet())
            {
                // La variable newVersion contient la version du fichier xml
                Version newVersion = null;
                XmlTextReader reader = null;

                try
                {
                    // Fourni l'url contenue dans le document xml
                    const string xmlUrl = "https://zguidetv.tuxfamily.org/geolock_version.xml";
                    reader = new XmlTextReader(xmlUrl);
                    reader.MoveToContent();

                    string elementName = null;
                    // On controle si le fichier xml contient le noeud "zguidetv"
                    if ((reader.NodeType == XmlNodeType.Element) && (string.Equals(reader.Name, "geolock", StringComparison.CurrentCulture)))
                    {
                        while (reader.Read())
                        {
                            // Quand on trouve un noeud on se souvient de son nom
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                elementName = reader.Name;
                            }
                            else
                            {
                                // noeud suivant
                                if ((reader.NodeType == XmlNodeType.Text) && (reader.HasValue))
                                {
                                    // On controle le nom des noeuds
                                    switch (elementName)
                                    {
                                        case "version":
                                            // On parse le num de version
                                            // dans le format :  xxx.xxx.xxx.xxx
                                            newVersion = new Version(reader.Value);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(DateTime.Now + " " + "XmlParser: " + ex);
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }

                // On regarde le numéro de la version actuelle
                Version curVersion = Assembly.GetExecutingAssembly().GetName().Version;

                // On compare les versions
                if (curVersion.CompareTo(newVersion) < 0)
                {
                    Option.GeolockUpdate = true;
                }
                else
                {
                    Option.GeolockUpdate = false;
                }

                reader.Close();
            }
            else
            {
                return;
            }
        }

        #endregion XmlParser

        #region CloseComport

        private static void CloseComport()
        {
            Trace.WriteLine(DateTime.Now + " " + "CloseComport");

            try
            {
                if (_spa == null)
                {
                    return;
                }

                if (_spa.IsOpen)
                {
                    _spa.DiscardInBuffer();
                    _spa.Close();
                    _spa = null;
                }
                else
                {
                    Trace.WriteLine(DateTime.Now + " " + "Le port n'est pas ouvert.");
                    return;
                }
            }
            catch (IOException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + ex);
            }
        }

        #endregion CloseComport

        #region Nmea2DecDeg

        private double Nmea2DecDeg(string NmeaLonLat)
        {
            //NmeaLonLat = NmeaLonLat.Replace(',', '.');

            int inx = NmeaLonLat.IndexOf(".");
            if (inx == -1)
            {
                return 0;    // Syntaxe invalide
            }

            double PosDb = Convert.ToDouble(NmeaLonLat.Replace(",", "."));
            double Deg = Convert.ToDouble(Math.Floor(PosDb / 100));
            double DecPos = Math.Round(Deg + ((PosDb - (Deg * 100)) / 60), 5);
            Console.WriteLine("degrees " + DecPos);
            return DecPos;
        }

        #endregion Nmea2DecDeg

        #region getChecksum

        private string GetChecksum(string sentence)
        {
            int checksum = Convert.ToByte(sentence[sentence.IndexOf('$') + 1]);

            // Boucle à travers tous les caractères de la trame afin d'obtenir la somme de contrôle
            for (int i = sentence.IndexOf('$') + 2; i < sentence.IndexOf('*'); i++)
            {
                checksum = checksum ^ Convert.ToByte(sentence[i]);
            }

            // Retourne le contrôle formaté en un nombre hexadécimal à deux caractères
            return checksum.ToString("X2");
        }

        #endregion getChecksum

        #region OpenComPort

        private void OpenComPort()
        {
            Trace.WriteLine(DateTime.Now + " " + "OpenComport");

            try
            {
                if (_spa != null)
                {
                    Trace.WriteLine(DateTime.Now + " On ouvre le port: " + _spa);
                    if (_spa.IsOpen)
                    {
                        return;
                    }
                }

                _spa = new SerialPort("COM" + Convert.ToString(Settings.Default.ComPort), Settings.Default.ComSpeed, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500
                };

                _spa.Open();
            }
            catch (IOException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "GPS ComPort Error: " + ex.ToString());
                PortError = true;
            }
        }

        #endregion OpenComPort

        #region TimerGpsScan

        private void TimerGpsScanTick(object sender, EventArgs e)
        {
            Trace.WriteLine("");
            Console.WriteLine(DateTime.Now + " " + "On entre dans TimerGpsScanTick");
            Trace.WriteLine(DateTime.Now + " " + "On entre dans TimerGpsScanTick");

            if (!_spa.IsOpen)
            {
                OpenComPort();
                ComPortException();
            }

            try
            {
                if (_spa.IsOpen)
                {
                    string data = _spa.ReadExisting();
                    string[] strArr = data.Split('$');

                    for (int i = 0; i < strArr.Length; i++)
                    {
                        string strTemp = strArr[i].Trim();

                        // Définition de la trame NMEA
                        string[] lineArr = strTemp.Split(',');

                        if (string.Equals(lineArr[0], "GPGGA", StringComparison.CurrentCulture))
                        {
                            // Vérification de la somme de contrôle (Checksum)
                            Trace.WriteLine(DateTime.Now + " " + "Trame GPGGA:" + " " + strArr[i].Remove(strArr[i].Length - 2));
                            string strTempChecksum = strTemp.Substring(strTemp.IndexOf("*", StringComparison.Ordinal) + 1);

                            string checksum = GetChecksum("$" + strTemp);
                            Trace.WriteLine(DateTime.Now + " " + "Control checksum:" + " " + checksum.ToString());

                            if (!(string.Equals(checksum, strTempChecksum, StringComparison.CurrentCulture)))
                            {
                                // La somme de contrôle n'est pas valide on passe à l'itération suivante
                                Trace.WriteLine(DateTime.Now + " " + "La somme de contrôle n'est pas valide on passe à l'itération suivante");
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        // Latitude et longitude actuelle.
                        if (lineArr[2] == "0" || lineArr[4] == "0")
                        {
                            Trace.WriteLine(DateTime.Now + " " + "latitude ou longitude est null");
                            return;
                        }

                        // Latitude
                        _latitude = Nmea2DecDeg((lineArr[2]));
                        Trace.WriteLine(DateTime.Now + " " + "Latitude: " + _latitude);
                        _latitudeindicateur = (lineArr[3]);

                        // Longitude
                        _longitude = Nmea2DecDeg((lineArr[4]));
                        Trace.WriteLine(DateTime.Now + " " + "Longitude: " + _longitude);
                        _longitudeindicateur = (lineArr[5]);

                        // Indicateur de qualité GPS (0=no fix, 1=GPS fix, 2=DGPS fix)
                        Console.WriteLine(DateTime.Now + " " + "Indicateur de qualité GPS: " + lineArr[6]);
                        Trace.WriteLine(DateTime.Now + " " + "Indicateur de qualité GPS: " + lineArr[6]);

                        // Nombre de satellites utilisé
                        Console.WriteLine(DateTime.Now + " " + "Nombre de satellites utilisé: " + lineArr[7]);
                        Trace.WriteLine(DateTime.Now + " " + "Nombre de satellites utilisé: " + lineArr[7]);

                        // Horizontal dilution of precision
                        Console.WriteLine(DateTime.Now + " " + "Horizontal dilution of precision: " + lineArr[8]);
                        Trace.WriteLine(DateTime.Now + " " + "Horizontal dilution of precision: " + lineArr[8]);

                        // Altitude
                        Console.WriteLine(DateTime.Now + " " + "Altitude au-dessus du niveau moyen de la mer: " + lineArr[9] + " m");
                        Trace.WriteLine(DateTime.Now + " " + "Altitude au-dessus du niveau moyen de la mer: " + lineArr[9] + " m");

                        // Contient la position actuelle
                        _latitudeChanged = _latitude;
                        _longitudeChanged = _longitude;

                        CurrentBearing();
                        MovePushpin();

                        if (_watcherState)
                        {
                            MainMap.Position = new PointLatLng(_latitudeChanged, _longitudeChanged);
                        }

                        _distance = CalculDistance(_latitudePushpinClicked, _longitudePushpinClicked, _latitudeChanged,
                            _longitudeChanged);

                        if (!(_distance > 15))
                        {
                            double miles = ConvertKilometersToMiles(_distance);

                            LabelDistance.Content = "Approx. Distance: " + string.Format("{0:0.0}", _distance) +
                                                    " km" + " - " +
                                                    string.Format("{0:0.0}", miles) + " mi";
                        }
                        else
                        {
                            LabelDistance.Content = "Approx. Distance: no data";
                        }

                        // Conversion UTC
                        if (!(lineArr[1] == null) && lineArr[1].Length > 6)
                        {
                            _utcTime = lineArr[1].Substring(0, 2) + ":" + lineArr[1].Substring(2, 2) + ":" + lineArr[1].Substring(4, 2);
                            LabelTimeStamp.Content = "TimeStamp: " + _utcTime + " UTC";
                        }
                        else
                        {
                            return;
                        }

                        // Vitesse de déplacement
                        DateTime test = Convert.ToDateTime(_utcTime);
                        TimeSpan duration = (DateTime.UtcNow - test);
                        int time_s = duration.Seconds;
                        double kph = ((_distance * 1000) / (time_s)) / 3600;
                        Trace.WriteLine(DateTime.Now + " " + "Vitesse moyenne: " + Math.Round(kph, 2) + " km/h - " + Math.Round(kph / 1.609, 2) + " mi/h");

                        return;
                    }
                }
            }
            catch (IOException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + ex);
                PortError = true;
                return;
            }

            PortError = false;
        }

        #endregion TimerGpsScan

        #region ComPortException

        private void ComPortException()
        {
            // Erreur lors de l'ouverture du ComPort et c'est la première fois
            if (PortError && FirstComError)
            {
                // On affiche le message d'erreur
                Message window = new Message
                {
                    ShowInTaskbar = false,
                    Topmost = true,
                    ResizeMode = ResizeMode.NoResize,
                    Owner = System.Windows.Application.Current.MainWindow,
                    Top = Top,
                    Left = Left
                };

                window.ShowDialog();
                PortError = false;
                FirstComError = false;
                return;
            }
            else if (PortError && !FirstComError)
            {
                // Erreur lors de l'ouverture du ComPort et ce n'est pas la première fois
                // on affiche pas le message
                PortError = false;
                return;
            }
            else if (!PortError && !FirstComError)
            {
                // On affiche le message en indiquant qu'il n'y a plus d'erreur
                Message window = new Message
                {
                    ShowInTaskbar = false,
                    Topmost = true,
                    ResizeMode = ResizeMode.NoResize,
                    Owner = System.Windows.Application.Current.MainWindow,
                    Top = Top,
                    Left = Left
                };

                window.ShowDialog();

                // Plus d'erreur du ComPort on remet le flag à true
                FirstComError = true;
            }
        }

        #endregion ComPortException

        #region InitialisationTraceListener

        private void InitialisationTraceListener()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(_tl);
            Trace.AutoFlush = true;
            Trace.WriteLine(DateTime.Now + " " + "InitialisationTraceListener");
        }

        #endregion InitialisationTraceListener

        #region CreateDirectory

        private void CreateDirectory()
        {
            try
            {
                string path = GetFolderPath(SpecialFolder.ApplicationData) + "\\brussels open data - wifi.brussels";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path + "\\log");
                    Directory.CreateDirectory(path + "\\json");
                    Directory.CreateDirectory(path + "\\profiles");
                    Trace.WriteLine("Répertoires Appdata Brussels Open Data - wifi.brussels créés avec succès");
                }

                string path1 = GetFolderPath(SpecialFolder.CommonApplicationData) + "\\Brussels Open Data - wifi.brussels";
                if (!Directory.Exists(path1))
                {
                    Directory.CreateDirectory(path1);
                }

                if (Directory.Exists(path1))
                {
                    Directory.CreateDirectory(path1 + "\\cache\\");
                    Trace.WriteLine("Répertoires ApplicationData Brussels Open Data - wifi.brussels créés avec succès");
                }
            }
            catch (IOException ex)
            {
                Trace.WriteLine("Répertoires Appdata Brussels Open Data - wifi.brussels créés sans succès: " + ex);
            }
        }

        #endregion CreateDirectory

        #region ProxyAuthentification

        public static void ProxyAuthentification()
        {
            try
            {
                Trace.WriteLine(DateTime.Now + " " + "Un proxy est configuré");
                GMapProvider.IsSocksProxy = Settings.Default.UseProxy;
                GMapProvider.WebProxy = new WebProxy(Settings.Default.ProxyAdress, Settings.Default.ProxyPort)
                {
                    Credentials = new NetworkCredential(Settings.Default.ProxyUsername, Settings.Default.ProxyPassword)
                };
            }
            catch (InvalidOperationException ex)
            {
                Trace.WriteLine(DateTime.Now + " " + "Erreur dans la configuration du proxy: " + ex);
            }
        }

        #endregion ProxyAuthentification

        #region MainMapPrefetch

        private void MainMapPrefetch(RectLatLng Selection, bool ZoomToFit)
        {
            RectLatLng area = Selection;
            if (!area.IsEmpty && MainMap.IsLoaded)
            {
                MainMap.SetZoomToFitRect(area);

                if (CheckNet())
                {
                    for (int i = MainMap.MinZoom; i <= MainMap.MaxZoom; i++)
                    {
                        TilePrefetcher Prefetch = new TilePrefetcher()
                        {
                            Title = "brussels open data - wifi.brussels",
                            Owner = System.Windows.Application.Current.MainWindow,
                            Top = Top,
                            Left = Left,
                            ShowCompleteMessage = false
                        };

                        Prefetch.Start(area, i, MainMap.MapProvider, 100);
                    }
                }

                MainMap.SelectedArea = RectLatLng.Empty;
            }
        }

        #endregion MainMapPrefetch

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