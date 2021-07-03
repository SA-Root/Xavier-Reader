using System;

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