using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System;

namespace Misc
{
    public static class FileAssociation
    {
        private const string FILE_EXTENSION = ".scn";
        private const long SHCNE_ASSOCCHANGED = 0x8000000L;
        private const uint SHCNF_IDLIST = 0x0U;

        public static void Associate(string description, string icon)
        {
            string productName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            Registry.ClassesRoot.CreateSubKey(FILE_EXTENSION).SetValue("", productName);

            if (!string.IsNullOrEmpty(productName))
            {
                using (var key = Registry.ClassesRoot.CreateSubKey(productName))
                {
                    if (description != null)
                        key.SetValue("", description);

                    if (icon != null)
                        key.CreateSubKey("DefaultIcon").SetValue("", ToShortPathName(Path.GetFullPath(icon)));

                    key.CreateSubKey(@"Shell\Open\Command").SetValue("", ToShortPathName(Environment.GetCommandLineArgs()[0]) + " \"%1\"");
                }
            }

            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        public static bool IsAssociated
        {
            get { return (Registry.ClassesRoot.OpenSubKey(FILE_EXTENSION, false) != null); }
        }

        public static void Remove()
        {
            string productName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            Registry.ClassesRoot.DeleteSubKeyTree(FILE_EXTENSION);
            Registry.ClassesRoot.DeleteSubKeyTree(productName);
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("Kernel32.dll")]
        private static extern uint GetShortPathName(string lpszLongPath, [Out]StringBuilder lpszShortPath, uint cchBuffer);

        private static string ToShortPathName(string longName)
        {
            StringBuilder s = new StringBuilder(1000);
            uint iSize = (uint)s.Capacity;
            uint iRet = GetShortPathName(longName, s, iSize);
            return s.ToString();
        }
    }
}