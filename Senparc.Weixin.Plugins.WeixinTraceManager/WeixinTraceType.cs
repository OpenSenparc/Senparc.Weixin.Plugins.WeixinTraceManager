using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Weixin.Plugins.WeixinTraceManager
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum WeixinTraceType
    {
        Normal = 0,
        API = 2,
        PostRequest = 4,
        GetRequest = 6,
        Exception = 8
    }
}
