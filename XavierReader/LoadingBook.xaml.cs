using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Threading.Tasks;
using System.Threading;

namespace XavierReader
{
    public sealed partial class LoadingBook : ContentDialog
    {
        public LoadingBook(bool isDark)
        {
            this.InitializeComponent();
            if (isDark)
            {
                RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                RequestedTheme = ElementTheme.Light;
            }
        }
        private Task CreateBook() =>
            Task.Run(() =>
            {
                Thread.Sleep(200);
            }
            );
        public async Task ShowAsync(XavierEpubFile epub, StorageFile sf)
        {
            var ret = base.ShowAsync();
            await CreateBook();
            try
            {
                await epub.LoadBook(sf);
            }
            catch (EpubExtractionFailureException)
            {
                txt.Text = "Failed to load book.Cleaning up...";
                epub.CleanUp();
                Thread.Sleep(2000);
                return;
            }
            catch (EpubContentLoadFailureException)
            {
                txt.Text = "Failed to load book.Cleaning up...";
                epub.CleanUp();
                Thread.Sleep(2000);
                return;
            }
            return;
        }
        public async Task ShowAsync(XavierEpubFile epub, string str)
        {
            var ret = base.ShowAsync();
            await CreateBook();
            try
            {
                epub.LoadBook(str);
            }
            catch (EpubExtractionFailureException)
            {
                txt.Text = "Failed to load book.Cleaning up...";
                epub.CleanUp();
                Thread.Sleep(2000);
                return;
            }
            catch (EpubContentLoadFailureException)
            {
                txt.Text = "Failed to load book.Cleaning up...";
                epub.CleanUp();
                Thread.Sleep(2000);
                return;
            }
            return;
        }
    }
}
