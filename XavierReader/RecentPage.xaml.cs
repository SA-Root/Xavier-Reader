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
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Media.Playback;
using Windows.UI;
using System.Xml;
using System.Diagnostics;
using System.Globalization;

namespace XavierReader
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class RecentPage : Page
    {
        public static event Action<string> OpenRecentBookEvent;
        public static event Func<bool> QueryAppThemeEvent;
        //All recent books
        private List<BookImage> AllBooks { get; set; }
        private async void LoadRecent()
        {
            var CurrentBooks = new ObservableCollection<BookImage>();
            var dir = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path + "/tmp");
            if (!dir.Exists)
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                await storageFolder.CreateFolderAsync("tmp");
                dir = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path + "/tmp");
            }
            else
            {
                AllBooks = new List<BookImage>();
                foreach (var f in dir.GetDirectories())
                {
                    var bi = new BookImage
                    {
                        ImageLocation = f.FullName + "/cover_image.jpg",
                        TmpFolderName = f.Name
                    };
                    var settings = ApplicationData.Current.LocalSettings;
                    if (!settings.Values.ContainsKey(bi.TmpFolderName + ".LastReadTime"))
                    {
                        bi.LastReadTime = DateTime.Now.ToString("g", new CultureInfo("en-US"));
                        settings.Values[bi.TmpFolderName + ".LastReadTime"] = DateTime.Now.ToString("g", new CultureInfo("en-US"));
                    }
                    else
                    {
                        bi.LastReadTime = settings.Values[bi.TmpFolderName + ".LastReadTime"].ToString();
                    }
                    if (settings.Values.ContainsKey(bi.TmpFolderName + ".Chapters"))
                    {
                        var cur = int.Parse(settings.Values[bi.TmpFolderName + ".Chapter"].ToString());
                        var all = int.Parse(settings.Values[bi.TmpFolderName + ".Chapters"].ToString());
                        bi.Progress = cur * 100.0 / all;
                    }
                    if (settings.Values.ContainsKey(bi.TmpFolderName + ".Author"))
                    {
                        bi.Author = settings.Values[bi.TmpFolderName + ".Author"].ToString();
                    }
                    if (settings.Values.ContainsKey(bi.TmpFolderName + ".Rating"))
                    {
                        bi.Rating = settings.Values[bi.TmpFolderName + ".Rating"].ToString();
                    }
                    AllBooks.Add(bi);
                }
                var t = AllBooks.OrderByDescending(item => DateTime.ParseExact(item.LastReadTime, "g", new CultureInfo("en-US")));
                foreach (var i in t)
                {
                    CurrentBooks.Add(i);
                }
            }
            ImageGrid.ItemsSource = CurrentBooks;
        }
        public RecentPage()
        {
            this.InitializeComponent();
            LoadRecent();
        }
        private void ImageGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var book = e.ClickedItem as BookImage;
            OpenRecentBookEvent(book.TmpFolderName);
        }
        private void ManageBooksButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteBooksButton.IsEnabled = false;
            if (ImageGrid.SelectionMode == ListViewSelectionMode.None)
            {
                ImageGrid.Background = new SolidColorBrush(Color.FromArgb(255, 170, 170, 170));
                ImageGrid.IsItemClickEnabled = false;
                DeleteBooksButton.Visibility = Visibility.Visible;
                ImageGrid.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                ImageGrid.Background = null;
                ImageGrid.SelectionMode = ListViewSelectionMode.None;
                DeleteBooksButton.Visibility = Visibility.Collapsed;
                ImageGrid.IsItemClickEnabled = true;
            }
            
        }
        private async void DeleteBooksButton_Click(object sender, RoutedEventArgs e)
        {
            var cfm = new MessageDialog(new MsgParam
            {
                Title = "Attention!",
                MsgContent = "Delete your book?",
                isDark = QueryAppThemeEvent()
            });
            var result = await cfm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var dia = new Loading("Deleting books...", RequestedTheme == ElementTheme.Dark);
                dia.ShowAsync();
                List<string> tmp = new List<string>();
                foreach (BookImage bi in ImageGrid.SelectedItems)
                {
                    tmp.Add(bi.TmpFolderName);
                    var settings = ApplicationData.Current.LocalSettings;
                    var local = ApplicationData.Current.LocalFolder;
                    var localtmp = await local.GetFolderAsync("tmp");
                    var title = settings.Values[bi.TmpFolderName]?.ToString();
                    if (title != null)
                    {
                        settings.Values.Remove(bi.TmpFolderName);
                        settings.Values.Remove(title);
                        settings.Values.Remove(bi.TmpFolderName + ".Chapter");
                        settings.Values.Remove(bi.TmpFolderName + ".Chapters");
                        settings.Values.Remove(bi.TmpFolderName + ".Author");
                        settings.Values.Remove(bi.TmpFolderName + ".Rating");
                        settings.Values.Remove(bi.TmpFolderName + ".ChapterProgress");
                    }
                    var fileToDelete = await local.GetFileAsync(bi.TmpFolderName);
                    await fileToDelete.DeleteAsync();
                    var folderToDelete = await localtmp.GetFolderAsync(bi.TmpFolderName);
                    await folderToDelete.DeleteAsync();
                }
                foreach (var s in tmp)
                {
                    for (int i = 0; i < AllBooks.Count; ++i)
                    {
                        if (s == AllBooks[i].TmpFolderName)
                        {
                            AllBooks.RemoveAt(i);
                            break;
                        }
                    }
                }
                ReloadBooks();
                dia.Hide();
            }
            ImageGrid.Background = null;
            ImageGrid.SelectionMode = ListViewSelectionMode.None;
            DeleteBooksButton.Visibility = Visibility.Collapsed;
            ImageGrid.IsItemClickEnabled = true;
        }
        private void FilterFlyout_Closed(object sender, object e)
        {
        }
        private void ImageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageGrid.SelectedItems.Count > 0)
            {
                DeleteBooksButton.IsEnabled = true;
            }
            else
            {
                DeleteBooksButton.IsEnabled = false;
            }
        }
        private void AllRatingFlyout_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<BookImage> newBooks;
            if (AllRatingFlyout.IsChecked == true)
            {
                PG13Flyout.IsChecked = true;
                PGFlyout.IsChecked = true;
                GFlyout.IsChecked = true;
                NC17Flyout.IsChecked = true;
                RFlyout.IsChecked = true;
                newBooks = new ObservableCollection<BookImage>();
                foreach (var i in AllBooks)
                {
                    newBooks.Add(i);
                }
            }
            else
            {
                PG13Flyout.IsChecked = false;
                PGFlyout.IsChecked = false;
                GFlyout.IsChecked = false;
                NC17Flyout.IsChecked = false;
                RFlyout.IsChecked = false;
                newBooks = null;
            }
            ImageGrid.ItemsSource = newBooks?.OrderByDescending(item => DateTime.ParseExact(item.LastReadTime, "g", new CultureInfo("en-US")));
        }
        private void ReloadBooks()
        {
            var newBooks = new ObservableCollection<BookImage>();
            if (NC17Flyout.IsChecked == true)
            {
                var tmp = from c in AllBooks
                          where c.Rating == "NC17"
                          select c;
                var nc17 = tmp.ToList();
                foreach (var i in nc17)
                {
                    newBooks.Add(i);
                }
            }
            if (GFlyout.IsChecked == true)
            {
                var tmp = from c in AllBooks
                          where c.Rating == "G"
                          select c;
                var g = tmp.ToList();
                foreach (var i in g)
                {
                    newBooks.Add(i);
                }
            }
            if (PGFlyout.IsChecked == true)
            {
                var tmp = from c in AllBooks
                          where c.Rating == "PG"
                          select c;
                var pg = tmp.ToList();
                foreach (var i in pg)
                {
                    newBooks.Add(i);
                }
            }
            if (RFlyout.IsChecked == true)
            {
                var tmp = from c in AllBooks
                          where c.Rating == "R"
                          select c;
                var r = tmp.ToList();
                foreach (var i in r)
                {
                    newBooks.Add(i);
                }
            }
            if (PG13Flyout.IsChecked == true)
            {
                var tmp = from c in AllBooks
                          where c.Rating == "PG13"
                          select c;
                var pg13 = tmp.ToList();
                foreach (var i in pg13)
                {
                    newBooks.Add(i);
                }
            }
            if (GFlyout.IsChecked &&
                PG13Flyout.IsChecked &&
                PGFlyout.IsChecked &&
                NC17Flyout.IsChecked &&
                RFlyout.IsChecked)
            {
                AllRatingFlyout.IsChecked = true;
            }
            else
            {
                AllRatingFlyout.IsChecked = false;
            }
            ImageGrid.ItemsSource = newBooks.OrderByDescending(item => DateTime.ParseExact(item.LastReadTime, "g", new CultureInfo("en-US")));
        }
        private void NC17Flyout_Click(object sender, RoutedEventArgs e)
        {
            ReloadBooks();
        }
    }
}
