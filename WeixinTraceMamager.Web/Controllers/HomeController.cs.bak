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
            var vd = new Home_DateLogVD()
            {
                Date = date,
                WeixinTraceItemList = WeixinTraceHelper.GetAllLogs(date)
            };

            return View(vd);
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