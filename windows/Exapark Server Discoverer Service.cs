using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Timer = System.Timers.Timer;

namespace com.exapark.tools.cloud
{
    public partial class ServerDiscovererService : ServiceBase
    {
        public ServerDiscovererService()
        {
            InitializeComponent();

            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _wrongCredentialsErrorMessage = string.Format(_wrongCredentialsErrorMessage, conf.FilePath);
        }

        /// <summary>
        /// On service start actions
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // In case of empty credentials - log error message and stop the service process
            if (!ServersWatcher.CredentialsCorrect)
            {
                LogErrorAndStopService(_wrongCredentialsErrorMessage);
                return;
            }

            try
            {
                // First perform start actions only once
                var startThread = new Thread(OnStartEvent);
                startThread.Start();

                // After that launch timed actions
                _timer = new Timer(AppConfig.AWSPoolingInterval);
                _timer.AutoReset = true;
                _timer.Elapsed += new ElapsedEventHandler(OnTimerEvent);
                _timer.Start();

                Logger.Log("Exapark Server Discoverer Service was started", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                LogErrorAndStopService(ex.Message);
            }
        }

        /// <summary>
        /// On service stop actions
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Dispose();
                }

                HostsManager hostsMgr = new HostsManager();
                hostsMgr.ClearHostsFile();

                Logger.Log("Exapark Server Discoverer Service was stopped", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Logs message with Error entry type and stops service process
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogErrorAndStopService(string message)
        {
            Logger.Log(message, EventLogEntryType.Error);
            this.Stop();
        }

        /// <summary>
        /// Actions on service start
        /// </summary>
        private static void OnStartEvent()
        {
            try
            {
                List<RunningInstance> instances = ServersWatcher.Instances;

                EC2Helper ec2Helper = EC2Helper.Make();
                RunningInstance instance = ec2Helper.GetCurrentInstance(instances);

                if (instance == null)
                    return;

                // Assign elastic IP if it is free and tag exists
                var tag = ec2Helper.GetElasticIpTag(instance);
                if (tag != null)
                    ec2Helper.AssignElasticIpByTag(instance, tag);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, EventLogEntryType.Error);
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Actions on timer event
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnTimerEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                List<RunningInstance> instances = ServersWatcher.Instances;

                // Write running instances to hosts file
                HostsManager hostsMgr = new HostsManager();
                hostsMgr.OpenHostsFile();
                foreach (RunningInstance inst in instances)
                {
                    hostsMgr.AddRecord(inst.PrivateIpAddress, ServersWatcher.GetInstanceName(inst));
                }
                hostsMgr.CommitHostsFile();

                //Apply ElasticIP for current host:
                EC2Helper ec2Helper = EC2Helper.Make();

                RunningInstance instance = ec2Helper.GetCurrentInstance(instances);
                if (instance == null)
                    return;

                if (ec2Helper.IsElasticIpAssigned(instance))
                {
                    // ElasticIP assigned to instance, update tags
                    Tag tag = new Tag().WithKey(EC2Helper.TAG_ELASTIC_IP).WithValue(instance.IpAddress);
                    ec2Helper.AddTagToInstance(instance.InstanceId, tag);
                }
                else
                {
                    var tag = ec2Helper.GetElasticIpTag(instance);
                    
                    // Delete tag if exists
                    if (tag != null)
                        ec2Helper.DeleteTag(instance.InstanceId, tag);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, EventLogEntryType.Error);
                throw new Exception(ex.Message, ex);
            }
        }

        private Timer _timer;
        private readonly string _wrongCredentialsErrorMessage =
            "Exapark Discoverer Service failed to start. Error connecting to EC cloud - invalid connect data. Check config at {0}";
    }
}
    