using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using System.IO.Compression;
using Windows.Storage;
using Windows.UI.Xaml.Documents;
using System.Xml;
using System.Globalization;
using System.Threading.Tasks;

namespace XavierReader
{
    public class BookSettings
    {
        public string ContentFolder { get; set; }
        public DateTime LastReadTime { get; set; }
        public string Author { get; set; }
        public string Rating { get; set; }
        public int Chapters { get; set; }
        public int CurrentChapter { get; set; }
        public double ChapterProgress { get; set; }
        public string Title { get; set; }
        /// <summary>
        /// For recent page image brief
        /// </summary>
        /// <param name="contentFolder"></param>
        public BookSettings(string contentFolder)
        {
            ContentFolder = contentFolder;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentFolder"></param>
        /// <param name="title"></param>
        /// <param name="chapters"></param>
        public BookSettings(string contentFolder, string title, int chapters, EpubRating rating, string author)
        {
            ContentFolder = contentFolder;
            Title = title;
            Chapters = chapters;
            Rating = rating.ToString();
            Author = author;
        }
        public void LoadSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            //new book
            if (!settings.Values.ContainsKey(ContentFolder))
            {
                SaveNew();
            }
            else
            {
                CurrentChapter = int.Parse(settings.Values[ContentFolder + ".Chapter"].ToString());
                ChapterProgress = double.Parse(settings.Values[ContentFolder + ".Chapter.Progress"].ToString());
                LastReadTime = DateTime.ParseExact(settings.Values[ContentFolder + ".LastReadTime"].ToString(), "g", new CultureInfo("en-US"));
                Rating = settings.Values[ContentFolder + ".Rating"].ToString();
                Author = settings.Values[ContentFolder + ".Author"].ToString();
                Chapters = int.Parse(settings.Values[ContentFolder + ".Chapters"].ToString());
                Title = settings.Values[ContentFolder].ToString();
            }
        }
        private void SaveNew()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[ContentFolder] = Title;
            settings.Values[Title] = ContentFolder;
            settings.Values[ContentFolder + ".Chapters"] = Chapters.ToString();
            settings.Values[ContentFolder + ".Rating"] = Rating;
            settings.Values[ContentFolder + ".Author"] = Author;
            settings.Values[ContentFolder + ".Chapter"] = "0";
            settings.Values[ContentFolder + ".Chapter.Progress"] = "0";
            settings.Values[ContentFolder + ".LastReadTime"] = DateTime.Now.ToString("g", new CultureInfo("en-US"));
        }
        public void SaveSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[ContentFolder] = Title;
            settings.Values[Title] = ContentFolder;
            settings.Values[ContentFolder + ".Chapters"] = Chapters.ToString();
            settings.Values[ContentFolder + ".Rating"] = Rating;
            settings.Values[ContentFolder + ".Author"] = Author;
            settings.Values[ContentFolder + ".Chapter"] = CurrentChapter.ToString();
            settings.Values[ContentFolder + ".Chapter.Progress"] = ChapterProgress.ToString();
            settings.Values[ContentFolder + ".LastReadTime"] = LastReadTime.ToString("g", new CultureInfo("en-US"));
        }
        public void RemoveSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.ContainsKey(ContentFolder))
            {
                settings.Values.Remove(settings.Values[ContentFolder].ToString());
                settings.Values.Remove(ContentFolder);
                settings.Values.Remove(ContentFolder + ".Chapter");
                settings.Values.Remove(ContentFolder + ".Chapters");
                settings.Values.Remove(ContentFolder + ".Author");
                settings.Values.Remove(ContentFolder + ".Rating");
                settings.Values.Remove(ContentFolder + ".ChapterProgress");
                settings.Values.Remove(ContentFolder + ".LastReadTime");
            }
        }
    }
    class BookImage
    {
        public string ImageLocation { get; set; }
        public string TmpFolderName { get; set; }
        public BookSettings Settings { get; set; }
        public double Progress { get; set; }
    }
    public enum EpubRating
    {
        PG = 0,
        PG13 = 1,
        R = 2,
        NC17 = 3,
        G = 4,
        None = 5
    }
    public enum NodeType
    {
        text = 0x1,
        br = 0x2,
        b = 0x4,
        li = 0x8,
        i = 0x10,
        h = 0x20,
    }
    public enum EpubLoadMode
    {
        Full = 0,
        PerChapter = 1,
        Auto = 2
    }
    public class XavierEpubFile
    {
        /// <summary>
        /// Name of folder containing extracted epub content
        /// </summary>
        public string ContentFolder { get; set; }
        /// <summary>
        /// Path of local Epub copied from file system
        /// </summary>
        public string LocalEpub { get; set; }
        /// <summary>
        /// Path to the folder containing extracted epub content
        /// </summary>
        public string ExtractedPath { get; set; }
        public string Author { get; set; }
        public EpubRating Rating { get; set; }
        public string Title { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool DualPageView { get; set; }
        public int TotalChapters { get; set; }
        public int CurrentChapter { get; set; }
        public double ChapterProgress { get; set; }
        public DateTime LastReadTime { get; set; }
        public string[] ChapterTitles { get; set; }
        private string[] ChapterPaths { get; set; }
        /// <summary>
        /// Path to the cover image file
        /// </summary>
        public string CoverImage { get; set; }
        /// <summary>
        /// Block collection for FlipView1
        /// </summary>
        public List<List<Block>> BC1 { get; set; }
        /// <summary>
        /// Block collection for FlipView2
        /// </summary>
        public List<List<Block>> BC2 { get; set; }
        /// <summary>
        /// Single block collection for per-chapter loading mode
        /// </summary>
        public List<Block> Block1 { get; set; }
        /// <summary>
        /// Single block collection for per-chapter loading mode
        /// </summary>
        public List<Block> Block2 { get; set; }
        public EpubLoadMode LoadMode { get; set; }
        public BookSettings Settings { get; set; }
        public XavierEpubFile()
        {

        }
        /// <summary>
        /// Clean up after exception thrown
        /// </summary>
        public void CleanUp()
        {
            Directory.Delete(ExtractedPath, true);
            File.Delete(LocalEpub);
        }
        /// <summary>
        /// Load new Epub file
        /// </summary>
        /// <param name="storageFile">Epub file selected by FilePicker</param>
        /// <param name="loadMode">Specify epub file loading mode</param>
        /// <exception cref="EpubExtractionFailureException"/>
        /// <exception cref="EpubContentLoadFailureException"/>
        public async Task LoadBook(StorageFile storageFile, EpubLoadMode loadMode = EpubLoadMode.Full)
        {
            LoadMode = loadMode;
            //copy to LocalState
            var tmp = FileIO.ReadBufferAsync(storageFile);
            var newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(storageFile.Name, CreationCollisionOption.ReplaceExisting);
            LocalEpub = newFile.Path;
            await FileIO.WriteBufferAsync(newFile, await tmp);
            //extract epub
            ContentFolder = newFile.DisplayName;
            ExtractedPath = ApplicationData.Current.LocalFolder.Path + "/tmp/" + ContentFolder;
            try
            {
                //overwrite folder
                if (Directory.Exists(ExtractedPath))
                {
                    Directory.Delete(ExtractedPath, true);
                }
                //extract epub
                ZipFile.ExtractToDirectory(LocalEpub, ExtractedPath);
            }
            catch (Exception)
            {
                throw new EpubExtractionFailureException();
            }
            try
            {
                LoadBookInfo();
                LoadChapters();
                LoadProgress();
            }
            catch (EpubContentLoadFailureException)
            {
                throw;
            }
            return;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="nt"></param>
        /// <param name="B1"></param>
        /// <param name="B2"></param>
        /// <param name="cnt"></param>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        private void AddTextBlock(string text, NodeType nt, List<Block> B1, List<Block> B2, int cnt = 0, Paragraph par1 = null, Paragraph par2 = null)
        {
            //b par!=null
            if (nt == NodeType.b)
            {
                string processed = text;
                processed = processed.Replace("\n", " ");
                var run1 = new Run
                {
                    Text = processed
                };
                run1.FontWeight = Windows.UI.Text.FontWeights.Bold;
                par1.Inlines.Add(run1);
                var run2 = new Run
                {
                    Text = processed
                };
                run2.FontWeight = Windows.UI.Text.FontWeights.Bold;
                par2.Inlines.Add(run2);
            }
            //i par!=null
            else if (nt == NodeType.i)
            {
                string processed = text;
                processed = processed.Replace("\n", " ");
                var run1 = new Run
                {
                    Text = processed
                };
                run1.FontStyle = Windows.UI.Text.FontStyle.Italic;
                par1.Inlines.Add(run1);
                var run2 = new Run
                {
                    Text = processed
                };
                run2.FontStyle = Windows.UI.Text.FontStyle.Italic;
                par2.Inlines.Add(run2);
            }
            //h1-h6
            else if (nt == NodeType.h)
            {
                var pp = new Paragraph();
                var r = new Run
                {
                    Text = text + "\n",//extra line break
                    Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
                    FontWeight = Windows.UI.Text.FontWeights.Bold
                };
                pp.Inlines.Add(r);
                B1.Add(pp);
                var pp2 = new Paragraph();
                var r2 = new Run
                {
                    Text = text + "\n",
                    Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
                    FontWeight = Windows.UI.Text.FontWeights.Bold
                };
                pp2.Inlines.Add(r2);
                B2.Add(pp2);
            }
            //li
            else if (nt == NodeType.li)
            {
                string processed = text;
                processed = processed.Replace("\n", " ");
                par1 = new Paragraph();
                var run1 = new Run
                {
                    Text = cnt.ToString() + "." + processed + "\n"//extra line break
                };
                par1.Inlines.Add(run1);
                B1.Add(par1);
                par2 = new Paragraph();
                var run2 = new Run
                {
                    Text = cnt.ToString() + "." + processed + "\n"
                };
                par2.Inlines.Add(run2);
                B2.Add(par2);
            }
            //br par!=null
            else if (nt == NodeType.br)
            {
                var run1 = new Run
                {
                    Text = "\n"
                };
                par1.Inlines.Add(run1);
                var run2 = new Run
                {
                    Text = "\n"
                };
                par2.Inlines.Add(run2);
            }
            //text par!=null
            else
            {
                string processed = text;
                processed = processed.Replace("\n", " ");
                var run1 = new Run
                {
                    Text = processed
                };
                par1.Inlines.Add(run1);
                var run2 = new Run
                {
                    Text = processed
                };
                par2.Inlines.Add(run2);
            }
        }
        public void LoadChapter(string f)
        {
            var B1 = new List<Block>();
            var B2 = new List<Block>();
            //process single chapter
            XmlDocument xd = new XmlDocument();
            xd.Load(ExtractedPath + "/" + f);
            var html = xd.DocumentElement;
            //select 'body'
            var body = html.GetElementsByTagName("body")[0];
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xd.NameTable);
            nsMgr.AddNamespace("ns", body.NamespaceURI);
            //select 'h1-h6'
            for (int i = 1; i <= 6; ++i)
            {
                var h = body.SelectSingleNode(".//ns:h" + i.ToString(), nsMgr);
                if (h != null)
                {
                    AddTextBlock(h.InnerText, NodeType.h, B1, B2);
                }
            }
            //select 'section'
            var section = body.SelectSingleNode("ns:section", nsMgr);
            if (section != null)
            {
                //extract 'p'
                var ps = section.SelectNodes(".//ns:p", nsMgr);
                foreach (XmlNode p in ps)
                {
                    //extract single 'p'
                    var par1 = new Paragraph();
                    var par2 = new Paragraph();
                    //plain text is a node
                    foreach (XmlNode pc in p.ChildNodes)
                    {
                        if (pc.HasChildNodes)
                        {
                            switch (pc.Name)
                            {
                                case "strong":
                                case "b":
                                    AddTextBlock(pc.InnerText, NodeType.b, B1, B2, 0, par1, par2);
                                    break;
                                case "i":
                                    AddTextBlock(pc.InnerText, NodeType.i, B1, B2, 0, par1, par2);
                                    break;
                                default:
                                    AddTextBlock(pc.InnerText, NodeType.text, B1, B2, 0, par1, par2);
                                    break;
                            }
                        }
                        else //'p' only has plain text or pc is 'br'
                        {
                            if (pc.Name == "br")
                            {
                                AddTextBlock("", NodeType.br, B1, B2, 0, par1, par2);
                            }
                            else
                            {
                                AddTextBlock(pc.InnerText, NodeType.text, B1, B2, 0, par1, par2);
                            }
                        }
                    }
                    //add a paragraph line break
                    AddTextBlock("", NodeType.br, B1, B2, 0, par1, par2);
                    B1.Add(par1);
                    B2.Add(par2);
                }
            }
            //select 'li'(Content chapter)
            else
            {
                var ol = body.SelectNodes(".//ns:li", nsMgr);
                int cnt = 1;
                foreach (XmlNode t in ol)
                {
                    AddTextBlock(t.InnerText, NodeType.li, B1, B2, cnt);
                    ++cnt;
                }
            }
            if (LoadMode == EpubLoadMode.Full)
            {
                BC1.Add(B1);
                BC2.Add(B2);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void LoadChapters()
        {
            BC1 = new List<List<Block>>();
            BC2 = new List<List<Block>>();
            if (LoadMode == EpubLoadMode.Full)
            {
                foreach (var f in ChapterPaths)
                {
                    LoadChapter(f);
                }
            }
            else if (LoadMode == EpubLoadMode.PerChapter)
            {

            }
            else if (LoadMode == EpubLoadMode.Auto)
            {

            }
        }
        /// <summary>
        /// Get the *.opf file of the epub
        /// </summary>
        /// <returns>Path to *.opf</returns>
        /// <exception cref="EpubContentLoadFailureException"/>
        private string GetOpfFile()
        {
            try
            {
                var container = new XmlDocument();
                container.Load(ExtractedPath + "/META-INF/container.xml");
                var root = container.DocumentElement;
                var nsmgr = new XmlNamespaceManager(container.NameTable);
                nsmgr.AddNamespace("ns", root.NamespaceURI);
                var rootfile = root.SelectSingleNode("//ns:rootfile", nsmgr);
                return ExtractedPath + "/" + rootfile.Attributes["full-path"].Value;
            }
            catch (Exception)
            {
                throw new EpubContentLoadFailureException();
            }
        }
        /// <summary>
        /// Split title for HHr books
        /// </summary>
        /// <param name="title">Raw title</param>
        private void SplitTitle(string title)
        {
            try
            {
                var tmp = new StringBuilder(title);
                //split last update time
                var tmpdate = tmp.ToString();
                tmpdate = tmpdate.Substring(0, tmpdate.IndexOf(' ')).Replace('-', '/');
                LastUpdateTime = DateTime.ParseExact(tmpdate, "d", CultureInfo.CreateSpecificCulture("ja-JP"));
                tmp.Remove(0, tmpdate.IndexOf(' ') + 1);//removed date
                                                        //split rating level
                tmpdate = tmp.ToString();
                var leftBracket = tmpdate.IndexOf('(');
                var rightBracket = tmpdate.IndexOf(')');
                var rating = tmpdate.Substring(leftBracket + 8, rightBracket - leftBracket - 8);
                switch (rating)
                {
                    case "G":
                        Rating = EpubRating.G;
                        break;
                    case "PG":
                        Rating = EpubRating.PG;
                        break;
                    case "PG13":
                        Rating = EpubRating.PG13;
                        break;
                    case "R":
                        Rating = EpubRating.R;
                        break;
                    case "NC17":
                        Rating = EpubRating.NC17;
                        break;
                    default:
                        Rating = EpubRating.None;
                        break;
                }
                tmp.Remove(leftBracket - 1, rightBracket - leftBracket + 2);//remove rating
                                                                            //split title
                Title = tmp.ToString();
            }
            catch (Exception)
            {
                LastUpdateTime = DateTime.MinValue;
                Title = title;
            }
        }
        /// <summary>
        /// Load book info(author, title, ...)
        /// </summary>
        /// <exception cref="EpubContentLoadFailureException"/>
        private void LoadBookInfo()
        {
            try
            {
                var opfPath = GetOpfFile();
                var opf = new XmlDocument();
                opf.Load(opfPath);
                var package = opf.DocumentElement;
                var opfNsmgr = new XmlNamespaceManager(opf.NameTable);
                opfNsmgr.AddNamespace("pkg", package.NamespaceURI);
                var metadata = package.SelectSingleNode("pkg:metadata", opfNsmgr);
                //get title and try splitting
                opfNsmgr.AddNamespace("dc", metadata.Attributes["xmlns:dc"].Value);
                var dctitle = metadata.SelectSingleNode("dc:title", opfNsmgr).InnerText;
                SplitTitle(dctitle);
                //get creator(author)
                Author = metadata.SelectSingleNode("dc:creator", opfNsmgr).InnerText;
                var manifest = package.SelectSingleNode("pkg:manifest", opfNsmgr);
                //get chapter paths
                var chapterPaths = manifest.SelectNodes("pkg:item[@media-type = \"application/xhtml+xml\" and @id != \"titlepage\"]", opfNsmgr);
                TotalChapters = chapterPaths.Count;
                ChapterPaths = new string[TotalChapters];
                for (int i = 0; i < TotalChapters; ++i)
                {
                    ChapterPaths[i] = chapterPaths[i].Attributes["href"].Value;
                }
                //get *.ncx path
                var ncxPath = ExtractedPath + "/" + manifest.SelectSingleNode("pkg:item[@media-type = \"application/x-dtbncx+xml\"]", opfNsmgr).Attributes["href"].Value;
                var ncx = new XmlDocument();
                ncx.Load(ncxPath);
                var ncxroot = ncx.DocumentElement;
                var ncxNsmgr = new XmlNamespaceManager(ncx.NameTable);
                ncxNsmgr.AddNamespace("ncx", ncxroot.NamespaceURI);
                //get chapter titles
                var chapters = ncxroot.SelectNodes(".//ncx:navPoint", ncxNsmgr);
                ChapterTitles = new string[TotalChapters];
                ChapterTitles[0] = "Intro";
                ChapterTitles[1] = "Contents";
                for (int i = 0; i < chapters.Count; ++i)
                {
                    ChapterTitles[i + 2] = chapters[i].InnerText;
                }
            }
            catch (EpubContentLoadFailureException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new EpubContentLoadFailureException();
            }
            return;
        }
        /// <summary>
        /// Load a recently read book
        /// </summary>
        /// <param name="epub">Epub file selected by FilePicker</param>
        /// <param name="loadMode">Specify epub file loading mode</param>
        public void LoadBook(string epub, EpubLoadMode loadMode = EpubLoadMode.Full)
        {
            ContentFolder = epub;
            LoadMode = loadMode;
            ExtractedPath = ApplicationData.Current.LocalFolder.Path + "/tmp/" + ContentFolder;
            LoadBookInfo();
            LoadChapters();
            LoadProgress();
            return;
        }
        private void LoadProgress()
        {
            Settings = new BookSettings(ContentFolder, Title, TotalChapters, Rating, Author);
            Settings.LoadSettings();
            CurrentChapter = Settings.CurrentChapter;
            ChapterProgress = Settings.ChapterProgress;
            LastReadTime = Settings.LastReadTime;
        }
        /// <summary>
        /// Clear content for reloading
        /// </summary>
        public void Clear()
        {
            foreach (var i in BC1)
            {
                i.Clear();
            }
            BC1.Clear();
            foreach (var i in BC2)
            {
                i.Clear();
            }
            BC2.Clear();
        }
    }
    public class GlobalSettings
    {
        public bool isAcrylicOn { get; set; }
        public bool isDark { get; set; }
        public bool isAutoDark { get; set; }
        public TimeSpan AutoDarkStartTime { get; set; }
        public TimeSpan AutoDarkEndTime { get; set; }
    }
    public struct MsgParam
    {
        public bool isDark { get; set; }
        public string MsgContent { get; set; }
        public string Title { get; set; }
    }
}
