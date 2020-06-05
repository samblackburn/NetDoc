using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace NetDoc.Tests
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TempDir : IDisposable
    {
        private static List<TempDir> s_Dirs = new List<TempDir>();
        public TempDir() => System.IO.Directory.CreateDirectory(Directory);
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        public void Dispose() => System.IO.Directory.Delete(Directory, true);

        public static string Get()
        {
            var dir = new TempDir();
            s_Dirs.Add(dir);
            return dir.Directory;
        }

        public static void CleanUp()
        {
            var dirs = s_Dirs;
            s_Dirs = new List<TempDir>();

            foreach (var tempDir in dirs)
            {
                tempDir.Dispose();
            }
        }
    }
}