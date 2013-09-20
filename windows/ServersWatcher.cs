using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace com.exapark.tools.cloud
{
    /// <summary>
    /// Get description of running instances
    /// </summary>
    public class ServersWatcher
    {
        /// <summary>
        /// List of instances
        /// </summary>
        static public List<RunningInstance> Instances
        {
            get { return GetRunningInstances(); }
        }

        /// <summary>
        /// Verifies credentials from app.config
        /// </summary>
        static public bool CredentialsCorrect
        {
            get
            {
                AppConfig.Refresh();

                // Credentials are empty?
                if (string.IsNullOrEmpty(AppConfig.AWSAccessKey) || string.IsNullOrEmpty(AppConfig.AWSSecretKey))
                    return false;

                // Trying to connect to EC2
                try
                {
                    AmazonEC2Config config = new AmazonEC2Config();

                    config.ServiceURL = AppConfig.AWSServiceURL;

                    AmazonEC2 ec2 = AWSClientFactory.CreateAmazonEC2Client(
                        AppConfig.AWSAccessKey,
                        AppConfig.AWSSecretKey,
                        config
                        );

                    DescribeInstancesRequest ec2Request = new DescribeInstancesRequest();
                    ec2.DescribeInstances(ec2Request);
                }
                catch (AmazonEC2Exception)
                {
                    // Somethong went wrong - incorrect credentials
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Get list of running instances in region for
        /// web service URL in application config
        /// </summary>
        /// <returns></returns>
        static private List<RunningInstance> GetRunningInstances()
        {
            AppConfig.Refresh();
            AmazonEC2Config config = new AmazonEC2Config();

            config.ServiceURL = AppConfig.AWSServiceURL;

            AmazonEC2 ec2 = AWSClientFactory.CreateAmazonEC2Client(
                    AppConfig.AWSAccessKey,
                    AppConfig.AWSSecretKey,
                    config
                    );

            
            //list of running instances
            List<RunningInstance> runningInstancesList = new List<RunningInstance>();
            DescribeInstancesResult serviceResult;

            if (!string.IsNullOrEmpty(AppConfig.FilterByTag))
            {
                RunningInstance currentInstance = GetCurrentInstance(ec2);
                String currentInstancegroupvalue = GetCurrentInstanceGroupValue(currentInstance);
                // ask service for descriptions
                serviceResult =
               ec2.DescribeInstances(new DescribeInstancesRequest().WithFilter(new Filter().WithName("tag:" + AppConfig.FilterByTag).WithValue(currentInstancegroupvalue))).DescribeInstancesResult;
            }
            else
            {
                serviceResult =
               ec2.DescribeInstances(new DescribeInstancesRequest()).DescribeInstancesResult;

            }

            if (serviceResult.IsSetReservation())
            {
                //reservation is a group of instances launched from the same console
                List<Reservation> reservationList = serviceResult.Reservation;
                foreach (Reservation reservation in reservationList)
                {
                    if (reservation.IsSetRunningInstance())
                    {
                        //with all instances in reservation
                        //This list contains not only running instances
                        List<RunningInstance> instancesList = reservation.RunningInstance;
                        foreach (RunningInstance instance in instancesList)
                        { //include in result only really running ones
                            if (RUNNING_STATE == instance.InstanceState.Code)
                                runningInstancesList.Add(instance);
                        }
                    }
                }
            }

            return runningInstancesList;
        }

        /// <summary>
        /// Get value of Name tag or instance id if no Name tag found
        /// </summary>
        /// <param name="instance">Instance referance</param>
        /// <returns>value of Name tag or instance id if no Name tag found</returns>
        public static string GetInstanceName(RunningInstance instance)
        {
            foreach (Tag tag in instance.Tag)
                if (tag.Key.Equals( TAG_INSTANCE_NAME))
                    return tag.Value;

            return instance.InstanceId;
        }

        /// <summary>
        ///  Get current instance
        /// </summary>
        /// <returns>Current instance or NULL if instance not found</returns>
        public static RunningInstance GetCurrentInstance(AmazonEC2 ec2)
        {
           
            DescribeInstancesResult serviceResult =
                ec2.DescribeInstances(new DescribeInstancesRequest()).DescribeInstancesResult;
            foreach (Reservation reservation in serviceResult.Reservation)
            {
                // these are not only really running ones...
                foreach (RunningInstance instance in reservation.RunningInstance)
                {
                    // deal only with really running instances
                    if (instance.InstanceState.Code == RUNNING_STATE && CheckIpAddress(instance.PrivateIpAddress))
                        return instance;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks local IP.
        /// </summary>
        /// <param name="ipAddr">IP Address</param>
        /// <returns>True if specified IP is equals to local one.</returns>
        private static bool CheckIpAddress(string ipAddr)
        {
            System.Net.NetworkInformation.NetworkInterface[] netInterfaces = null;
            try
            {
                netInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (NetworkInformationException ex)
            {
                //Logger.Log(ex.Message, EventLogEntryType.ERROR);
            }
            if (netInterfaces != null && !string.IsNullOrEmpty(ipAddr))
            {
                for (int i = 0; i < netInterfaces.Length; i++)
                {
                    System.Net.NetworkInformation.NetworkInterface netInterface = netInterfaces[i];
                    if (!netInterface.Supports(NetworkInterfaceComponent.IPv4))
                        continue;
                    foreach (UnicastIPAddressInformation addr in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ipAddr == addr.Address.ToString())
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets instance tag Groupvalue or NULL id if no tag found.
        /// </summary>
        /// <param name="instance">Amazon instance</param>
        /// <returns>Group Tag value or NULL</returns>

        public static String GetCurrentInstanceGroupValue(RunningInstance instance)
        {
            foreach (Tag tag in instance.Tag)
            {
                if (tag.Key.Equals(AppConfig.FilterByTag, StringComparison.OrdinalIgnoreCase))
                    return tag.Value;
            }

            return null;

        }

        /// <summary>
        /// Bit mask of running instance
        /// </summary>
        const decimal RUNNING_STATE = 16;

        /// <summary>
        /// tag for instance name
        /// </summary>
        const string TAG_INSTANCE_NAME = "Name";

    }
}
