using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Home.Base.Services;
using HTCHome.Widgets;

namespace HTCHome.Services
{
    public interface ITrayIconService
    {
        void Initialize();
        void Dispose();
    }

    public class TrayIconService(WidgetManager widgetManager) : ITrayIconService, IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly WidgetManager _widgetManager = widgetManager;

        public void Initialize()
        {
            _notifyIcon = new NotifyIcon();
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    _notifyIcon.Icon = Icon.ExtractAssociatedIcon(entryAssembly.Location);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            _notifyIcon.Text = "HTC Home";
            _notifyIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();

            var addWidgetItem = new ToolStripMenuItem("Add Widget");

            var toggleItem = new ToolStripMenuItem("Show/Hide Widgets");
            toggleItem.Click += (s, e) => ToggleWidgets();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();

            contextMenu.Items.Add(toggleItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ToggleWidgets();
        }

        private void ToggleWidgets()
        {
            _widgetManager.ToggleWidgetsVisibility();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
