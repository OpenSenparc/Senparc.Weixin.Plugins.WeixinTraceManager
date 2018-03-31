using Newtonsoft.Json;
using Senparc.Weixin.Plugins.WeixinTraceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WeixinTraceMamager.Web.Models;

namespace WeixinTraceMamager.Web.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// 首页列表
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var vd = new Home_IndexVD()
            {
                DateList = WeixinTraceHelper.GetLogDate()
            };

            return View(vd);
        }

        /// <summary>
        /// 单天详情
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public ActionResult DateLog(string date)
        {
            var vd = new Home_DateLogVD() { Date = date, WeixinTraceItemList = WeixinTraceHelper.GetAllLogs(date) };
            Session[date + "DateLog"] = vd;
            return View(vd);
        }



        //根据查询条件获取日期
        public ActionResult GetDateLog(SearchPara para)
        {
            Home_DateLogVD modelAll = new Home_DateLogVD();
            if (para == null || string.IsNullOrEmpty(para.date))
            {
                return Content("抱歉，参数错误，para=" + para);
            }
            var allData = Session[para.date + "DateLog"];

            if (allData == null)
            {
                modelAll = new Home_DateLogVD() { Date = para.date, WeixinTraceItemList = WeixinTraceHelper.GetAllLogs(para.date) };
            }
            else
            {
                modelAll = allData as Home_DateLogVD;
            }

            if (modelAll != null && modelAll.Date == para.date)
            {
                WeixinTraceItem[] list = new WeixinTraceItem[modelAll.WeixinTraceItemList.Count];
                modelAll.WeixinTraceItemList.CopyTo(list);
                if (para.ckExcept)
                {
                    list = list.Where(a => a.IsException).ToArray();
                }
                if (para.ckDetail)
                {
                    list = list.Where(a => a.IsShowDetail).ToArray();
                }
                if (!string.IsNullOrEmpty(para.title))
                {
                    list = list.Where(a => a.Title == para.title).ToArray();
                }
                if (!string.IsNullOrEmpty(para.type))
                {
                    list = list.Where(a => a.weixinTraceType.ToString() == para.type).ToArray();
                }
                if (!string.IsNullOrEmpty(para.thread))
                {
                    list = list.Where(a => a.ThreadId.ToString() == para.thread).ToArray();
                }

                if (!string.IsNullOrEmpty(para.keyWord))
                {
                    var kes = para.keyWord.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    kes.OrderByDescending(x => x.Length);
                    foreach (var item in kes)
                    {
                        list = list.Where(a => JsonConvert.SerializeObject(a.Result).Contains(item)).ToArray();
                    }
                }
                return Content(string.Join(",", list.Select(a => a.Line).ToList()));
            }
            return Content("");

        }




        public class SearchPara
        {
            public string date { get; set; }
            public bool ckExcept { get; set; }
            public bool ckDetail { get; set; }
            public string keyWord { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string thread { get; set; }
        }


        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}