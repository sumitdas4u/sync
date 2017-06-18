using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;

namespace SyncUtil.entities
{
    class downloadManager
    {

        private downloadfile _downloadFile;
        private Thread _downloadThread;
        private bool _cancellationPending = false;

        public bool isDownloading { get; set; }

        public event fileDownloadEventHandler onFileDownloadStart;
        public event fileDownloadEventHandler onFileDownloadComplete;
        public event fileDownloadEventHandler onFileDownloadProgress;
        public event fileDownloadEventHandler onFileDownloadInterrupted;
        public event fileDownloadEventHandler onFileDownloadStopped;
        public event fileDownloadEventHandler onFileDeleting;


        public downloadManager()
        {
            _downloadThread = new Thread(() => download(_downloadFile));

            this.isDownloading = false;
        }


        public void beginDownload(downloadfile file)
        {
            if (this.isDownloading)
            {
                throw new Exception("A download is already in progress.");
            }
            _downloadFile = file;
            _downloadThread.Start();
        }

        public void stopDownload()
        {
            _cancellationPending = true;
        }

        private void download(object file)
        {
            this.isDownloading = true;
            downloadfile _file = (downloadfile)file;

            if (onFileDownloadStart != null)
            {
                //Invoked the file download start
                downloadEventArgs args = new downloadEventArgs();
                args.Event = DownloadEvents.DOWNLOAD_START;
                args.downloadFile = _file;
                args.Progress = 0;
                onFileDownloadStart.Invoke(this, args);
                FileStream outFile = null;
                try
                {
                    string localfolder = Properties.Settings.Default.path.ToString();
                     System.Diagnostics.Debug.WriteLine(localfolder + "\\" + _file.dir + "\\" + _file.filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(localfolder + "\\" + _file.dir + "\\" + _file.filetempname));
                    outFile = new FileStream(localfolder + "\\" + _file.dir + "\\" + _file.filetempname, FileMode.OpenOrCreate);

                    string fileUrl = "http://teamcacm.com/onebook/download.php?filename=" + _file.filemd5 + "&startpoint=" + _file.startpoint;



                    HttpWebRequest fileUrlRequest = (HttpWebRequest)WebRequest.Create(fileUrl);
                    var myHttpWebRequest = (HttpWebRequest)fileUrlRequest;
                    String username = "cacm";
                    String password = "onebook";
                    String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                    myHttpWebRequest.Headers.Add("Authorization", "Basic " + encoded);
                    HttpWebResponse fileUrlResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                    // we will read data via the response stream
                    Stream ReceiveStream = fileUrlResponse.GetResponseStream();
                    ReceiveStream.ReadTimeout = 30000;
                    byte[] buffer = new byte[1024];

                    //TODO: Seek to the start point. In case of partial download, seek to resume point
                    outFile.Seek(_file.startpoint, SeekOrigin.Begin);

                    int bytesRead = 0;
                    double downloadsize = _file.startpoint;
                    while ((bytesRead = ReceiveStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        downloadsize = downloadsize + bytesRead;
                        outFile.Write(buffer, 0, bytesRead);
                        //Download completed
                        args = new downloadEventArgs();
                        args.Event = DownloadEvents.DOWNLOAD_PROGRESS;
                        args.downloadFile = _file;
                        args.Progress = (int)((100 * downloadsize) / _file.filesize);
                        onFileDownloadProgress.Invoke(this, args);
                        
                        if (_cancellationPending)
                        {
                            //Download completed
                            args = new downloadEventArgs();
                            args.Event = DownloadEvents.DOWNLOAD_STOPPED;
                            args.downloadFile = _file;
                            args.Progress = (int)((100 * downloadsize) / _file.filesize); ;
                            onFileDownloadStopped.Invoke(this, args);
                            outFile.Close();
                            this.isDownloading = false;
                            return;
                        }
                    }
                    outFile.Close();

                    //Download completed
                    args = new downloadEventArgs();
                    args.Event = DownloadEvents.DOWNLOAD_COMPLETE;
                    args.downloadFile = _file;
                    args.Progress = 100;
                    onFileDownloadComplete.Invoke(this, args);
                    outFile.Close();
                    this.isDownloading = false;
                }
                catch (Exception ex)
                {
                    //Download Interrupted
                    args = new downloadEventArgs();
                    args.Event = DownloadEvents.DOWNLOAD_INTERRUPTED;
                    args.downloadFile = _file;
                    args.Progress = 0;
                    if (outFile != null)
                    {
                        outFile.Close();
                    }
                    onFileDownloadInterrupted.Invoke(this, args);
                    this.isDownloading = false;
                }
            }
        }
    }
}
