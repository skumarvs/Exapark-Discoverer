using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceProcess;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Net.NetworkInformation;
using System.Net;

namespace com.exapark.tools.cloud
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (true == Environment.UserInteractive)
                RunAsConsoleApp();
            else
                RunAsService();
        }

        private static void RunAsConsoleApp()
        {
            if (!ServersWatcher.CredentialsCorrect)
            {
                // In case of empty credentials - show error message and exit
                Console.WriteLine("Error connecting to EC cloud - invalid connect data");
            }
            else
            {
                // Get list of running instances
                List<RunningInstance> instances = ServersWatcher.Instances;

                // Put it to console
                foreach (RunningInstance inst in instances)
                {
                    Console.WriteLine("{0} {1}", ServersWatcher.GetInstanceName(inst), inst.PrivateIpAddress);
                }
            }

            #if (DEBUG) 
            System.Net.NetworkInformation.NetworkInterface[] netInterfaces = null;
            try
            {
                netInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            }
            catch //(NetworkInformationException ex)
            {
                
            }
            if (netInterfaces != null)
            {
                for (int i = 0; i < netInterfaces.Length; i++)
                {
                    System.Net.NetworkInformation.NetworkInterface netInterface = netInterfaces[i];
                    if (!netInterface.Supports(NetworkInterfaceComponent.IPv4))
                        continue;
                    foreach (UnicastIPAddressInformation addr in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        Console.WriteLine(addr.Address);
                    }
                }
            }

            Console.WriteLine("\nService test starts.");
            Console.WriteLine("Start...");
            ConsoleStartTest();
            Console.WriteLine("Press any key...");
            Console.ReadKey();

            Console.WriteLine("Resume...");
            ConsoleInstanceTest();
            Console.WriteLine("Service test ended.\n"); 
#endif

            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }

        private static void ConsoleStartTest()
        {
            try
            {
                List<RunningInstance> instances = ServersWatcher.Instances;

                EC2Helper ec2Helper = EC2Helper.Make();
                RunningInstance instance = ec2Helper.GetCurrentInstance(instances);

                if (instance == null)
                    return;

                Console.WriteLine("Instance: {0}", ServersWatcher.GetInstanceName(instance));

                // Assign elastic IP if it is free and tag exists
                var tag = ec2Helper.GetElasticIpTag(instance);
                if (tag != null)
                {
                    Console.WriteLine("Tag value: {0}", tag.Value);
                    ec2Helper.AssignElasticIpByTag(instance, tag);
                }
                else
                    Console.WriteLine("Tag not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void ConsoleInstanceTest()
        {
            try
            {
                List<RunningInstance> instances = ServersWatcher.Instances;
                
                //Apply ElasticIP for current host:
                EC2Helper ec2Helper = EC2Helper.Make();

                RunningInstance instance = ec2Helper.GetCurrentInstance(instances);
                if (instance == null)
                    return;

                Console.WriteLine("Instance: {0}", ServersWatcher.GetInstanceName(instance));

                if (ec2Helper.IsElasticIpAssigned(instance))
                {
                    Console.WriteLine("ElasticIP assigned to instance: {0}", instance.IpAddress);

                    // ElasticIP assigned to instance update tags
                    Tag tag = new Tag().WithKey(EC2Helper.TAG_ELASTIC_IP).WithValue(instance.IpAddress);
                    ec2Helper.AddTagToInstance(instance.InstanceId, tag);
                }
                else
                {
                    var tag = ec2Helper.GetElasticIpTag(instance);

                    // Delete tag if exists
                    if (tag != null)
                    {
                        Console.WriteLine("Deleting tad value: {0}", tag.Value);
                        ec2Helper.DeleteTag(instance.InstanceId, tag);
                    }
                    else
                        Console.WriteLine("Tag not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void RunAsService()
        {
            ServiceBase.Run(new ServerDiscovererService());
        }
    }
}
