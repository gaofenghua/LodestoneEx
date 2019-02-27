﻿using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Xml;
using System.IO;

namespace TransactionServer.Base
{
    /// <summary>
    /// Utilities
    /// </summary>
    public class ServiceTools : System.Configuration.IConfigurationSectionHandler
    {

        public static readonly object writeLock = new object();

        /// <summary>
        /// AppSettings key's value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key].ToString();
        }

        /// <summary>
        /// configSections node
        /// </summary>
        /// <returns></returns>
        public static XmlNode GetConfigSections()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath);
            return doc.DocumentElement.FirstChild;
        }

        /// <summary>
        /// section node
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public static NameValueCollection GetSection(string nodeName)
        {
            return (NameValueCollection)ConfigurationManager.GetSection(nodeName);
        }

        /// <summary>
        /// stop Windows Service
        /// </summary>
        /// <param name="serviceName"></param>
        public static void WindowsServiceStop(string serviceName)
        {
            System.ServiceProcess.ServiceController control = new System.ServiceProcess.ServiceController(serviceName);
            control.Stop();
            control.Dispose();
        }

        /// <summary>
        /// write log file
        /// </summary>
        /// <param name="path">log path</param>
        /// <param name="cont">log content</param>
        /// <param name="isAppend"></param>
        public static void WriteLog(string path, string cont, bool isAppend)
        {
            lock (writeLock)
            {
                using (StreamWriter sw = new StreamWriter(path, isAppend, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine(cont);
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Implement of Interface, to read/write app.config
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            System.Configuration.NameValueSectionHandler handler = new System.Configuration.NameValueSectionHandler();
            return handler.Create(parent, configContext, section);
        }
    }
}
