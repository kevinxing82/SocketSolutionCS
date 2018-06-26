﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.kevinxing.socket
{
    public enum SocketConnectStatus
    {
        SUCCESS = 0,
        IP_IS_INVALID,
    }
    public class SocketClient
    {
        public SocketConnectStatus connect(string v1, int v2)
        {
            return SocketConnectStatus.SUCCESS;
        }

        public static bool IsValidIP(string v)
        {
            if ((v.Length > 12)||(v.Length<7))
            {
                return false;
            }
            try
            {
                string[] s= v.Split(new string[] { "." }, StringSplitOptions.None);
                if(s.Length!=4)
                {
                    return false;
                }
                foreach(string i in s)
                {
                    Convert.ToInt32(i);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
