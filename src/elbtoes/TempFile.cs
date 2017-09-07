using System;
using System.IO;

namespace elbtoes
{
    public class TempFile : IDisposable
    {
        public TempFile()
        {
            Path = System.IO.Path.GetTempFileName();
        }

        public string Path { get; private set; }

        public Stream OpenWrite() => File.Open(Path, FileMode.Create);

        public Stream OpenRead() => File.OpenRead(Path);

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
