using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace XavierReader
{
    class BookImage
    {
        public string ImageLocation { get; set; }
        public string TmpFolderName { get; set; }
        public string LastReadTime { get; set; }
        public string Author { get; set; }
        public string Rating { get; set; }
        public double Progress { get; set; }
    }
    public enum EpubRating
    {
        PG = 0,
        PG13 = 1,
        R = 2,
        NC17 = 3,
        G = 4
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
    class XavierEpub
    {
        public bool isTwo { get; set; }
        //Extracted book folder root path
        public string Path { get; set; }
        //Extracted book folder(also Book ID)
        public string TmpFolderName { get; set; }
        //Block collection for FlipView1
        public List<List<Block>> BC1 { get; set; }
        //Block collection for FlipView2
        public List<List<Block>> BC2 { get; set; }
        //Chapter count
        public int TotalChapters { get; set; }
        //Chapter progress
        public int CurrentChapter { get; set; }
        //In-chapter progress
        public double ChapterProgress { get; set; }
        //Book title
        public string Title { get; set; } = "no title";
        //Last update time of book
        public DateTime FinalDate { get; set; }
        public string LastReadTime { get; set; }
        public string Author { get; set; } = "no author";
        public string Rate { get; set; }
        public List<string> ChapterTitles { get; set; }
        /// <summary>
        /// Load reading progress from app settings
        /// </summary>
        private void LoadProgress()
        {
            var settings = ApplicationData.Current.LocalSettings;
            //new book
            if (!settings.Values.ContainsKey(Title))
            {
                settings.Values[Title] = TmpFolderName;
                settings.Values[TmpFolderName] = Title;
                settings.Values[TmpFolderName + ".Chapter"] = "0";
                settings.Values[TmpFolderName + ".Chapter.Progress"] = "0";
                settings.Values[TmpFolderName + ".LastReadTime"] = DateTime.Now.ToString("g", new CultureInfo("en-US"));
                ChapterProgress = 0;
                CurrentChapter = 0;
                LastReadTime = DateTime.Now.ToString("g", new CultureInfo("en-US"));
            }
            else //recent book
            {
                if (!settings.Values.ContainsKey(TmpFolderName + ".Chapter"))
                {
                    CurrentChapter = 0;
                    settings.Values[TmpFolderName + ".Chapter"] = "0";
                }
                else
                {
                    CurrentChapter = int.Parse(settings.Values[TmpFolderName + ".Chapter"].ToString());
                }
                if (!settings.Values.ContainsKey(TmpFolderName + ".Chapter.Progress"))
                {
                    ChapterProgress = 0;
                    settings.Values[TmpFolderName + ".Chapter.Progress"] = "0";
                }
                else
                {
                    ChapterProgress = double.Parse(settings.Values[TmpFolderName + ".Chapter.Progress"].ToString());
                }
                if (!settings.Values.ContainsKey(TmpFolderName + ".LastReadTime"))
                {
                    LastReadTime = DateTime.Now.ToString("g", new CultureInfo("en-US"));
                    settings.Values[TmpFolderName + ".LastReadTime"] = DateTime.Now.ToString("g", new CultureInfo("en-US"));
                }
                else
                {
                    LastReadTime = settings.Values[TmpFolderName + ".LastReadTime"].ToString();
                }
            }
        }
        /// <summary>
        /// Load book title from 'toc.ncx'
        /// </summary>
        private void LoadBookInfo()
        {
            //Load 'toc.ncx'
            XmlDocument xd0 = new XmlDocument();
            xd0.Load(Path + "/toc.ncx");
            //select root node
            var rootNode = xd0.DocumentElement;
            //select 'docTitle'
            var tmp = new StringBuilder(rootNode.GetElementsByTagName("docTitle")[0].InnerText);
            //split last update time
            tmp.Replace('-', '/');
            var tmpdate = tmp.ToString();
            FinalDate = DateTime.ParseExact(tmpdate.Substring(0, tmpdate.IndexOf(' ')), "d", CultureInfo.CreateSpecificCulture("ja-JP"));
            tmp.Remove(0, tmpdate.IndexOf(' ') + 1);//removed date
            //split rating level
            tmpdate = tmp.ToString();
            var leftBracket = tmpdate.IndexOf('(');
            var rightBracket = tmpdate.IndexOf(')');
            var rating = tmpdate.Substring(leftBracket + 8, rightBracket - leftBracket - 8);
            switch (rating)
            {
                case "G":
                    Rate = "G";
                    break;
                case "PG":
                    Rate = "PG";
                    break;
                case "PG13":
                    Rate = "PG13";
                    break;
                case "R":
                    Rate = "R";
                    break;
                case "NC17":
                    Rate = "NC17";
                    break;
                default:
                    Rate = "none";
                    break;
            }
            tmp.Remove(leftBracket - 1, rightBracket - leftBracket + 2);//remove rating
            //split title
            Title = tmp.ToString();
            //select 'navMap'
            var navMap = rootNode.GetElementsByTagName("navMap")[0];
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xd0.NameTable);
            nsMgr.AddNamespace("ns", navMap.NamespaceURI);
            //extract chapter titles
            var navLabels = navMap.SelectNodes(".//ns:navLabel", nsMgr);
            ChapterTitles = new List<string>();
            ChapterTitles.Add("Intro");
            ChapterTitles.Add("Contents");
            foreach (XmlNode navLabel in navLabels)
            {
                ChapterTitles.Add(navLabel.InnerText);
            }
        }
        private void AddTextBlock(string text, NodeType nt, int cnt = 0, Paragraph par1 = null, Paragraph par2 = null)
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
                BC1[TotalChapters].Add(pp);
                var pp2 = new Paragraph();
                var r2 = new Run
                {
                    Text = text + "\n",
                    Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
                    FontWeight = Windows.UI.Text.FontWeights.Bold
                };
                pp2.Inlines.Add(r2);
                BC2[TotalChapters].Add(pp2);
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
                BC1[TotalChapters].Add(par1);
                par2 = new Paragraph();
                var run2 = new Run
                {
                    Text = cnt.ToString() + "." + processed + "\n"
                };
                par2.Inlines.Add(run2);
                BC2[TotalChapters].Add(par2);
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
        /// <summary>
        /// Load chapter contents from 'text/***.html'
        /// </summary>
        private void LoadChapters()
        {
            TotalChapters = 0;
            var dir = new DirectoryInfo(Path + "/text");
            bool isFirstFile = true;
            foreach (var f in dir.GetFiles())
            {
                BC1.Add(new List<Block>());
                BC2.Add(new List<Block>());
#if DEBUG
                Debug.Print(f.FullName + "\n");
#endif
                //process single chapter
                XmlDocument xd = new XmlDocument();
                xd.Load(f.FullName);
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
                        AddTextBlock(h.InnerText, NodeType.h);
                    }
                }
                //select 'section'
                var section = body.SelectSingleNode("ns:section", nsMgr);
                if (section != null)
                {
                    //extract 'p'
                    var ps = section.SelectNodes(".//ns:p", nsMgr);
                    //get Author
                    if (isFirstFile)
                    {
                        isFirstFile = false;
                        Author = ps[0].InnerText;
                    }
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
                                        AddTextBlock(pc.InnerText, NodeType.b, 0, par1, par2);
                                        break;
                                    case "i":
                                        AddTextBlock(pc.InnerText, NodeType.i, 0, par1, par2);
                                        break;
                                    default:
                                        AddTextBlock(pc.InnerText, NodeType.text, 0, par1, par2);
                                        break;
                                }
                            }
                            else //'p' only has plain text or pc is 'br'
                            {
                                if (pc.Name == "br")
                                {
                                    AddTextBlock("", NodeType.br, 0, par1, par2);
                                }
                                else
                                {
                                    AddTextBlock(pc.InnerText, NodeType.text, 0, par1, par2);
                                }
                            }
                        }
                        //add a paragraph line break
                        AddTextBlock("", NodeType.br, 0, par1, par2);
                        BC1[TotalChapters].Add(par1);
                        BC2[TotalChapters].Add(par2);
                    }
                }
                //select 'li'(Content chapter)
                else
                {
                    var ol = body.SelectNodes(".//ns:li", nsMgr);
                    int cnt = 1;
                    foreach (XmlNode t in ol)
                    {
                        AddTextBlock(t.InnerText, NodeType.li, cnt);
                        ++cnt;
                    }
                }
                ++TotalChapters;
            }
        }
        /// <summary>
        /// Load new book uses this
        /// </summary>
        /// <param name="file"></param>
        public void LoadBook(StorageFile file)
        {
            var BookPath = ApplicationData.Current.LocalFolder.Path + "/tmp/" + file.DisplayName;
            TmpFolderName = file.DisplayName;
            if (!Directory.Exists(BookPath))
            {
                ZipFile.ExtractToDirectory(file.Path, BookPath);
            }
            Path = BookPath;
            BC1 = new List<List<Block>>();
            BC2 = new List<List<Block>>();
            LoadBookInfo();
            LoadChapters();
            LoadProgress();
#if DEBUG
            Debug.Print(BC1.Count.ToString() + "\n");
#endif
        }
        public void LoadBook(string TmpFName)
        {
            TmpFolderName = TmpFName;
            Path = ApplicationData.Current.LocalFolder.Path + "/tmp/" + TmpFolderName;
            BC1 = new List<List<Block>>();
            BC2 = new List<List<Block>>();
            LoadBookInfo();
            LoadChapters();
            LoadProgress();
        }
        public XavierEpub()
        {
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
    class XavierEpubFile
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
        public uint ChapterCount { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool DualPageView { get; set; }
        /// <summary>
        /// Path to the cover image file
        /// </summary>
        public string CoverImage { get; set; }
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
        public async Task<int> LoadBook(StorageFile storageFile, EpubLoadMode loadMode = EpubLoadMode.Full, bool multiThreadedTextLoading = false)
        {
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
            }
            catch (EpubContentLoadFailureException)
            {
                throw;
            }
            return 0;
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

        }
        /// <summary>
        /// Load book info(author, title, ...)
        /// </summary>
        /// <exception cref="EpubContentLoadFailureException"/>
        public void LoadBookInfo()
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
                //get *.ncx path

            }
            catch (EpubContentLoadFailureException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new EpubContentLoadFailureException();
            }
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
