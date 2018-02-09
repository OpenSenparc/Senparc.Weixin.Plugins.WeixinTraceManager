using Senparc.Weixin.Plugins.WeixinTraceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeixinTraceMamager.Web.Models
{
    public class Home_IndexVD
    {
        public List<string> DateList { get; set; }
    }

    public class Home_DateLogVD
    {
        public string Date { get; set; }
        public List<WeixinTraceItem> WeixinTraceItemList { get; set; }
    }
}