using System;
using System.Collections.Generic;
using System.Windows;

namespace PotK_AssetDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Respath = String.Empty;
        public static int TotalCount = 0;
        public static int glocount = 0;
        public static string ServerURL = "https://punk-dlc.gu3.jp/dlc/production/2018/android/";
        public static string ServerURL_ab = ServerURL + "ab/";
        public static string ServerURL_sa = ServerURL + "sa/";
        public static List<string> log = new List<string>();
    }
}
