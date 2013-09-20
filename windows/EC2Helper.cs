using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Net;
using System.Net.NetworkInformation;

namespace com.exapark.tools.cloud
{
    internal class EC2Helper
    {

        /// <summary>
        /// Prevent explicit instance creation
        /// </summary>
        private EC2Helper()
        {
            //create amazon client instance
            AmazonEC2Config config = new AmazonEC2Config();
            AppConfig.Refresh();
            config.ServiceURL = AppConfig.AWSServiceURL;
            _ec2 = new AmazonEC2Client(config);
        }

        /// <summary>
        /// Access helper instance
        /// </summary>
        /// <returns>instance referance</returns>
        public static EC2Helper Make()
        {
            //create instance if there is no one yet
            if (_singleton == null)
                _singleton = new EC2Helper();
            //return instance referance
            return _singleton;
        }
       
        /// <summary>
        ///  Get running instances from Amazon for configured region.
        /// </summary>
        /// <returns>List of running instances</returns>
        public List<RunningInstance> GetRunningInstances()
        {
            // list running instances
            List<RunningInstance> runningInstances = new List<RunningInstance>();

            // ask service for descriptions
            DescribeInstancesResult serviceResult;
            if (!string.IsNullOrEmpty(AppConfig.FilterByTag))
            {
                RunningInstance currentInstance = GetCurrentInstance();
                String currentInstancegroupvalue = GetCurrentInstanceGroupValue(currentInstance);
                // ask service for descriptions
                serviceResult =
               _ec2.DescribeInstances(new DescribeInstancesRequest().WithFilter(new Filter().WithName("tag:" + AppConfig.FilterByTag).WithValue(currentInstancegroupvalue))).DescribeInstancesResult;
            }
            else
            {
                serviceResult =
               _ec2.DescribeInstances(new DescribeInstancesRequest()).DescribeInstancesResult;

            }

            // with all reservations
            // reservation is a group of instances launched with the same command
            foreach (Reservation reservation in serviceResult.Reservation)
            {
                // these are not only really running ones...
                foreach (RunningInstance instance in reservation.RunningInstance)
                {
                    // deal only with really running instances
                    if (instance.InstanceState.Code == RUNNING_STATE)
                    {
                        runningInstances.Add(instance);
                    }
                }
            }
            return runningInstances;
        }

        /// <summary>
        /// Gets instance tag name or instance id if no tag found.
        /// </summary>
        /// <param name="instance">Amazon instance</param>
        /// <returns>Instance name</returns>
        public String GetInstanceName(RunningInstance instance)
        {
            foreach (Tag tag in instance.Tag)
            {
                if (tag.Key.Equals(TAG_INSTANCE_NAME, StringComparison.OrdinalIgnoreCase))
                    return tag.Value;
            }

            return instance.InstanceId;
        }

        /// <summary>
        /// Gets instance tag ElasticIP or NULL id if no tag found.
        /// </summary>
        /// <param name="instance">Amazon instance</param>
        /// <returns>ElasticIP or NULL</returns>
        public String GetElasticIpTagValue(RunningInstance instance)
        {
            foreach (Tag tag in instance.Tag)
            {
                if (tag.Key.Equals(TAG_ELASTIC_IP, StringComparison.OrdinalIgnoreCase))
                    return tag.Value;
            }

            return null;
        }

        /// <summary>
        /// Associates ElasticIP to Instance.
        /// </summary>
        /// <param name="instanceId">InstanceId</param>
        /// <param name="publicIpAddress">Elastic IP Address</param>
        public void SetElasticIpToInstance(String instanceId, String publicIpAddress)
        {
            if (!string.IsNullOrEmpty(instanceId) && !string.IsNullOrEmpty(publicIpAddress))
            {
                AssociateAddressRequest request = new AssociateAddressRequest();
                request.InstanceId = instanceId;
                request.PublicIp = publicIpAddress;

                _ec2.AssociateAddress(request);
            }
        }

