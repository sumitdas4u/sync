using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncUtil.entities
{
    public class downloadfile
    {
        public String filename { get; set; }
        public String filetempname { get; set; }
        public String filemd5 { get; set; }
        public long filesize { get; set; }
        public long startpoint { get; set; }
        public String dir { get; set; }
        public downloadfile(String fname, String tname, String md5, long fsize,long spoint,String path)
        {
            filename = fname;
            filetempname = tname;
            filemd5 = md5;
            filesize = fsize;
            startpoint = spoint;
            dir = path ;
        }
    }
}
 