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


        //在开始新一条日志记录的时候，对上一条日志的结果进行处理
        static WeixinTraceItem HandData(WeixinTraceItem log, List<String> exMsgList, List<String> exStackList)
        {
            //若前一个log为异常，则进行处理
            if (exMsgList != null && exMsgList.Count > 0)
            {
                log.Result.ExceptionMessage = string.Join(Environment.NewLine, exMsgList);
                log.Result.ExceptionStackTrace = string.Join(Environment.NewLine, exStackList);
                log.IsShowDetail = true;
            }
            else
            {
                //若Result 数据 较长，则进行处理
                string resultOri = log.Result.Result;
                string postOri = log.Result.PostData;
                if (!string.IsNullOrEmpty(resultOri) && resultOri.Length > maxDataLength)
                {
                    string newResult = LongDataToShort(resultOri);
                    if (newResult.Length < resultOri.Length)   //处理成功，需要显示详情数据
                    {
                        log.Result.Result = newResult;
                        log.IsShowDetail = true;
                    }
                    else
                    {
                        log.Result.TotalResult = "";
                    }
                }
                else if (!string.IsNullOrEmpty(postOri) && postOri.Length > maxDataLength)
                {
                    string newResult = LongDataToShort(postOri);
                    if (newResult.Length < postOri.Length)   //处理成功，需要显示详情数据
                    {
                        log.Result.PostData = newResult;
                        log.IsShowDetail = true;
                    }
                    else
                    {
                        log.Result.TotalResult = "";
                    }
                }
                //else if (string.IsNullOrEmpty(postOri) && string.IsNullOrEmpty(postOri))
                //{

                //}
                else if (log.weixinTraceType != WeixinTraceType.Normal)
                {
                    log.Result.TotalResult = "";//若是正常的消息，不显示TotalResult
                }

            }
            return log;
        }

        static int maxDataLength = 1000;


        //将较长的数据变为较短的数据，省略掉中间的数据
        static string LongDataToShort(string oriData)
        {
            oriData = !string.IsNullOrEmpty(oriData) ? oriData : "";
            if (oriData.Length <= maxDataLength)
            {
                return oriData;
            }

            //处理   "},{"  和    ","  
            string splitStr1 = "\"},{\"";    //  "},{"
            string splitStr2 = "\",\"";    //  ","

            char[] specChar = { '#', '?', '&', '!', '$', '%', '？', '。', '，', '、' };

            var realChar = specChar.Where(a => !oriData.Contains(a)).FirstOrDefault();
            if (realChar == 0 || realChar == '0')
            {
                return oriData;
            }
            string[] strArr1 = oriData.Replace(splitStr1, realChar.ToString()).Split(realChar);
            if (strArr1.Length > 10)
            {
                string resultStr = HandDataWithChar(strArr1, splitStr1);
                if (!string.IsNullOrEmpty(resultStr))
                {
                    return resultStr;
                }
            }

            string[] strArr2 = oriData.Replace(splitStr2, realChar.ToString()).Split(realChar);
            if (strArr2.Length > 10)
            {
                string resultStr = HandDataWithChar(strArr2, splitStr2);
                if (!string.IsNullOrEmpty(resultStr))
                {
                    return resultStr;
                }
            }
            return oriData;
        }


        //取最前面length条数据和最后面length条数据，length的长度根据最中间那条的长度决定
        static string HandDataWithChar(string[] strArr1, string splitStr1)
        {
            if (strArr1.Length <= 10)
            {
                return "";
            }

            string midStr = strArr1[strArr1.Length / 2];
            int length = 2;
            if (midStr.Length < 100)
            {
                length = 5;
            }
            List<string> resList = new List<string>();
            for (int i = 0; i < length; i++)
            {
                resList.Add(strArr1[i]);
            }
            resList.Add("【......省略" + (strArr1.Length - 6).ToString() + "组以（" + splitStr1 + "）分隔的数据......】");
            for (int i = length; i > 0; i--)
            {
                resList.Add(strArr1[strArr1.Length - i]);
            }

            string resultStr = string.Join(splitStr1, resList);
            return resultStr;
        }

        /// <summary>
        /// 获取指定日期的日志
        /// </summary>
        /// <returns></returns>
        public static List<WeixinTraceItem> GetAllLogs(string date)
        {
            date = !string.IsNullOrEmpty(date) ? date :  DateTime.Now.AddDays(0).ToString("yyyyMMdd") ;
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
                var readExceptionMsg = false;
                List<String> exMsgList = new List<string>();
                List<String> exStackList = new List<string>();

                WeixinTraceItem log = new WeixinTraceItem();
                while ((lineText = sr.ReadLine()) != null)
                {
                    line++;

                    lineText = lineText.Trim();

                    if (string.IsNullOrEmpty(lineText))
                    {
                        continue;
                    }

                    var startExceptionRegex = Regex.Match(lineText, @"(?<=\[{3})(\S+)(?=Exception(\]{3}))");

                    if (startExceptionRegex.Success)
                    {
                        log = HandData(log, exMsgList, exStackList);

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
                        readExceptionMsg = false;
                        continue;
                    }

                    //其他自定义类型
                    var startRegex = Regex.Match(lineText, @"(?<=\[{3})(\S+)(?=\]{3})");
                    if (startRegex.Success)
                    {
                        log = HandData(log, exMsgList, exStackList);

                        //一个片段的开始
                        log = new WeixinTraceItem();
                        logList.Add(log);
                        log.Title = startRegex.Value;//记录标题
                        log.Line = line;

                        readPostData = false;
                        readResult = false;
                        readExceptionStackTrace = false;
                        readExceptionMsg = false;
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

                        if (WeixinTraceType.Normal == log.weixinTraceType)
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
                            exMsgList = new List<string>();
                            lineText = lineText.Replace("Message：", "");
                            log.Result.ExceptionMessage = lineText;//“errcode：”保留
                            readExceptionMsg = true;
                        }
                        else if (lineText.StartsWith("StackTrace："))
                        {
                            readExceptionMsg = false;
                            log.Result.ExceptionStackTrace = lineText.Replace("StackTrace：", "");
                            readExceptionStackTrace = true;
                            exStackList = new List<string>();
                            continue;
                        }
                        else if (readExceptionStackTrace)
                        {
                            log.Result.ExceptionStackTrace = "\r\n" + lineText;
                        }
                        if (!string.IsNullOrEmpty(lineText))
                        {
                            if (readExceptionMsg)
                            {
                                exMsgList.Add(lineText);
                            }
                            if (readExceptionStackTrace)
                            {
                                exStackList.Add(lineText);
                            }
                        }
                    }
                    else
                    {
                        readExceptionMsg = false;
                        readExceptionStackTrace = false;
                        exMsgList = new List<string>();
                        exStackList = new List<string>();
                    }
                }
                log = HandData(log, exMsgList, exStackList);
            }

            var allThreadNum = logList.Select(a => a.ThreadId).Distinct().Count();
            var exThreadNum = logList.Where(a => a.IsException).Select(a => a.ThreadId).Distinct().Count(); //发生异常的线程数


            System.IO.File.Delete(bakFilename);//删除备份文件

            logList.Reverse();//翻转序列
            return logList;
        }
    }


}
