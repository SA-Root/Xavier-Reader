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
using muxc = Microsoft.UI.Xaml.Controls;
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
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace XavierReader
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class Startup : Page
    {
        public static event Action DecreaseFontEvent;
        public static event Action IncreaseFontEvent;
        public static event Action<int> ChangeChapterEvent;
        public static event Action<bool> SwitchModeEvent;
        private XavierEpub CurrentBook { get; set; }
        private int CurrentChapter { get; set; }
        private GlobalSettings Settings { get; set; }
        private void SetMiniState()
        {
            OpenButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            OpenButton.Width = 40;
            HomePageButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            HomePageButton.Width = 40;
            ShrinkFontButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            ShrinkFontButton.Width = 40;
            EnlargeFontButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            EnlargeFontButton.Width = 40;
            PreviousChapterButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            PreviousChapterButton.Width = 40;
            NextChapterButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            NextChapterButton.Width = 40;
            SwitchViewButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            SwitchViewButton.Width = 40;
            BookInfoButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            BookInfoButton.Width = 40;
            NightModeButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            NightModeButton.Width = 40;
            SettingsButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            SettingsButton.Width = 40;
            AboutButton.LabelPosition = CommandBarLabelPosition.Collapsed;
            AboutButton.Width = 40;
        }
        private void SetFullState()
        {
            OpenButton.LabelPosition = CommandBarLabelPosition.Default;
            OpenButton.Width = double.NaN;
            HomePageButton.LabelPosition = CommandBarLabelPosition.Default;
            HomePageButton.Width = double.NaN;
            ShrinkFontButton.LabelPosition = CommandBarLabelPosition.Default;
            ShrinkFontButton.Width = double.NaN;
            EnlargeFontButton.LabelPosition = CommandBarLabelPosition.Default;
            EnlargeFontButton.Width = double.NaN;
            PreviousChapterButton.LabelPosition = CommandBarLabelPosition.Default;
            PreviousChapterButton.Width = double.NaN;
            NextChapterButton.LabelPosition = CommandBarLabelPosition.Default;
            NextChapterButton.Width = double.NaN;
            SwitchViewButton.LabelPosition = CommandBarLabelPosition.Default;
            SwitchViewButton.Width = double.NaN;
            BookInfoButton.LabelPosition = CommandBarLabelPosition.Default;
            BookInfoButton.Width = double.NaN;
            NightModeButton.LabelPosition = CommandBarLabelPosition.Default;
            NightModeButton.Width = double.NaN;
            SettingsButton.LabelPosition = CommandBarLabelPosition.Default;
            SettingsButton.Width = double.NaN;
            AboutButton.LabelPosition = CommandBarLabelPosition.Default;
            AboutButton.Width = double.NaN;
        }
        private bool GetAppTheme() => Settings.isDark;
        public Startup()
        {
            this.InitializeComponent();
            MainPage.ButtonFeedback += ButtonFeedback;
            MainPage.ButtonFeedback2 += ButtonFeedback2;
            RecentPage.OpenRecentBookEvent += OpenRecentBook;
            RecentPage.QueryAppThemeEvent += GetAppTheme;
            Window.Current.SizeChanged += CurrentWindow_SizeChanged;
            LoadSettings();
            MainFrame.Navigate(typeof(RecentPage));
            if (Window.Current.Bounds.Width < 1200)
            {
                SetMiniState();
            }
            else
            {
                SetFullState();
            }
        }
        private void UpdateSidebarInfo()
        {
            BookTitle.Text = CurrentBook.Title;
            ToolTipService.SetToolTip(BookTitle, BookTitle.Text);
            BookAuthor.Text = CurrentBook.Author;
            BookRating.Text = CurrentBook.Rate;
            var t = new ObservableCollection<string>();
            int cnt = 1;
            foreach (var i in CurrentBook.ChapterTitles)
            {
                t.Add(cnt.ToString() + ": " + i);
                ++cnt;
            }
            SideContents.ItemsSource = t;
            SideScroller.Height = splitView.ActualHeight - 150;
            SideContents.SelectedIndex = CurrentBook.CurrentChapter;
            CurrentChapter = CurrentBook.CurrentChapter;
        }
        private void EnableReadingControls()
        {
            ShrinkFontButton.IsEnabled = true;
            EnlargeFontButton.IsEnabled = true;
            PreviousChapterButton.IsEnabled = true;
            NextChapterButton.IsEnabled = true;
            SwitchViewButton.IsEnabled = true;
            BookInfoButton.IsEnabled = true;
        }
        private void DisableReadingControls()
        {
            ShrinkFontButton.IsEnabled = false;
            EnlargeFontButton.IsEnabled = false;
            PreviousChapterButton.IsEnabled = false;
            NextChapterButton.IsEnabled = false;
            SwitchViewButton.IsEnabled = false;
            BookInfoButton.IsEnabled = false;
        }
        private Task CreateBook() =>
            Task.Run(() =>
            {
                Thread.Sleep(200);
            }
            );
        private async void OpenRecentBook(string TmpFolderName)
        {
            var dia = new Loading("Loading Book...", NightModeButton.IsChecked == true);
            dia.ShowAsync();
            await CreateBook();
            CurrentBook = new XavierEpub();
            CurrentBook.LoadBook(TmpFolderName);
            CurrentBook.isTwo = SwitchViewButton.IsChecked == true;
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(CurrentBook.TmpFolderName + ".Chapters"))
            {
                settings.Values[CurrentBook.TmpFolderName + ".Chapters"] = CurrentBook.TotalChapters.ToString();
            }
            if (!settings.Values.ContainsKey(CurrentBook.TmpFolderName + ".Chapters"))
            {
                settings.Values[CurrentBook.TmpFolderName + ".Author"] = CurrentBook.Author;
            }
            if (!settings.Values.ContainsKey(CurrentBook.TmpFolderName + ".Author"))
            {
                settings.Values[CurrentBook.TmpFolderName + ".Rating"] = CurrentBook.Rate;
            }
            EnableReadingControls();
            UpdateSidebarInfo();
            MainFrame.Navigate(typeof(MainPage), CurrentBook);
            dia.Hide();
        }
        private void ButtonFeedback2(int fontsize)
        {
            if (fontsize <= 20)
            {
                ShrinkFontButton.IsEnabled = false;
            }
            else if (fontsize >= 50)
            {
                EnlargeFontButton.IsEnabled = false;
            }
            else
            {
                ShrinkFontButton.IsEnabled = true;
                EnlargeFontButton.IsEnabled = true;
            }
        }
        private void ButtonFeedback(int cur, int all)
        {
            if (cur == 0)
            {
                PreviousChapterButton.IsEnabled = false;
                NextChapterButton.IsEnabled = true;
            }
            else if (cur >= all - 1)
            {
                NextChapterButton.IsEnabled = false;
                PreviousChapterButton.IsEnabled = true;
            }
            else
            {
                PreviousChapterButton.IsEnabled = true;
                NextChapterButton.IsEnabled = true;
            }
        }
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".epub");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var dia = new Loading("Loading Book...", NightModeButton.IsChecked == true);
                dia.ShowAsync();
                IBuffer tmp = await FileIO.ReadBufferAsync(file);
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile newFile = await storageFolder.CreateFileAsync(file.Name, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(newFile, tmp);
                CurrentBook = new XavierEpub();
                CurrentBook.LoadBook(newFile);
                CurrentBook.isTwo = SwitchViewButton.IsChecked == true;
                var settings = ApplicationData.Current.LocalSettings;
                if (!settings.Values.ContainsKey(CurrentBook.TmpFolderName + ".Chapters"))
                {
                    settings.Values[CurrentBook.TmpFolderName + ".Chapters"] = CurrentBook.TotalChapters.ToString();
                }
                if (!settings.Values.ContainsKey(CurrentBook.TmpFolderName + ".Chapters"))
                {
                    settings.Values[CurrentBook.TmpFolderName + ".Author"] = CurrentBook.Author;
                }
                if (!settings.Values.ContainsKey(CurrentBook.TmpFolderName + ".Author"))
                {
                    settings.Values[CurrentBook.TmpFolderName + ".Rating"] = CurrentBook.Rate;
                }
                EnableReadingControls();
                UpdateSidebarInfo();
                MainFrame.Navigate(typeof(MainPage), CurrentBook);
                dia.Hide();
            }
        }
        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WhatsNewDialog(NightModeButton.IsChecked == true);
            await dialog.ShowAsync();
        }
        private void ShrinkFont_Click(object sender, RoutedEventArgs e)
        {
            DecreaseFontEvent();
        }
        private void EnlargeFontButton_Click(object sender, RoutedEventArgs e)
        {
            IncreaseFontEvent();
        }
        private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
        {
            --CurrentChapter;
            ChangeChapterEvent(CurrentChapter);
            --SideContents.SelectedIndex;
        }
        private void NextChapterButton_Click(object sender, RoutedEventArgs e)
        {
            ++CurrentChapter;
            ChangeChapterEvent(CurrentChapter);
            ++SideContents.SelectedIndex;
        }
        private void NightModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (NightModeButton.IsChecked == true)
            {
                RequestedTheme = ElementTheme.Dark;
                Settings.isDark = true;
            }
            else
            {
                RequestedTheme = ElementTheme.Light;
                Settings.isDark = false;
            }
        }
        private void HomePageButton_Click(object sender, RoutedEventArgs e)
        {
            DisableReadingControls();
            MainFrame.Navigate(typeof(RecentPage));
        }
        private void SwitchViewButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchModeEvent(SwitchViewButton.IsChecked == true);
        }
        private void SetDark(bool isDark)
        {
            if (isDark)
            {
                NightModeButton.IsChecked = true;
                RequestedTheme = ElementTheme.Dark;
                Settings.isDark = true;
            }
            else
            {
                NightModeButton.IsChecked = false;
                RequestedTheme = ElementTheme.Light;
                Settings.isDark = false;
            }
        }
        private void SetAutoDark()
        {
            var now = DateTime.Now;
            if (Settings.isAutoDark)
            {
                if (
                    (now.Hour == Settings.AutoDarkStartTime.Hours && now.Minute >= Settings.AutoDarkStartTime.Minutes)
                    || (now.Hour == Settings.AutoDarkEndTime.Hours && now.Minute <= Settings.AutoDarkEndTime.Minutes)
                   )
                {
                    SetDark(true);
                }
                else
                {
                    if (Settings.AutoDarkStartTime > Settings.AutoDarkEndTime)
                    {
                        if (
                           now.Hour > Settings.AutoDarkStartTime.Hours
                           || now.Hour < Settings.AutoDarkEndTime.Hours
                           )
                        {
                            SetDark(true);
                        }
                        else
                        {
                            SetDark(false);
                        }
                    }
                    else
                    {
                        if (
                            now.Hour > Settings.AutoDarkStartTime.Hours
                            && now.Hour < Settings.AutoDarkEndTime.Hours
                          )
                        {
                            SetDark(true);
                        }
                        else
                        {
                            SetDark(false);
                        }
                    }
                }
            }
            else
            {
                SetDark(false);
            }
        }
        private void LoadSettings()
        {
            Settings = new GlobalSettings
            {
                isDark = NightModeButton.IsChecked == true
            };
            var AppSettings = ApplicationData.Current.LocalSettings;
            if (!AppSettings.Values.ContainsKey("isAcrylic"))
            {
                AppSettings.Values["isAcrylic"] = "0";
                Settings.isAcrylicOn = false;
                VisualStateManager.GoToState(this, "Default", false);
            }
            else
            {
                if (AppSettings.Values["isAcrylic"].ToString() == "0")
                {
                    Settings.isAcrylicOn = false;
                    VisualStateManager.GoToState(this, "Default", false);
                }
                else
                {
                    Settings.isAcrylicOn = true;
                    VisualStateManager.GoToState(this, "Acrylic", false);
                }
            }
            if (!AppSettings.Values.ContainsKey("isAutoDark"))
            {
                AppSettings.Values["isAutoDark"] = "0";
                Settings.isAutoDark = false;
                AppSettings.Values["AutoDarkStartTime"] = "17:00:00";
                Settings.AutoDarkStartTime = TimeSpan.ParseExact("17:00:00", "g", CultureInfo.InvariantCulture);
                AppSettings.Values["AutoDarkEndTime"] = "07:00:00";
                Settings.AutoDarkEndTime = TimeSpan.ParseExact("07:00:00", "g", CultureInfo.InvariantCulture);
            }
            else
            {
                if (AppSettings.Values["isAutoDark"].ToString() == "0")
                {
                    Settings.isAutoDark = false;
                }
                else
                {
                    Settings.isAutoDark = true;
                }
                if (!AppSettings.Values.ContainsKey("AutoDarkStartTime"))
                {
                    AppSettings.Values["AutoDarkStartTime"] = "17:00:00";
                    Settings.AutoDarkStartTime = TimeSpan.ParseExact("17:00:00", "g", CultureInfo.InvariantCulture);
                }
                else
                {
                    Settings.AutoDarkStartTime = TimeSpan.ParseExact(AppSettings.Values["AutoDarkStartTime"].ToString(), "g", CultureInfo.InvariantCulture);
                }
                if (!AppSettings.Values.ContainsKey("AutoDarkEndTime"))
                {
                    AppSettings.Values["AutoDarkEndTime"] = "07:00:00";
                    Settings.AutoDarkEndTime = TimeSpan.ParseExact("07:00:00", "g", CultureInfo.InvariantCulture);
                }
                else
                {
                    Settings.AutoDarkEndTime = TimeSpan.ParseExact(AppSettings.Values["AutoDarkEndTime"].ToString(), "g", CultureInfo.InvariantCulture);
                }
            }
            SetAutoDark();
        }
        private void SaveSettings(GlobalSettings gs)
        {
            Settings = gs;
            var AppSettings = ApplicationData.Current.LocalSettings;
            if (Settings.isAcrylicOn)
            {
                VisualStateManager.GoToState(this, "Acrylic", false);
                AppSettings.Values["isAcrylic"] = "1";
            }
            else
            {
                VisualStateManager.GoToState(this, "Default", false);
                AppSettings.Values["isAcrylic"] = "0";
            }
            if (Settings.isAutoDark)
            {
                AppSettings.Values["isAutoDark"] = "1";
            }
            else
            {
                AppSettings.Values["isAutoDark"] = "0";
            }
            AppSettings.Values["AutoDarkStartTime"] = Settings.AutoDarkStartTime.ToString("g", CultureInfo.InvariantCulture);
            AppSettings.Values["AutoDarkEndTime"] = Settings.AutoDarkEndTime.ToString("g", CultureInfo.InvariantCulture);
            SetAutoDark();
        }
        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog.SaveSettingsEvent += SaveSettings;
            var dialog = new SettingsDialog(Settings);
            await dialog.ShowAsync();
            SettingsDialog.SaveSettingsEvent -= SaveSettings;
        }
        private void BookInfoButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = true;
        }
        private void CurrentWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            if (e.Size.Width < 1200)
            {
                SetMiniState();
            }
            else
            {
                SetFullState();
            }
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            MainPage.ButtonFeedback -= ButtonFeedback;
            MainPage.ButtonFeedback2 -= ButtonFeedback2;
            RecentPage.OpenRecentBookEvent -= OpenRecentBook;
            RecentPage.QueryAppThemeEvent -= GetAppTheme;
            Window.Current.SizeChanged -= CurrentWindow_SizeChanged;
        }
        private void SideContents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SideContents.SelectedIndex >= 0 && ChangeChapterEvent != null)
            {
                CurrentChapter = SideContents.SelectedIndex;
                ChangeChapterEvent(SideContents.SelectedIndex);
                splitView.IsPaneOpen = false;
            }
        }
    }
}
