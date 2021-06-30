using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.IO.Compression;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Activation;
using System.Xml;
using System.Diagnostics;
using System.Globalization;

namespace XavierReader
{
    /// <summary>
    /// Epub file failed to extract to temp folder.
    /// </summary>
    class EpubExtractionFailureException : Exception
    {
        public EpubExtractionFailureException() : base() { }
    }
    class EpubContentLoadFailureException : Exception
    {
        public EpubContentLoadFailureException() : base() { }
    }
}