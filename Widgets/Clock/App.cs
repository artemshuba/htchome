using Home.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;

namespace Clock
{
    class App
    {
        public static Settings Settings;
        public static string ConfigFile;

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //private NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;

        public static Domain.WeatherProviderManager WpManager;

        //public static DispatcherTimer UpdateTimer;

        public static SoundPlayer SoundPlayer;

        private void ApplicationStartup()
        {
            //#if DEBUG
            //            AllocConsole();
            //#endif
            logger.Info("App started");
            logger.Info("HTC Home version: " + E.VersionString);
            logger.Info("Widget version:" + Assembly.GetExecutingAssembly().GetName().Version);

            //if (e.Args.Contains("-c") || e.Args.Contains("/c"))
            //{
            //    ConfigFile = e.Args.Single(x => x.EndsWith(".config"));
            //    logger.Info("Using custom config: " + ConfigFile);
            //}

            //try
            //{
            //    //check if we must run as administrator
            //    if (!Directory.Exists(E.Root + "\\Temp"))
            //        Directory.CreateDirectory(E.Root + "\\Temp");
            //    Directory.Delete(E.Root + "\\Temp");
            //}
            //catch (UnauthorizedAccessException)
            //{
            //    logger.Warn("Admin privilegies are required. Restarting as admin.");
            //    var p = new ProcessStartInfo { Verb = "runas", FileName = Assembly.GetExecutingAssembly().Location };
            //    Process.Start(p);
            //    Shutdown();
            //}

            //var fileInfo = new FileInfo(E.Root + "\\Home.Base.dll");
            //if (DateTime.Now.CompareTo(fileInfo.LastWriteTimeUtc.AddMonths(3)) >= 0)
            //{
            //    logger.Error("This version has expired.");
            //    MessageBox.Show(Clock.Properties.Resources.Expired, "HTC Home 3 Expired", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
            if (string.IsNullOrEmpty(ConfigFile))
                ConfigFile = E.Root + "\\Clock.config";
            Settings = (Settings)XmlSerializable.Load(typeof(Settings), ConfigFile) ?? new Settings();
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(Settings.Language);
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(Settings.Language);

            if (Settings.UseSoftwareRendering)
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            if (Settings.UseProxy)
            {
                var proxy = new WebProxy();
                if (string.IsNullOrEmpty(Settings.ProxyAddress))
                    GeneralHelper.Proxy = WebRequest.GetSystemWebProxy();
                else
                {
                    proxy.Address = new Uri(Settings.ProxyAddress + ":" + Settings.ProxyPort);
                    proxy.Credentials = new NetworkCredential(Settings.ProxyUsername, Settings.ProxyPassword);
                    GeneralHelper.Proxy = proxy;
                }
            }

            //UpdateTimer = new DispatcherTimer();

            //if (Settings.CheckForUpdates && File.Exists(E.Root + "\\Update.exe"))
            //{
            //    var a = Settings.Language;
            //    if (Settings.SilentUpdate)
            //        a += " /silent";
            //    Process.Start(E.Root + "\\Update.exe", a);

            //    if (App.Settings.UpdateInterval > 0)
            //    {
            //        UpdateTimer.Interval = TimeSpan.FromMinutes(App.Settings.UpdateInterval);
            //        UpdateTimer.Tick += UpdateTimerTick;
            //        UpdateTimer.Start();
            //    }
            //}

            if (ExtrasManager.IsExtrasInstalled("4fe4eee3-6480-4007-b1ba-2c5d1d86386d"))
            {
                SoundPlayer = new SoundPlayer();
            }

            //switch (Settings.Style)
            //{
            //    case Styles.FlipClock:
            //        StartupUri = new Uri("/Windows/FlipClock.xaml", UriKind.Relative);
            //        //FlipClockSettings = (FlipClockSettings)XmlSerializable.Load(typeof(FlipClockSettings), E.Root + "\\Clock.FlipClock.config") ?? new FlipClockSettings();
            //        WpManager = new WeatherProviderManager();
            //        WpManager.FindProviders();
            //        break;
            //    case Styles.AnalogClockBig:
            //        StartupUri = new Uri("/Windows/AnalogClockBig.xaml", UriKind.Relative);
            //        break;
            //    case Styles.AnalogClockSmall:
            //        StartupUri = new Uri("/Windows/AnalogClockSmall.xaml", UriKind.Relative);
            //        break;
            //    case Styles.DualClock:
            //        StartupUri = new Uri("/Windows/DualClock.xaml", UriKind.Relative);
            //        break;
            //    case Styles.ModernFlipClock:
            //        StartupUri = new Uri("/Windows/ModernFlipClock.xaml", UriKind.Relative);
            //        WpManager = new WeatherProviderManager();
            //        WpManager.FindProviders();
            //        break;
            //    case Styles.WideFlipClock:
            //        StartupUri = new Uri("/Windows/WideFlipClock.xaml", UriKind.Relative);
            //        WpManager = new WeatherProviderManager();
            //        WpManager.FindProviders();
            //        break;
            //    case Styles.FlipClockNoWeather:
            //        StartupUri = new Uri("/Windows/FlipClockNoWeather.xaml", UriKind.Relative);
            //        break;
            //}

            //if (e.Args.Length > 0)
            //{
            //    var extensions = from x in e.Args
            //                     where x.EndsWith(".hhpack") && File.Exists(x)
            //                     select x;
            //    if (extensions.Count() > 0)
            //    {
            //        var packageManager = new PackageManager();
            //        foreach (var ext in extensions)
            //        {
            //            packageManager.BeginUnpack(ext, E.Root);
            //        }
            //    }
            //}

            //if (Settings.UseTrayIcon)
            //{
            //    if (!e.Args.Contains("-noicon"))
            //    {
            //        AddTrayIcon();
            //    }

            //}
            //else
            //{
            //    var jumpList = new JumpList();
            //    JumpList.SetJumpList(this, jumpList);

            //    if (File.Exists(E.Root + "\\Weather.exe"))
            //    {
            //        var weatherTask = new JumpTask();
            //        weatherTask.Title = Clock.Properties.Resources.JumpListWeatherWidget;
            //        weatherTask.WorkingDirectory = E.Root;
            //        weatherTask.CustomCategory = Clock.Properties.Resources.JumpListWidgets;
            //        weatherTask.ApplicationPath = E.Root + "\\Weather.exe";
            //        weatherTask.IconResourcePath = E.Root + "\\Weather.exe";
            //        weatherTask.IconResourceIndex = 0;

            //        jumpList.JumpItems.Add(weatherTask);
            //    }


            //    if (File.Exists(E.Root + "\\Photos.exe"))
            //    {
            //        var photosTask = new JumpTask();
            //        photosTask.Title = Clock.Properties.Resources.JumpListPhotosWidget;
            //        photosTask.WorkingDirectory = E.Root;
            //        photosTask.CustomCategory = Clock.Properties.Resources.JumpListWidgets;
            //        photosTask.ApplicationPath = E.Root + "\\Photos.exe";
            //        photosTask.IconResourcePath = E.Root + "\\Photos.exe";
            //        photosTask.IconResourceIndex = 0;

            //        jumpList.JumpItems.Add(photosTask);
            //    }

            //    if (File.Exists(E.Root + "\\News.exe"))
            //    {
            //        var newsTask = new JumpTask();
            //        newsTask.Title = Clock.Properties.Resources.JumpListNewsWidget;
            //        newsTask.WorkingDirectory = E.Root;
            //        newsTask.CustomCategory = Clock.Properties.Resources.JumpListWidgets;
            //        newsTask.ApplicationPath = E.Root + "\\News.exe";
            //        newsTask.IconResourcePath = E.Root + "\\News.exe";
            //        newsTask.IconResourceIndex = 0;

            //        jumpList.JumpItems.Add(newsTask);
            //    }

            //    if (File.Exists(E.Root + "\\FriendStream.exe"))
            //    {
            //        var friendsTask = new JumpTask();
            //        friendsTask.Title = Clock.Properties.Resources.JumpListFriendStreamWidget;
            //        friendsTask.WorkingDirectory = E.Root;
            //        friendsTask.CustomCategory = Clock.Properties.Resources.JumpListWidgets;
            //        friendsTask.ApplicationPath = E.Root + "\\FriendStream.exe";
            //        friendsTask.IconResourcePath = E.Root + "\\FriendStream.exe";
            //        friendsTask.IconResourceIndex = 0;

            //        jumpList.JumpItems.Add(friendsTask);
            //    }

            //    if (File.Exists(E.Root + "\\Calendar.exe"))
            //    {
            //        var calendarTask = new JumpTask();
            //        calendarTask.Title = Clock.Properties.Resources.JumpListCalendarWidget;
            //        calendarTask.WorkingDirectory = E.Root;
            //        calendarTask.CustomCategory = Clock.Properties.Resources.JumpListWidgets;
            //        calendarTask.ApplicationPath = E.Root + "\\Calendar.exe";
            //        calendarTask.IconResourcePath = E.Root + "\\Calendar.exe";
            //        calendarTask.IconResourceIndex = 0;

            //        jumpList.JumpItems.Add(calendarTask);
            //    }

            //    jumpList.Apply();
            //}
        }

