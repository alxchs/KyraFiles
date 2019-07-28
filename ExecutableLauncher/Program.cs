using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace ExecutableLauncher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = executable;
            process.Start();
        }
    }
}
