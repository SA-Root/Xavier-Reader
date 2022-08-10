using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.UI;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;

namespace XavierReader
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class RecentPage : Page
    {
        public static event Action<XavierEpubFile> OpenRecentBookEvent;
        //All recent books
        private List<BookImage> AllBooks { get; set; }
        private GlobalSettings Settings { get; set; }
        private BookImage DetailPageBook { get; set; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Settings = e.Parameter as GlobalSettings;
            if (Settings.RatingFilter.Contains("PG13"))
            {
                PG13Flyout.IsChecked = true;
            }
            else
            {
                PG13Flyout.IsChecked = false;
            }
            if (Settings.RatingFilter.Contains("G"))
            {
                GFlyout.IsChecked = true;
            }
            else
            {
                GFlyout.IsChecked = false;
            }
            if (Settings.RatingFilter.Contains("PG"))
            {
                PGFlyout.IsChecked = true;
            }
            else
            {
                PGFlyout.IsChecked = false;
            }
            if (Settings.RatingFilter.Contains("R"))
            {
                RFlyout.IsChecked = true;
            }
            else
            {
                RFlyout.IsChecked = false;
            }
            if (Settings.RatingFilter.Contains("NC17"))
            {
                NC17Flyout.IsChecked = true;
            }
            else
            {
                NC17Flyout.IsChecked = false;
            }
            LoadRecent();
        }
        private async void LoadRecent()
        {
            AllBooks = new List<BookImage>();
            var dir = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path + "/tmp");
            if (!dir.Exists)
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                await storageFolder.CreateFolderAsync("tmp");
                dir = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path + "/tmp");
            }
            else
            {
                foreach (var f in dir.GetDirectories())
                {
                    if(Directory.Exists(f.FullName + "/cover_image.jpg"))
                    {
                        var bi = new BookImage
                        {
                            ImageLocation = f.FullName + "/cover_image.jpg",
                            TmpFolderName = f.Name
                        };
                        bi.Settings = new BookSettings(bi.TmpFolderName);
                        bi.Settings.LoadSettings();
                        bi.Progress = (bi.Settings.CurrentChapter + bi.Settings.ChapterProgress) / bi.Settings.Chapters * 100.0;
                        AllBooks.Add(bi);
                    }
                    else
                    {
                        var bi = new BookImage
                        {
                            ImageLocation = f.FullName + "/cover.jpeg",
                            TmpFolderName = f.Name
                        };
                        bi.Settings = new BookSettings(bi.TmpFolderName);
                        bi.Settings.LoadSettings();
                        bi.Progress = (bi.Settings.CurrentChapter + bi.Settings.ChapterProgress) / bi.Settings.Chapters * 100.0;
                        AllBooks.Add(bi);
                    }
                }
                var t = AllBooks.OrderByDescending(item => item.Settings.LastReadTime);

            }
            ReloadBooks();
        }
        public RecentPage()
        {
            this.InitializeComponent();
        }
        private void ImageGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            DetailPageBook = e.ClickedItem as BookImage;
            var book = new XavierEpubFile()
            {
                LoadMode = EpubLoadMode.InfoOnly
            };
            ConnectedAnimation animation = null;
            var width = Window.Current.Bounds.Width;
            var height = Window.Current.Bounds.Height;
            SmokeGridContent.Width = width * 0.6;
            SmokeGridContent.Height = height * 0.6;
            SmokeGrid.Visibility = Visibility.Visible;
            if (ImageGrid.ContainerFromItem(e.ClickedItem) is GridViewItem container)
            {
                var _storedItem = container.Content as BookImage;
                animation = ImageGrid.PrepareConnectedAnimation("forwardAnimation", _storedItem, "ConnectedImage");
            }
            animation.TryStart(DetailPageImage);
            book.LoadBook(DetailPageBook.TmpFolderName);
            DetailPageLoadingGrid.Visibility = Visibility.Collapsed;
            DetailPageImage.Source = new BitmapImage(new Uri(DetailPageBook.ImageLocation));
            DetailPageTitle.Text = book.Title;
            ToolTipService.SetToolTip(DetailPageTitle, new ToolTip() { Content = book.Title });
            DetailPageAuthor.Text = book.Author;
            ToolTipService.SetToolTip(DetailPageAuthor, new ToolTip() { Content = book.Author });
            DetailPageRating.Text = book.Rating.ToString();
            ToolTipService.SetToolTip(DetailPageRating, new ToolTip() { Content = book.Rating.ToString() });
            DetailPageLastUpdateTime.Text = book.LastUpdateTime.ToString("g", new CultureInfo("en-US"));
            ToolTipService.SetToolTip(DetailPageLastUpdateTime, new ToolTip() { Content = DetailPageLastUpdateTime.Text });
            DetailPageTotalChapters.Text = book.TotalChapters.ToString();
            DetailPageProgress.Text = string.Format("{0:f2}%", (book.CurrentChapter + book.ChapterProgress) / book.TotalChapters * 100);
            DetailPageReadBookButton.IsEnabled = true;
        }
        private void ManageBooksButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteBooksButton.IsEnabled = false;
            if (ImageGrid.SelectionMode == ListViewSelectionMode.None)
            {
                ImageGridTop.Background = new SolidColorBrush(Color.FromArgb(255, 170, 170, 170));
                ImageGrid.IsItemClickEnabled = false;
                DeleteBooksButton.Visibility = Visibility.Visible;
                ImageGrid.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                ImageGridTop.Background = null;
                ImageGrid.SelectionMode = ListViewSelectionMode.None;
                DeleteBooksButton.Visibility = Visibility.Collapsed;
                ImageGrid.IsItemClickEnabled = true;
            }
        }
        private Task CreateBook() =>
            Task.Run(() =>
            {
                Thread.Sleep(100);
            }
            );
        private async void DeleteBooksButton_Click(object sender, RoutedEventArgs e)
        {
            var cfm = new MessageDialog(new MsgParam
            {
                Title = "Attention!",
                MsgContent = "Delete your book?",
                isDark = Settings.isDark
            });
            var result = await cfm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var dia = new DeleteBook("Deleting books...", Settings.isDark);
                dia.ShowAsync();
                await CreateBook();
                List<BookImage> tmp = new List<BookImage>();
                foreach (BookImage bi in ImageGrid.SelectedItems)
                {
                    tmp.Add(bi);
                    bi.Settings.RemoveSettings();
                    var local = ApplicationData.Current.LocalFolder;
                    var localtmp = await local.GetFolderAsync("tmp");
                    var fileToDelete = await local.GetFileAsync(bi.TmpFolderName);
                    await fileToDelete.DeleteAsync();
                    var folderToDelete = await localtmp.GetFolderAsync(bi.TmpFolderName);
                    await folderToDelete.DeleteAsync();
                }
                foreach (var s in tmp)
                {
                    AllBooks.Remove(s);
                }
                ReloadBooks();
                dia.Hide();
                ImageGridTop.Background = null;
                ImageGrid.SelectionMode = ListViewSelectionMode.None;
                DeleteBooksButton.Visibility = Visibility.Collapsed;
                ImageGrid.IsItemClickEnabled = true;
            }
            else
            {
                ImageGrid.SelectionMode = ListViewSelectionMode.None;
                ImageGrid.SelectionMode = ListViewSelectionMode.Multiple;
            }
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
            Settings.RatingFilter.Add("PG");
            Settings.RatingFilter.Add("PG13");
            Settings.RatingFilter.Add("G");
            Settings.RatingFilter.Add("R");
            Settings.RatingFilter.Add("NC17");
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
            ImageGrid.ItemsSource = newBooks?.OrderByDescending(item => item.Settings.LastReadTime);
        }
        /// <summary>
        /// Load books from AllBooks, filtered
        /// </summary>
        private void ReloadBooks()
        {
            var newBooks = new ObservableCollection<BookImage>();
            var tmp = from c in AllBooks
                      where Settings.RatingFilter.Contains(c.Settings.Rating)
                      select c;
            var selected = tmp.ToArray();
            foreach (var i in selected)
            {
                newBooks.Add(i);
            }
            if (Settings.RatingFilter.Count == 5)
            {
                AllRatingFlyout.IsChecked = true;
            }
            else
            {
                AllRatingFlyout.IsChecked = false;
            }
            ImageGrid.ItemsSource = newBooks.OrderByDescending(item => item.Settings.LastReadTime);
        }
        private void NC17Flyout_Click(object sender, RoutedEventArgs e)
        {
            if (NC17Flyout.IsChecked)
            {
                Settings.RatingFilter.Add("NC17");
            }
            else
            {
                Settings.RatingFilter.Remove("NC17");
            }
            if (GFlyout.IsChecked)
            {
                Settings.RatingFilter.Add("G");
            }
            else
            {
                Settings.RatingFilter.Remove("G");
            }
            if (PGFlyout.IsChecked)
            {
                Settings.RatingFilter.Add("PG");
            }
            else
            {
                Settings.RatingFilter.Remove("PG");
            }
            if (PG13Flyout.IsChecked)
            {
                Settings.RatingFilter.Add("PG13");
            }
            else
            {
                Settings.RatingFilter.Remove("PG13");
            }
            if (RFlyout.IsChecked)
            {
                Settings.RatingFilter.Add("R");
            }
            else
            {
                Settings.RatingFilter.Remove("R");
            }
            ReloadBooks();
        }

        private async void DetailPageReadBookButton_Click(object sender, RoutedEventArgs e)
        {
            DetailPageReadBookButton.IsEnabled = false;
            DetailPageLoadingGrid.Visibility = Visibility.Visible;
            await CreateBook();
            var epub = new XavierEpubFile();
            epub.LoadMode = Settings.LoadMode;
            try
            {
                epub.LoadBook(DetailPageBook.TmpFolderName);
            }
            catch (EpubExtractionFailureException)
            {
                DetailPageLoadingStatus.Text = "Failed to load book.Cleaning up...";
                epub.CleanUp();
                DetailPageLoadingStatus.Text = "Failed to load book.";
                return;
            }
            catch (EpubContentLoadFailureException)
            {
                DetailPageLoadingStatus.Text = "Failed to load book.Cleaning up...";
                epub.CleanUp();
                DetailPageLoadingStatus.Text = "Failed to load book.";
                return;
            }
            OpenRecentBookEvent(epub);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SmokeGrid.Visibility == Visibility.Visible)
            {
                SmokeGridContent.Width = e.NewSize.Width * 0.6;
                SmokeGridContent.Height = e.NewSize.Height * 0.6;
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            SmokeGrid.Visibility = Visibility.Collapsed;
        }
    }
}