        //void UpdateTimerTick(object sender, EventArgs e)
        //{
        //    if (File.Exists(E.Root + "\\Update.exe"))
        //    {
        //        var a = Settings.Language;
        //        if (Settings.SilentUpdate)
        //            a += " /silent";
        //        Process.Start(E.Root + "\\Update.exe", a);
        //    }
        //}

        //public void AddTrayIconNew()
        //{
        //    if (trayIcon != null)
        //    {
        //        return;
        //    }
        //    var trayMenu = new System.Windows.Forms.ContextMenuStrip();
        //    trayIcon = new NotifyIcon
        //    {
        //        Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
        //        Text = "HTC Home 3"
        //    };
        //    //trayIcon.MouseClick += (s,e) => { if (e.Button == MouseButtons.Right) trayMenu. };
        //    trayIcon.ContextMenuStrip = trayMenu;
        //    trayIcon.Visible = true;

        //    var closeItem = new System.Windows.Forms.ToolStripMenuItem();
        //    closeItem.Text = Clock.Properties.Resources.CloseItem;
        //    closeItem.Click += new EventHandler(closeItem_Click);
        //    trayMenu.Items.Add(closeItem);
        //}

        //void closeItem_Click(object sender, EventArgs e)
        //{
        //    foreach (Window window in Windows)
        //    {
        //        window.Close();
        //    }
        //}

