using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class UdpClientInfo
    {
        public IPEndPoint IPEndPoint { get; set; }
        public AesEncryption aes;
        public string name;
        public UdpClientInfo(IPEndPoint iPEndPoint, AesEncryption aes, string name)
        {
            IPEndPoint = iPEndPoint;
            this.aes = aes;
            this.name = name;
        }
       
    }
}
