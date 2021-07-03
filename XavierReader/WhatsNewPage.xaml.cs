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

namespace XavierReader
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class WhatsNewPage : Page
    {
        private string Version = "Version: 2.2.93";
        public WhatsNewPage()
        {
            this.InitializeComponent();
            ScrViewer.Height = Window.Current.Bounds.Height * 0.5;
        }
        public void Page_SizeChanged()
        {
            Task.Run(async () =>
            {
                Thread.Sleep(50);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ScrViewer.Height = Window.Current.Bounds.Height * 0.5;
                });
            });
        }
    }
}
