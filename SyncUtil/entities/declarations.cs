using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncUtil.entities
{
    public enum DownloadEvents
    {
        DOWNLOAD_START = 1,
        DOWNLOAD_COMPLETE = 10,
        DOWNLOAD_INTERRUPTED = 20,
        DOWNLOAD_STOPPED = 30,
        DOWNLOAD_PROGRESS = 40,
                DOWNLOAD_DELETE = -1
    }

    public class downloadEventArgs : EventArgs
    {
        public DownloadEvents Event { get; set; }
        public int Progress { get; set; }
        public downloadfile downloadFile { get; set; }
    }

    public delegate void fileDownloadEventHandler(object Sender,downloadEventArgs args);


}
