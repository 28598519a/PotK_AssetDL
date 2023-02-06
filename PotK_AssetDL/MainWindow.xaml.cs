using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace PotK_AssetDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "path.json|*.json";
            if (!openFileDialog.ShowDialog() == true)
                return;

            JObject ResList = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            JObject ResList_AB = JObject.Parse(ResList["AssetBundle"].ToString());
            JObject ResList_SA = JObject.Parse(ResList["StreamingAssets"].ToString());

            List<Tuple<string, string>> abList = new List<Tuple<string, string>>();
            List<Tuple<string, string>> saList = new List<Tuple<string, string>>();
            App.Respath = Path.Combine(App.Root, "Asset");

            foreach (JProperty jp in (JToken)ResList_AB)
            {
                string path = jp.Name;
                JArray ja = JArray.Parse(jp.Value.ToString());
                string file = ja[0].ToString();
                
                if (ja.Count == 4)
                {
                    // unity3d的Container中已經紀錄了路徑，所以不需要特別建
                    abList.Add(new Tuple<string, string>(Path.Combine(App.Respath, "AssetBundle", file), file));
                }
                else
                {
                    // 意外狀況，可以下斷點確認是否有會進這裡的情況
                }
            }

            
            //Dictionary<string, int> sakey = new Dictionary<string, int>();
            foreach (JProperty jp in (JToken)ResList_SA)
            {
                string path = jp.Name.Replace("_acb", String.Empty).Replace("_awb", String.Empty);
                JArray ja = JArray.Parse(jp.Value.ToString());
                string file = ja[0].ToString();
                string type = ja[1].ToString();

                /*
                // SA的路徑應該就包含真實檔名，若這樣會有重複則做自動重命名 (經測試確認不需要)
                if (sakey.ContainsKey(path + type))
                    path = path + $"_{sakey[path + type]++}";
                else
                    sakey.Add(path + type, 0);
                */

                // sa是非unity3d檔案，需要存到指定位置
                saList.Add(new Tuple<string, string>(Path.Combine(App.Respath, "StreamingAssets", path + type), file));
            }

            App.TotalCount = abList.Count + saList.Count;
            
            if (App.TotalCount > 0)
            {
                if (!Directory.Exists(App.Respath))
                    Directory.CreateDirectory(App.Respath);

                int count = 0;
                List<Task> tasks = new List<Task>();
                foreach (Tuple<string, string> asset in abList)
                {
                    string path = asset.Item1;
                    string url = App.ServerURL_ab + asset.Item2;

                    tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                    count++;

                    // 阻塞線程，等待現有工作完成再給新工作
                    if ((count % pool).Equals(0) || App.TotalCount == count)
                    {
                        // await is better than Task.Wait()
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    // 用await將線程讓給UI更新
                    lb_counter.Content = $"進度 : {count} / {App.TotalCount}";
                    await Task.Delay(1);
                }

                tasks.Clear();
                foreach (Tuple<string, string> asset in saList)
                {
                    string path = asset.Item1;
                    string url = App.ServerURL_sa + asset.Item2;

                    tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                    count++;

                    // 阻塞線程，等待現有工作完成再給新工作
                    if ((count % pool).Equals(0) || App.TotalCount == count)
                    {
                        // await is better than Task.Wait()
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    // 用await將線程讓給UI更新
                    lb_counter.Content = $"進度 : {count} / {App.TotalCount}";
                    await Task.Delay(1);
                }


                if (cb_Debug.IsChecked == true)
                {
                    using (StreamWriter outputFile = new StreamWriter("404.log", false))
                    {
                        foreach (string s in App.log)
                            outputFile.WriteLine(s);
                    }
                }

                string failmsg = String.Empty;
                if (App.TotalCount - App.glocount > 0)
                    failmsg = $"，{App.TotalCount - App.glocount}個檔案失敗";

                System.Windows.MessageBox.Show($"下載完成，共{App.glocount}個檔案{failmsg}", "Finish");
                lb_counter.Content = String.Empty;
            }
        }

        /// <summary>
        /// 從指定的網址下載檔案
        /// </summary>
        public async Task<Task> DownLoadFile(string downPath, string savePath, bool overWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            if (File.Exists(savePath) && overWrite == false)
                return Task.FromResult(0);

            App.glocount++;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    // Don't use DownloadFileTaskAsync, if 404 it will create a empty file, use DownloadDataTaskAsync instead.
                    byte[] data = await wc.DownloadDataTaskAsync(downPath);
                    File.WriteAllBytes(savePath, data);
                }
                catch (Exception ex)
                {
                    App.glocount--;

                    if (cb_Debug.IsChecked == true)
                        App.log.Add(downPath + Environment.NewLine + savePath + Environment.NewLine);

                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
            }
            return Task.FromResult(0);
        }
    }
}
