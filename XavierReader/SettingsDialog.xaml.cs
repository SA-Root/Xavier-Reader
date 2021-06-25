using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.IO.Compression;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Documents;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Activation;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace XavierReader
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public static event Action<GlobalSettings> SaveSettingsEvent;
        private GlobalSettings TempGS { get; set; }
        public SettingsDialog(GlobalSettings gs)
        {
            this.InitializeComponent();
            if (gs.isDark)
            {
                RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                RequestedTheme = ElementTheme.Light;
            }
            ScrViewer.Height = Window.Current.Bounds.Height * 0.75;
            TempGS = gs;
            if (TempGS.isAutoDark)
            {
                AutoDarkStartTime.Visibility = Visibility.Visible;
                AutoDarkEndTime.Visibility = Visibility.Visible;
            }
            else
            {
                AutoDarkStartTime.Visibility = Visibility.Collapsed;
                AutoDarkEndTime.Visibility = Visibility.Collapsed;
            }
            AutoDarkStartTime.Time = TempGS.AutoDarkStartTime;
            AutoDarkEndTime.Time = TempGS.AutoDarkEndTime;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SaveSettingsEvent(TempGS);
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(100);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ScrViewer.Height = Window.Current.Bounds.Height * 0.75;
                });
            });
        }

        private void isAutoDark_Toggled(object sender, RoutedEventArgs e)
        {
            if (isAutoDark.IsOn)
            {
                AutoDarkStartTime.Visibility = Visibility.Visible;
                AutoDarkEndTime.Visibility = Visibility.Visible;
            }
            else
            {
                AutoDarkStartTime.Visibility = Visibility.Collapsed;
                AutoDarkEndTime.Visibility = Visibility.Collapsed;
            }
        }
    }
}
