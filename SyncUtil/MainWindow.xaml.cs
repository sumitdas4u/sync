﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using SyncUtil.entities;
using Newtonsoft.Json;
using System.Net;

using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace SyncUtil
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {



        downloadManager dwm = new downloadManager();


        private string url = "http://teamcacm.com/onebook/getfilelist.php";

    
        string localfolder = Properties.Settings.Default.path.ToString();

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        Dictionary<String, myfile> LIVEFILES;
        Dictionary<String, myfile> LOCALFILES;

        List<downloadfile> DOWNLOADQUEUE;
        List<String> DOWNLOADPENDING;

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("ApplicationName", System.Reflection.Assembly.GetExecutingAssembly().Location.ToString());
            }
            else
            {
                registryKey.DeleteValue("ApplicationName");
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            RegisterInStartup(true);
           
            this.WindowState = WindowState.Minimized;
            this.Visibility = Visibility.Hidden;

            dispatcherTimer.Tick += new EventHandler(timer_CheckLiveFiles);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);

            dwm.onFileDownloadStart += Dwm_onFileDownloadStart;
            dwm.onFileDownloadComplete += Dwm_onFileDownloadComplete;
            dwm.onFileDownloadProgress += Dwm_onFileDownloadProgress;
            dwm.onFileDownloadStopped += Dwm_onFileDownloadStopped;
            dwm.onFileDownloadInterrupted += Dwm_onFileDownloadInterrupted;

            DOWNLOADQUEUE = new List<downloadfile>();
            DOWNLOADPENDING = new List<string>();

            filecontroll.FileName.Text = "waiting for file.";
            filecontroll.DownloadProgress.Value = 0;
          
            checkfolder();
            processfile();
            dispatcherTimer.Start();
        }

        private void Dwm_onFileDownloadInterrupted(object Sender, downloadEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new fileDownloadEventHandler(Dwm_onFileDownloadInterrupted), new object[] { Sender, args });
                return;
            }
            filecontroll.FileName.Text = "process interrupted,waiting for resume.";
            filecontroll.DownloadProgress.Value = 0;
        }

        private void Dwm_onFileDownloadStopped(object Sender, downloadEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new fileDownloadEventHandler(Dwm_onFileDownloadStopped), new object[] { Sender, args });
                return;
            }
            filecontroll.FileName.Text = args.downloadFile.filename;
        }

        private void Dwm_onFileDownloadProgress(object Sender, downloadEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new fileDownloadEventHandler(Dwm_onFileDownloadProgress), new object[] { Sender, args });
                return;
            }
            filecontroll.DownloadProgress.Value = args.Progress;
        }

        private void Dwm_onFileDownloadComplete(object Sender, downloadEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new fileDownloadEventHandler(Dwm_onFileDownloadComplete), new object[] { Sender, args });
                return;
            }
            filecontroll.FileName.Text = "waiting for file.";
            filecontroll.DownloadProgress.Value = 0;
            if (validatefile(args.downloadFile))
            {
                renamefile(args.downloadFile);

                DOWNLOADQUEUE.Remove(args.downloadFile);
            }
            else
            {

                //delete file
                if (File.Exists(localfolder + "\\" + args.downloadFile.dir + "\\" + args.downloadFile.filename))
                {
                    File.Delete(localfolder + "\\" + args.downloadFile.dir + "\\" + args.downloadFile.filename);
                }

            }
        }

        private void Dwm_onFileDownloadStart(object Sender, downloadEventArgs args)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new fileDownloadEventHandler(Dwm_onFileDownloadStart), new object[] { Sender, args });
                return;
            }
            filecontroll.FileName.Text = "Download Start "+args.downloadFile.filename;
        }

        private void timer_CheckLiveFiles(object sender, EventArgs e)
        {
            processfile();
        }

        private void checkfolder()
        {
            bool ifexist = Directory.Exists(localfolder);
            if (!ifexist)
            {
                Directory.CreateDirectory(localfolder);
            }
            //if (!File.Exists(dqfile))
            //{
            //    File.WriteAllText(dqfile, "[]");
            //}
            //if (!File.Exists(localfile))
            //{
            //    File.WriteAllText(localfile, "[]");
            //}
        }

        private void processfile()
        {
            if (!dwm.isDownloading)
            {
                if (DOWNLOADQUEUE.Count > 0)
                {
                    //MessageBox.Show(DOWNLOADQUEUE.Count+"");
                    // TODO : already pending download process to download.
                    var item = DOWNLOADQUEUE.First();

                    //check the file is same which is trying to download

                    LIVEFILES = GetlivefilesData();
       

                        // MessageBox.Show("processfile" + localfolder + "\\" + item.dir + "\\" + item.filetempname);
                        if (File.Exists(localfolder + "\\" + item.dir+"\\" + item.filetempname) && LIVEFILES.ContainsKey(item.filemd5.Trim()))
                    {
                        
                        long length = new FileInfo(localfolder + "\\" + item.dir + "\\" + item.filetempname).Length;
                        long a = length / 1024;
                    
                        item.startpoint = (a * 1024);
                       
                        filecontroll.DownloadProgress.Value = (int)((100 * item.startpoint) / item.filesize);

                        dwm = new downloadManager();
                        dwm.onFileDownloadStart += Dwm_onFileDownloadStart;
                        dwm.onFileDownloadComplete += Dwm_onFileDownloadComplete;
                        dwm.onFileDownloadProgress += Dwm_onFileDownloadProgress;
                        dwm.onFileDownloadStopped += Dwm_onFileDownloadStopped;
                        dwm.onFileDownloadInterrupted += Dwm_onFileDownloadInterrupted;
                        dwm.beginDownload(item);
                    }
                }
                else
                {
                    createdownloadqueue();
                }
            }
        }
        private List<String> DirSearch(string sDir)
        {
            List<String> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(DirSearch(d));
                }
            }
            catch (Exception ex)
            {
                Reset_On_Error(ex);
            }

            return files;
        }
        private   void createdownloadqueue()
        {
            // stop timer so it can not intrupt on going process
            dispatcherTimer.Stop();
            // get local file dictionary.

            try
            {
                List<myfile> currentfiles = new List<myfile>();

                List<String> fileEntries = DirSearch(localfolder);

                LOCALFILES = new Dictionary<string, myfile>();

                foreach (string fileName in fileEntries)
                {
                    FileInfo fi = new FileInfo(fileName);

                    if (fi.Extension != ".part")
                    {

                        myfile mf = new myfile();
                        mf.name = fi.Name;
                        mf.size = fi.Length;

                        mf.dir = System.IO.Path.GetDirectoryName(fileName);

                        mf.md5 = createmd5(fileName).ToLower();


                        if (!LOCALFILES.ContainsKey(mf.md5))
                        {

                            LOCALFILES.Add(mf.md5, mf);
                        }


                    }
                    else if (fi.Extension == ".part")
                    {
                        DOWNLOADPENDING.Add(System.IO.Path.GetFileNameWithoutExtension(fi.Name));
                    }
                }

                // get live file dictionary.
               
                LIVEFILES = GetlivefilesData();
                // check both dictionary and manage downloadqueue.

                foreach (String k in LOCALFILES.Keys)
                {

                    var i = LIVEFILES.ContainsKey(k.Trim());
                
                    if (!i)
                    {
                        var localitem = LOCALFILES[k];

                    
                        if (File.Exists(localitem.dir + "\\" + localitem.name))
                        {
                            File.Delete(localitem.dir + "\\" + localitem.name);
                            showNotificationMessage(localitem.name + "removing...", 0);
                        }
                         LOCALFILES.Remove(k.Trim());
                         LOCALFILES.Clear();


                    }
                }

                // cross checking of existing file to live files.
                while (DOWNLOADPENDING.Count > 0)
                {
                    String k = DOWNLOADPENDING.First();
                    var i = LIVEFILES.ContainsKey(k);
                    if (i)
                    {
                        var item = LIVEFILES[k];
                        downloadfile df = new downloadfile(item.name, item.md5 + ".part", item.md5, item.size, 0, item.dir);
                    
                        myfile mf = new myfile();
                        mf.name = item.name;
                        mf.size = item.size;
                        mf.md5 = item.md5;
                        LOCALFILES.Add(mf.md5, mf);

                        DOWNLOADQUEUE.Add(df);
                    }
                    else
                    {
                        //TODO:delete the unwanted .partfiles
                       // MessageBox.Show(k+"");
                      //  var localitem = LOCALFILES[k];
                       
                      //  if (File.Exists(localfolder + "\\" + localitem.dir+"\\"+k + ".part"))
                      //  {
                      //      File.Delete(k + ".part");
                      //  }
                        // deletefile(k + ".part");
                    }
                    DOWNLOADPENDING.Remove(k);
                }

                // checking for new file
                foreach (String lv in LIVEFILES.Keys)
                {
                    var i = LOCALFILES.ContainsKey(lv);
                    if (!i)
                    {
                        var item = LIVEFILES[lv];


                        if (!File.Exists(localfolder + "\\" + item.dir + "\\" + item.md5 + ".part"))
                        {
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(localfolder + "\\" + item.dir + "\\" + item.md5 + ".part"));
                    
                            File.Create(localfolder + "\\" + item.dir + "\\" + item.md5 + ".part").Close();
                        }
                        downloadfile df = new downloadfile(item.name, item.md5 + ".part", item.md5, item.size, 0, item.dir);
                        //  MessageBox.Show("new" + item.name + item.md5 + ".part"  + item.dir);
                        DOWNLOADQUEUE.Add(df);
                    }
                }
                dispatcherTimer.Start();

            }
            catch (Exception ex)
            {
                Reset_On_Error(ex);
         
            }
        }

        private Dictionary<string, myfile> GetlivefilesData()
        {
                 List<myfile> lfs = null;

            try
            {


                string livedata = Getlivefiles();
                lfs = JsonConvert.DeserializeObject<List<myfile>>(livedata).ToList<myfile>();
            }
            catch (Exception ex)
            {
                Reset_On_Error(ex);

            }


            LIVEFILES = new Dictionary<string, myfile>();

            foreach (myfile f in lfs)
            {


                if (!LIVEFILES.ContainsKey(f.md5))
                {

                    LIVEFILES.Add(f.md5, f);
                }


            }

            return LIVEFILES;
        }

        private void Reset_On_Error(Exception ex)
        {
            DOWNLOADQUEUE.Clear();
            LOCALFILES.Clear();
            LIVEFILES.Clear();
            LogError(ex);
            dispatcherTimer.Start();
        }

        private void showNotificationMessage(String msg,float per)
        {
            filecontroll.FileName.Text = msg;
            filecontroll.DownloadProgress.Value =per;
        }

        private string Getlivefiles()
        {

            String result = string.Empty;
            try
            {
                HttpWebRequest fileUrlRequest = (HttpWebRequest)WebRequest.Create(url);
                var myHttpWebRequest = (HttpWebRequest)fileUrlRequest;
                String username = "cacm";
                String password = "onebook";
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + encoded);

                WebResponse fileUrlResponse = myHttpWebRequest.GetResponse();
                using (Stream stream = fileUrlResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    result = reader.ReadToEnd();
                }
                return result;
            }
            catch (WebException ex)
            {
                Reset_On_Error(ex);
               
                return null;

            }

        }



        private bool validatefile(downloadfile file)
        {
            bool isvalid = false;
            string filename = localfolder + "\\" + file.dir + "\\" + file.filetempname;
           // MessageBox.Show(file.filemd5 + "////" + createmd5(filename));
            if (File.Exists(filename) &&(createmd5(filename)== file.filemd5))
            {
       
                isvalid = true;
            }
            return isvalid;
        }

        private string createmd5(String fname)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fname))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }
                    return sb.ToString().ToLower();
                }
            }


        }

        private void renamefile(downloadfile file)
        {
            var filepath = localfolder + "\\" + file.dir + "\\" + file.filetempname;
            long length = new System.IO.FileInfo(filepath).Length;
            if (File.Exists(filepath) & length>0)
            {
                try
                              
                {
                
                    File.Move(localfolder + "\\" + file.dir + "\\" + file.filetempname, localfolder + "\\" + file.dir + "\\" + file.filename);
                }
                catch (Exception ex)
                {
                   
                    Reset_On_Error(ex);
                }
            }
        }

        private void LogError(Exception ex)
        {
            string message = string.Format("Time: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
            message += Environment.NewLine;
            message += "-----------------------------------------------------------";
            message += Environment.NewLine;
            message += string.Format("Message: {0}", ex.Message);
            message += Environment.NewLine;
            message += string.Format("Source: {0}", ex.Source);
            message += Environment.NewLine;
            message += "-----------------------------------------------------------";
            message += Environment.NewLine;

            string directory = AppDomain.CurrentDomain.BaseDirectory + "Errorlog.txt";

            using (StreamWriter writer = new StreamWriter(directory, true))
            {
                writer.WriteLine(message);
                writer.Close();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            sett win2 = new sett();

            win2.Show();
        }
    }
}