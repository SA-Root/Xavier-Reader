﻿using System;
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
            Window.Current.SizeChanged += ContentDialog_SizeChanged;
            if (gs.isDark)
            {
                RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                RequestedTheme = ElementTheme.Light;
            }
            ScrViewer.Height = Window.Current.Bounds.Height * 0.7;
            TempGS = gs;
            if (TempGS.LoadMode == EpubLoadMode.Full)
            {
                PerformanceOption.SelectedIndex = 0;
            }
            else if (TempGS.LoadMode == EpubLoadMode.PerChapter)
            {
                PerformanceOption.SelectedIndex = 1;
            }
            else if (TempGS.LoadMode == EpubLoadMode.Auto)
            {
                PerformanceOption.SelectedIndex = 2;
            }
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

        private void ContentDialog_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ScrViewer.Height = e.Size.Height * 0.7;
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

        private void PerformanceOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PerformanceOption.SelectedIndex == 0)
            {
                TempGS.LoadMode = EpubLoadMode.Full;
            }
            else if (PerformanceOption.SelectedIndex == 1)
            {
                TempGS.LoadMode = EpubLoadMode.PerChapter;
            }
            else if (PerformanceOption.SelectedIndex == 2)
            {
                TempGS.LoadMode = EpubLoadMode.Auto;
            }
        }

        private void ContentDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= ContentDialog_SizeChanged;
        }
    }
}
