using System;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;


namespace XavierReader
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private XavierEpubFile NowEpub { get; set; }
        private int _FontSize { get; set; }
        private RichTextBlockOverflow last1 { get; set; }
        private RichTextBlockOverflow last2 { get; set; }
        private int PreviousWidth { get; set; }
        private int PreviousHeight { get; set; }
        public static event Action<int, int> ButtonFeedback;
        public static event Action<int> ButtonFeedback2;
        private void UpdateProgressDisplay()
        {
            if (flp1.Visibility == Visibility.Visible)
            {
                NowEpub.Settings.ChapterProgress = (flp1.SelectedIndex + 1) / (double)flp1.Items.Count;
                curPage.Text = (NowEpub.Settings.CurrentChapter + 1).ToString() + "." + NowEpub.ChapterTitles[NowEpub.Settings.CurrentChapter] + "-" + Math.Round(NowEpub.Settings.ChapterProgress * 100, 2).ToString() + "% [" + (flp1.SelectedIndex + 1).ToString() + "/" + flp1.Items.Count.ToString() + "]";
            }
            else
            {
                NowEpub.Settings.ChapterProgress = (flp2.SelectedIndex + 1) / (double)flp2.Items.Count;
                curPage.Text = (NowEpub.Settings.CurrentChapter + 1).ToString() + "." + NowEpub.ChapterTitles[NowEpub.Settings.CurrentChapter] + "-" + Math.Round(NowEpub.Settings.ChapterProgress * 100, 2).ToString() + "% [" + (flp2.SelectedIndex + 1).ToString() + "/" + flp2.Items.Count.ToString() + "]";
            }
        }
        public MainPage()
        {
            this.InitializeComponent();
            last1 = null;
            last2 = null;
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey("FontSize"))
            {
                settings.Values["FontSize"] = "24";
                _FontSize = 24;
            }
            else
            {
                _FontSize = int.Parse(settings.Values["FontSize"].ToString());
            }
            Window.Current.SizeChanged += flp2_SizeChanged;
            Startup.DecreaseFontEvent += DecreaseFont;
            Startup.IncreaseFontEvent += IncreaseFont;
            Startup.ChangeChapterEvent += ChangeChapter;
            Startup.SwitchModeEvent += SwitchMode;
            PreviousWidth = (int)Window.Current.Bounds.Width;
            PreviousHeight = (int)Window.Current.Bounds.Height;
        }
        private void SwitchMode(bool isTwo)
        {
            if (isTwo)
            {
                flp1.Visibility = Visibility.Collapsed;
                flp2.Visibility = Visibility.Visible;
            }
            else
            {
                flp2.Visibility = Visibility.Collapsed;
                flp1.Visibility = Visibility.Visible;
            }
            UpdateView();
            TrackPage();
            UpdateProgressDisplay();
        }
        private void DecreaseFont()
        {
            _FontSize -= 3;
            ButtonFeedback2(_FontSize);
            foreach (var p in rich1.Blocks)
            {
                p.FontSize = _FontSize;
            }
            rich1.UpdateLayout();
            flp1.UpdateLayout();
            foreach (var p in rich2.Blocks)
            {
                p.FontSize = _FontSize;
            }
            rich2.UpdateLayout();
            flp2.UpdateLayout();
            UpdateView();
            TrackPage();
            UpdateProgressDisplay();
        }
        private void IncreaseFont()
        {
            _FontSize += 3;
            ButtonFeedback2(_FontSize);
            foreach (var p in rich1.Blocks)
            {
                p.FontSize = _FontSize;
            }
            rich1.UpdateLayout();
            flp1.UpdateLayout();
            foreach (var p in rich2.Blocks)
            {
                p.FontSize = _FontSize;
            }
            rich2.UpdateLayout();
            flp2.UpdateLayout();
            UpdateView();
            TrackPage();
            UpdateProgressDisplay();
        }
        private void UpdateView()
        {
            if (rich1 != null && flp1.Visibility == Visibility.Visible)
            {
                rich1.UpdateLayout();
                flp1.UpdateLayout();
                if (rich1.HasOverflowContent)
                {
                    if (last1 == null)
                    {
                        bool isOF = rich1.HasOverflowContent;
                        var of2 = new RichTextBlockOverflow();
                        of2.Margin = new Thickness(30, 30, 30, 30);
                        rich1.OverflowContentTarget = of2;
                        last1 = of2;
                        flp1.Items.Add(of2);
                        of2.UpdateLayout();
                        isOF = of2.HasOverflowContent;
                        while (isOF)
                        {
                            var of21 = new RichTextBlockOverflow();
                            of21.Margin = new Thickness(30, 30, 30, 30);
                            last1.OverflowContentTarget = of21;
                            last1 = of21;
                            flp1.Items.Add(of21);
                            of21.UpdateLayout();
                            isOF = of21.HasOverflowContent;
                        }
                    }
                    else
                    {
                        if (last1.HasOverflowContent)
                        {
                            var isOF = last1.HasOverflowContent;
                            while (isOF)
                            {
                                var of21 = new RichTextBlockOverflow();
                                of21.Margin = new Thickness(30, 30, 30, 30);
                                last1.OverflowContentTarget = of21;
                                last1 = of21;
                                flp1.Items.Add(of21);
                                of21.UpdateLayout();
                                isOF = of21.HasOverflowContent;
                            }
                        }
                        else
                        {
                            for (int i = 1; i < flp1.Items.Count; ++i)
                            {
                                var p = flp1.Items[i] as RichTextBlockOverflow;
                                if (p.HasOverflowContent == false)
                                {
                                    for (int j = flp1.Items.Count - 1; j > i; --j)
                                    {
                                        flp1.Items.RemoveAt(j);
                                        //flp1.UpdateLayout();
                                    }
                                    last1 = p;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = flp1.Items.Count - 1; i >= 1; --i)
                    {
                        flp1.Items.RemoveAt(i);
                        //flp1.UpdateLayout();
                    }
                    last1 = null;
                }
                //rich1.UpdateLayout();
                //flp1.UpdateLayout();
            }
            if (rich2 != null && flp2.Visibility == Visibility.Visible)
            {
                rich2.UpdateLayout();
                flp2.UpdateLayout();
                if (rich2.HasOverflowContent)
                {
                    if (last2 == null)
                    {
                        bool isOF = rich2.HasOverflowContent;
                        Grid ng = new Grid();
                        var ncd = new ColumnDefinition();
                        ng.ColumnDefinitions.Add(ncd);
                        var ncd2 = new ColumnDefinition();
                        ng.ColumnDefinitions.Add(ncd2);
                        var of2 = new RichTextBlockOverflow();
                        of2.Margin = new Thickness(30, 30, 30, 30);
                        rich2.OverflowContentTarget = of2;
                        last2 = of2;
                        ng.Children.Add(of2);
                        Grid.SetColumn(of2, 0);
                        var of3 = new RichTextBlockOverflow();
                        of3.Margin = new Thickness(30, 30, 30, 30);
                        last2.OverflowContentTarget = of3;
                        last2 = of3;
                        ng.Children.Add(of3);
                        Grid.SetColumn(of3, 1);
                        flp2.Items.Add(ng);
                        of2.UpdateLayout();
                        of3.UpdateLayout();
                        isOF = of3.HasOverflowContent;
                        while (isOF)
                        {
                            Grid ng1 = new Grid();
                            var ncd1 = new ColumnDefinition();
                            ng1.ColumnDefinitions.Add(ncd1);
                            var ncd21 = new ColumnDefinition();
                            ng1.ColumnDefinitions.Add(ncd21);
                            var of21 = new RichTextBlockOverflow();
                            of21.Margin = new Thickness(30, 30, 30, 30);
                            last2.OverflowContentTarget = of21;
                            last2 = of21;
                            ng1.Children.Add(of21);
                            Grid.SetColumn(of21, 0);
                            var of31 = new RichTextBlockOverflow();
                            of31.Margin = new Thickness(30, 30, 30, 30);
                            last2.OverflowContentTarget = of31;
                            last2 = of31;
                            ng1.Children.Add(of31);
                            Grid.SetColumn(of31, 1);
                            flp2.Items.Add(ng1);
                            of21.UpdateLayout();
                            of31.UpdateLayout();
                            isOF = of31.HasOverflowContent;
                        }
                    }
                    else
                    {
                        if (last2.HasOverflowContent)
                        {
                            var isOF = last2.HasOverflowContent;
                            while (isOF)
                            {
                                Grid ng1 = new Grid();
                                var ncd1 = new ColumnDefinition();
                                ng1.ColumnDefinitions.Add(ncd1);
                                var ncd21 = new ColumnDefinition();
                                ng1.ColumnDefinitions.Add(ncd21);
                                var of21 = new RichTextBlockOverflow();
                                of21.Margin = new Thickness(30, 30, 30, 30);
                                last2.OverflowContentTarget = of21;
                                last2 = of21;
                                ng1.Children.Add(of21);
                                Grid.SetColumn(of21, 0);
                                var of31 = new RichTextBlockOverflow();
                                of31.Margin = new Thickness(30, 30, 30, 30);
                                last2.OverflowContentTarget = of31;
                                last2 = of31;
                                ng1.Children.Add(of31);
                                Grid.SetColumn(of31, 1);
                                flp2.Items.Add(ng1);
                                of21.UpdateLayout();
                                of31.UpdateLayout();
                                isOF = of31.HasOverflowContent;
                            }
                        }
                        else
                        {
                            for (int i = 1; i < flp2.Items.Count; ++i)
                            {
                                var p = (flp2.Items[i] as Grid).Children[1] as RichTextBlockOverflow;
                                if (p.HasOverflowContent == false)
                                {
                                    for (int j = flp2.Items.Count - 1; j > i; --j)
                                    {
                                        flp2.Items.RemoveAt(j);
                                        //flp2.UpdateLayout();
                                    }
                                    last2 = p;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = flp2.Items.Count - 1; i >= 1; --i)
                    {
                        flp2.Items.RemoveAt(i);
                        //flp2.UpdateLayout();
                    }
                    last2 = null;
                }
                //flp2.UpdateLayout();
            }
        }
        private void TrackPage()
        {
            //计数追踪
            if (flp2.Visibility == Visibility.Visible)
            {
                if (NowEpub.Settings.ChapterProgress <= 0.001 || flp2.Items.Count == 1)
                {
                    flp2.SelectedIndex = 0;
                }
                else
                {
                    int tmp = (int)Math.Round(NowEpub.Settings.ChapterProgress * flp2.Items.Count, 0) - 1;
                    if (tmp < 0) tmp = 0;
                    flp2.SelectedIndex = tmp;
                }
            }
            else
            {
                if (NowEpub.Settings.ChapterProgress <= 0.001 || flp1.Items.Count == 1)
                {
                    flp1.SelectedIndex = 0;
                }
                else
                {
                    int tmp = (int)Math.Round(NowEpub.Settings.ChapterProgress * flp1.Items.Count, 0) - 1;
                    if (tmp < 0) tmp = 0;
                    flp1.SelectedIndex = tmp;
                }
            }
        }
        private void flp1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flp1.Items.Count > 0)
            {
                if (flp1.SelectedIndex >= 0 && NowEpub != null)
                {
                    var t = (flp1.SelectedItem as RichTextBlockOverflow)?.ContentStart;
                    if (t != null)
                    {
                        NowEpub.Settings.ChapterProgress = (flp1.SelectedIndex + 1) / (double)flp1.Items.Count;
                    }
                    else
                    {
                        NowEpub.Settings.ChapterProgress = 0;
                    }
                    SaveProgress();
                    UpdateProgressDisplay();
                }
            }
        }
        private void flp2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flp2.Items.Count > 0)
            {
                if (flp2.SelectedIndex >= 0 && NowEpub != null)
                {
                    var t = ((flp2.Items[flp2.SelectedIndex] as Grid)?.Children[0] as RichTextBlockOverflow)?.ContentEnd;
                    if (t != null)
                    {
                        NowEpub.Settings.ChapterProgress = (double)(flp2.SelectedIndex + 1) / (double)flp2.Items.Count;
                    }
                    else
                    {
                        t = ((flp2.Items[flp2.SelectedIndex] as Grid)?.Children[0] as RichTextBlock)?.ContentEnd;
                        if (t != null)
                        {
                            NowEpub.Settings.ChapterProgress = (double)(flp2.SelectedIndex + 1) / (double)flp2.Items.Count;
                        }
                        else
                        {
                            NowEpub.Settings.ChapterProgress = 0;
                        }
                    }
                    SaveProgress();
                    UpdateProgressDisplay();
                }
            }
        }
        private void NavigateChapter(int ch, double prog)
        {
            NowEpub.Settings.CurrentChapter = ch;
            ButtonFeedback(ch, NowEpub.TotalChapters);
            rich1.Blocks.Clear();
            rich2.Blocks.Clear();
            if (NowEpub.LoadMode == EpubLoadMode.Full)
            {
                foreach (var t in NowEpub.BC1[ch])
                {
                    rich1.Blocks.Add(t);
                    t.FontSize = _FontSize;
                }
                rich1.UpdateLayout();
                foreach (var t in NowEpub.BC2[ch])
                {
                    rich2.Blocks.Add(t);
                    t.FontSize = _FontSize;
                }
                rich2.UpdateLayout();
            }
            else if (NowEpub.LoadMode == EpubLoadMode.PerChapter)
            {
                NowEpub.LoadChapter(NowEpub.ChapterPaths[ch]);
                foreach (var t in NowEpub.Block1)
                {
                    rich1.Blocks.Add(t);
                    t.FontSize = _FontSize;
                }
                rich1.UpdateLayout();
                foreach (var t in NowEpub.Block2)
                {
                    rich2.Blocks.Add(t);
                    t.FontSize = _FontSize;
                }
                rich2.UpdateLayout();
            }
            UpdateView();
            NowEpub.Settings.ChapterProgress = prog;
            TrackPage();
            UpdateProgressDisplay();
            SaveProgress();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NowEpub = e.Parameter as XavierEpubFile;
            if (NowEpub.DualPageView)
            {
                flp1.Visibility = Visibility.Collapsed;
                flp2.Visibility = Visibility.Visible;
            }
            else
            {
                flp2.Visibility = Visibility.Collapsed;
                flp1.Visibility = Visibility.Visible;
            }
            NavigateChapter(NowEpub.CurrentChapter, NowEpub.ChapterProgress);
            UpdateTime();
        }
        private void ChangeChapter(int c)
        {
            NavigateChapter(c, 0.00);
            UpdateProgressDisplay();
        }
        private void SaveProgress()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["FontSize"] = _FontSize.ToString();
            NowEpub.Settings.LastReadTime = DateTime.Now;
            NowEpub.Settings.SaveSettings();
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= flp2_SizeChanged;
            Startup.DecreaseFontEvent -= DecreaseFont;
            Startup.IncreaseFontEvent -= IncreaseFont;
            Startup.ChangeChapterEvent -= ChangeChapter;
            Startup.SwitchModeEvent -= SwitchMode;

            rich2.Blocks.Clear();
            rich1.Blocks.Clear();
            NowEpub.Clear();
        }
        private void UpdateTime()
        {
            CurrentTime.Text = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
            Task.Run(async () =>
            {
                while (true)
                {
                    Thread.Sleep(50000);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        CurrentTime.Text = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
                    });
                }
            });
        }
        /// <summary>
        /// Window size change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void flp2_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            int curw = (int)Window.Current.Bounds.Width, curh = (int)Window.Current.Bounds.Height;
            if (curw != PreviousWidth || curh != PreviousHeight)
            {
                PreviousHeight = curh;
                PreviousWidth = curw;
                Task.Run(async () =>
                {
                    Thread.Sleep(200);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if ((flp2.Items.Count >= 1 && flp2.Visibility == Visibility.Visible) || (flp1.Items.Count >= 1 && flp1.Visibility == Visibility.Visible))
                        {
                            UpdateView();
                            TrackPage();
                        }
                    });
                });
            }
        }
    }
}