        /// <summary>
        ///  Get current instance
        /// </summary>
        /// <returns>Current instance or NULL if instance not found</returns>
        public RunningInstance GetCurrentInstance()
        {
            DescribeInstancesResult serviceResult =
                _ec2.DescribeInstances(new DescribeInstancesRequest()).DescribeInstancesResult;
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
        /// Gets current instance from running instances list
        /// </summary>
        /// <param name="instances">Running instances list</param>
        /// <returns>Current instance or NULL if instance not found</returns>
        public RunningInstance GetCurrentInstance(List<RunningInstance> instances)
        {
            // these are not only really running ones...
            foreach (RunningInstance instance in instances)
            {
                // deal only with really running instances
                if (instance.InstanceState.Code == RUNNING_STATE && CheckIpAddress(instance.PrivateIpAddress)) 
                    return instance;
            }

            return null;
        }

        /// <summary>
        /// Checks local IP.
        /// </summary>
        /// <param name="ipAddr">IP Address</param>
        /// <returns>True if specified IP is equals to local one.</returns>
        private bool CheckIpAddress(string ipAddr)
        {
            System.Net.NetworkInformation.NetworkInterface[] netInterfaces = null;
            try
            {
                netInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (NetworkInformationException ex)
            {
                Logger.Log(ex.Message, EventLogEntryType.Error);
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
        /// Checks if elastic IP is assigned
        /// </summary>
        /// <param name="instance">Amazon instance</param>
        /// <returns>True if ElasticIP is assigned to the current instance.</returns>
        public bool IsElasticIpAssigned(RunningInstance instance)
        {
            if (null != instance)
            {
                DescribeAddressesResult describeAddressesResult =
                    _ec2.DescribeAddresses(new DescribeAddressesRequest()).DescribeAddressesResult;
                foreach (Address address in describeAddressesResult.Address)
                {
                    if (instance.InstanceId.Equals(address.InstanceId, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Checks if ElasticIP is free to assign
        /// </summary>
        /// <param name="elasticIp">IP to check</param>
        /// <returns>TRUE if ElasticIP is free to assign</returns>
        public bool IsElasticIpFree(String elasticIp)
        {
            DescribeAddressesResult describeAddressesResult =
                _ec2.DescribeAddresses(new DescribeAddressesRequest()).DescribeAddressesResult;

            foreach (Address address in describeAddressesResult.Address)
            {
                if (address.PublicIp == elasticIp && string.IsNullOrEmpty(address.InstanceId))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets tag witn ElasticIP key or null
        /// </summary>
        /// <param name="instance">Instance to search in</param>
        /// <returns>ElasticIP tag or null if not found</returns>
        public Tag GetElasticIpTag(RunningInstance instance)
        {
            if (null != instance)
            {
                foreach (Tag tag in instance.Tag)
                    if (tag.Key == TAG_ELASTIC_IP)
                        return tag;
            }

            return null;
        }

        /// <summary>
        /// Retrieves current Amazon instance, check tags for ElasticIP and try to assign if it is possible.
        /// </summary>
        public void AssignElasticIpByTag(RunningInstance instance)
        {
            var tag = GetElasticIpTag(instance);
            AssignElasticIpByTag(instance, tag);
        }

        /// <summary>
        /// Retrieves current Amazon instance, check tags for ElasticIP and try to assign if it is possible.
        /// </summary>
        public void AssignElasticIpByTag(RunningInstance instance, Tag tag)
        {
            if (instance != null && tag != null)
            {
                if (IsElasticIpFree(tag.Value))
                    SetElasticIpToInstance(instance.InstanceId, tag.Value);
            }
        }

        /// <summary>
        /// Adds tag to specified instance.
        /// </summary>
        /// <param name="instanceId">InstanceId</param>
        /// <param name="tag">Tag (key-value pair)</param>
        public void AddTagToInstance(String instanceId, Tag tag)
        {
            CreateTagsRequest createTagsRequest = new CreateTagsRequest();
            createTagsRequest.WithResourceId(instanceId);
            createTagsRequest.WithTag(tag);
            _ec2.CreateTags(createTagsRequest);
        }

        /// <summary>
        /// Deletes tag
        /// </summary>
        /// <param name="instanceId">InstanceId</param>
        /// <param name="tag">Tag (key-value pair)</param>
        public void DeleteTag(string instanceId, Tag tag)
        {
            if (tag == null)
                return;

            DeleteTags deleteTag = new DeleteTags()
                .WithKey(tag.Key)
                .WithValue(tag.Value);

            DeleteTag(instanceId, deleteTag);
        }
        
        /// <summary>
        /// Deletes tag from specified instance.
        /// </summary>
        /// <param name="instanceId">InstanceId</param>
        /// <param name="tag">Tag (key-value pair)</param>
        public void DeleteTag(string instanceId, DeleteTags tag)
        {
            if (tag == null)
                return;

            DeleteTagsRequest deleteTagsRequest = new DeleteTagsRequest()
                .WithResourceId(instanceId)
                .WithTag(tag);

            _ec2.DeleteTags(deleteTagsRequest);
        }

        /// <summary>
        /// Gets instance tag Groupvalue or NULL id if no tag found.
        /// </summary>
        /// <param name="instance">Amazon instance</param>
        /// <returns>Group Tag value or NULL</returns>

        public String GetCurrentInstanceGroupValue(RunningInstance instance)
        {
            foreach (Tag tag in instance.Tag)
            {
                if (tag.Key.Equals(AppConfig.FilterByTag, StringComparison.OrdinalIgnoreCase))
                    return tag.Value;
            }

            return null;

        }

        //singleton pattern implementation
        private static EC2Helper _singleton;

        //Amazon client
        private readonly AmazonEC2Client _ec2;

        //Bit mask of running instance
        public const byte RUNNING_STATE = 16;

        //tag name for instance name
        public const String TAG_INSTANCE_NAME = "Name";

        //tag name for EIP.
        public const String TAG_ELASTIC_IP = "ElasticIP";
    }
}