        //public void AddTrayIcon()
        //{
        //    if (trayIcon != null)
        //    {
        //        return;
        //    }
        //    trayMenu = new System.Windows.Forms.ContextMenuStrip();

        //    trayIcon = new NotifyIcon
        //    {
        //        Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
        //        Text = "HTC Home Apis"
        //    };
        //    trayIcon.MouseClick += TrayIconMouseClick;
        //    trayIcon.Visible = true;
        //    trayIcon.ContextMenuStrip = trayMenu;

        //    var closeItem = new System.Windows.Forms.ToolStripMenuItem();
        //    closeItem.Text = Clock.Properties.Resources.CloseItem;
        //    closeItem.Click += (s, e) =>
        //    {
        //        foreach (Window window in Windows)
        //        {
        //            window.Close();
        //        }
        //    };
        //    trayMenu.Items.Add(closeItem);

        //    var widgetsItem = new System.Windows.Forms.ToolStripMenuItem();
        //    widgetsItem.Text = Clock.Properties.Resources.JumpListWidgets;

        //    trayMenu.Items.Add(widgetsItem);
        //    widgetsItem.Visible = false;

        //    if (File.Exists(E.Root + "\\Weather.exe"))
        //    {
        //        widgetsItem.Visible = true;
        //        var weatherItem = new System.Windows.Forms.ToolStripMenuItem();
        //        weatherItem.Text = Clock.Properties.Resources.JumpListWeatherWidget;
        //        weatherItem.Click += (s, e) => Process.Start(E.Root + "\\Weather.exe");

        //        widgetsItem.DropDownItems.Add(weatherItem);
        //    }

        //    if (File.Exists(E.Root + "\\Photos.exe"))
        //    {
        //        widgetsItem.Visible = true;
        //        var photosItem = new System.Windows.Forms.ToolStripMenuItem();
        //        photosItem.Text = Clock.Properties.Resources.JumpListPhotosWidget;
        //        photosItem.Click += (s, e) => Process.Start(E.Root + "\\Photos.exe");

        //        widgetsItem.DropDownItems.Add(photosItem);
        //    }

        //    if (File.Exists(E.Root + "\\News.exe"))
        //    {
        //        widgetsItem.Visible = true;
        //        var newsItem = new System.Windows.Forms.ToolStripMenuItem();
        //        newsItem.Text = Clock.Properties.Resources.JumpListNewsWidget;
        //        newsItem.Click += (s, e) => Process.Start(E.Root + "\\News.exe");

        //        widgetsItem.DropDownItems.Add(newsItem);
        //    }

        //    if (File.Exists(E.Root + "\\FriendStream.exe"))
        //    {
        //        widgetsItem.Visible = true;
        //        var friendsItem = new System.Windows.Forms.ToolStripMenuItem();
        //        friendsItem.Text = Clock.Properties.Resources.JumpListFriendStreamWidget;
        //        friendsItem.Click += (s, e) => Process.Start(E.Root + "\\FriendStream.exe");

        //        widgetsItem.DropDownItems.Add(friendsItem);
        //    }

        //    if (File.Exists(E.Root + "\\Calendar.exe"))
        //    {
        //        widgetsItem.Visible = true;
        //        var calendarItem = new System.Windows.Forms.ToolStripMenuItem();
        //        calendarItem.Text = Clock.Properties.Resources.JumpListCalendarWidget;
        //        calendarItem.Click += (s, e) => Process.Start(E.Root + "\\Calendar.exe");

        //        widgetsItem.DropDownItems.Add(calendarItem);
        //    }
        //}

        //void TrayIconMouseClick(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        foreach (Window w in Application.Current.Windows)
        //        {
        //            w.Activate();
        //        }
        //    }
        //    //else
        //    //{
        //    //    trayMenu.StaysOpen = false;
        //    //    trayMenu.IsOpen = true;
        //    //}
        //}

        //public void RemoveTrayIcon()
        //{
        //    if (trayIcon != null)
        //    {
        //        trayIcon.MouseClick -= TrayIconMouseClick;
        //        trayIcon.Visible = false;
        //        trayIcon.Dispose();
        //        trayIcon = null;
        //    }
        //}

        //private void ApplicationExit(object sender, ExitEventArgs e)
        //{
        //    //#if DEBUG
        //    //            FreeConsole();
        //    //#endif
        //    RemoveTrayIcon();
        //    logger.Info("App closed");
        //}

        //private static void AllocConsole()
        //{
        //    WinAPI.AllocConsole();
        //}

        //private static void FreeConsole()
        //{
        //    WinAPI.FreeConsole();
        //}

        private void ApplicationDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            logger.FatalException("An unhandled exception occured", e.Exception);
        }
    }
}
