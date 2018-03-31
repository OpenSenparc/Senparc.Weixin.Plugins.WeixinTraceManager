using Senparc.Weixin.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Senparc.Weixin.Plugins.WeixinTraceManager
{
    public static class WeixinTraceHelper
    {

#if NET40 || NET45 || NET461
        public static string DefaultLogPath { get; set; } = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "App_Data", "WeixinTraceLog");
#else
        public static string DefaultLogPath { get; set; } =  Path.Combine(Senparc.Weixin.Config.RootDictionaryPath, "App_Data", "WeixinTraceLog");
#endif

        /// <summary>
        /// 获取所有日期列表
        /// </summary>
        /// <returns></returns>
        public static List<string> GetLogDate()
        {
            var files = System.IO.Directory.GetFiles(DefaultLogPath, "*.log");
            return files.Select(z => Path.GetFileNameWithoutExtension(z).Replace("SenparcWeixinTrace-", "")).OrderByDescending(z => z).ToList();
        }

        /// <summary>
        /// 获取指定日期的日志
        /// </summary>
        /// <returns></returns>
        public static List<WeixinTraceItem> GetAllLogs(string date)
        {
            var logFile = Path.Combine(DefaultLogPath, string.Format("SenparcWeixinTrace-{0}.log", date));

            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            string bakFilename = logFile + ".bak";//备份文件名
            System.IO.File.Delete(bakFilename);
            System.IO.File.Copy(logFile, bakFilename, true);//读取备份文件，以免资源占用

            var logList = new List<WeixinTraceItem>();

            using (StreamReader sr = new StreamReader(bakFilename, Encoding.UTF8))
            {
                string lineText = null;
                int line = 0;
                var readPostData = false;
                var readResult = false;
                var readExceptionStackTrace = false;

                WeixinTraceItem log = new WeixinTraceItem();
                while ((lineText = sr.ReadLine()) != null)
                {
                    line++;

                    lineText = lineText.Trim();


                    var startExceptionRegex = Regex.Match(lineText, @"(?<=\[{3})(\S+)(?=Exception(\]{3}))");

                    if (startExceptionRegex.Success)
                    {
                        //一个片段的开始（异常）
                        log = new WeixinTraceItem();
                        logList.Add(log);
                        log.Title = "【{0}Exception】异常！".FormatWith(startExceptionRegex.Value);//记录标题
                        log.Line = line;
                        log.IsException = true;
                        log.weixinTraceType = WeixinTraceType.Exception;

                        readPostData = false;
                        readResult = false;
                        readExceptionStackTrace = false;
                        continue;
                    }

                    //其他自定义类型
                    var startRegex = Regex.Match(lineText, @"(?<=\[{3})(\S+)(?=\]{3})");
                    if (startRegex.Success)
                    {
                        //一个片段的开始
                        log = new WeixinTraceItem();
                        logList.Add(log);
                        log.Title = startRegex.Value;//记录标题
                        log.Line = line;

                        readPostData = false;
                        readResult = false;
                        readExceptionStackTrace = false;
                        continue;
                    }



                    var threadRegex = Regex.Match(lineText, @"(?<=\[{1}线程：)(\d+)(?=\]{1})");
                    if (threadRegex.Success)
                    {
                        //线程
                        log.ThreadId = int.Parse(threadRegex.Value);
                        continue;
                    }

                    var timeRegex = Regex.Match(lineText, @"(?<=\[{1})([\s\S]{8,30})(?=\]{1})");
                    if (timeRegex.Success && string.IsNullOrEmpty(log.DateTime))
                    {
                        //时间
                        log.DateTime = timeRegex.Value;
                        continue;
                    }


                    //内容
                    log.Result.TotalResult += lineText + "\r\n";

                    if (readPostData)
                    {
                        log.Result.PostData += lineText + "\r\n";
                        continue;//一直读到底
                    }


                    if (lineText.StartsWith("URL："))
                    {
                        log.Result.Url = lineText.Replace("URL：", "");

                        if (WeixinTraceType.Normal ==  log.weixinTraceType)
                        {
                            log.weixinTraceType = WeixinTraceType.API;
                        }
                        //log.weixinTraceType = log.weixinTraceType | WeixinTraceType.API;
                    }
                    else if (lineText == "Post Data：")
                    {
                        log.weixinTraceType = WeixinTraceType.PostRequest;//POST请求
                        readPostData = true;
                    }
                    else if (lineText == "Result：" || readResult)
                    {
                        log.Result.Result += lineText.Replace("Result：", "") + "\r\n";
                        readResult = true;

                        if (WeixinTraceType.PostRequest != log.weixinTraceType)
                        {
                            log.weixinTraceType = WeixinTraceType.GetRequest;//GET请求
                        }
                    }

                    if (log.IsException)
                    {
                        //异常信息处理
                        if (lineText.StartsWith("AccessTokenOrAppId："))
                        {
                            log.Result.ExceptionAccessTokenOrAppId = lineText.Replace("AccessTokenOrAppId：", "");
                        }
                        else if (lineText.StartsWith("Message：") || lineText.StartsWith("errcode："))
                        {
                            log.Result.ExceptionMessage = lineText.Replace("Message：", "");//“errcode：”保留
                        }
                        else if (lineText.StartsWith("StackTrace："))
                        {
                            log.Result.ExceptionStackTrace = lineText.Replace("StackTrace：", "");
                            readExceptionStackTrace = true;
                        }
                        else if (readExceptionStackTrace)
                        {
                            log.Result.ExceptionStackTrace = "\r\n" + lineText;
                        }
                    }
                }
            }

            System.IO.File.Delete(bakFilename);//删除备份文件

            logList.Reverse();//翻转序列
            return logList;
        }
    }
}
