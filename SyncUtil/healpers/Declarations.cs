using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncUtil.healpers
{
    public delegate void onDownloadProgress(Object sender,onDownloadProgressEventArgs e);
   
    public class onDownloadProgressEventArgs : EventArgs
    {
        public int progress { get; set; }
    }

}
