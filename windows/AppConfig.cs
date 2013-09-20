using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Configuration;

namespace com.exapark.tools.cloud
{
    /// <summary>
    /// Configuration paramenters holder
    /// </summary>
    class AppConfig
    {
        /// <summary>
        /// Refreshes app.config values in cache
        /// </summary>
        public static void Refresh()
        {
            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        /// Public key for Amazon API -->
        /// </summary>
        public static string AWSAccessKey
        {
            get
            {
                return ConfigurationManager.AppSettings["AWSAccessKey"];
            }
        }

        /// <summary>
        /// Secret key for Amazon API -->
        /// </summary>
        public static string AWSSecretKey
        {
            get
            {
                return ConfigurationManager.AppSettings["AWSSecretKey"];
            }
        }

        /// <summary>
        /// Web service URL for Amazon API of rerion we want to pool for running instances
        /// </summary>
        public static string AWSServiceURL
        {
            get
            {
                return ConfigurationManager.AppSettings["AWSServiceURL"];
            }
        }

        /// <summary>
        /// Filter by Tag -->
        /// </summary>
        public static string FilterByTag
        {
            get
            {
                return ConfigurationManager.AppSettings["FilterByTag"];
            }
        }
        
        /// <summary>
        /// Pooling interval in MILLIseconds to refresh running instances list
        /// </summary>
        public static double AWSPoolingInterval
        {
            get
            {
                // We got seconds from config file and convert to milliseconds
                return Convert.ToDouble(ConfigurationManager.AppSettings["AWSPoolingInterval"]) * 1000;
            }
        }
    }
}
