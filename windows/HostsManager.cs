using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.exapark.tools.cloud
{
    /// <summary>
    /// Hosts file controller
    /// </summary>
    public class HostsManager
    {
        /// <summary>
        /// Primary initialization constructor
        /// </summary>
        public HostsManager()
        {
            //started formattion of managed section
            _managedSection = new StringBuilder();
            _managedSection.AppendLine(MANAGED_SECTION_HEADER);
        }

        /// <summary>
        /// Read and parse current file
        /// </summary>
        public void OpenHostsFile()
        {
            //let's open file
            using (StreamReader reader = File.OpenText(HostsFileFullName))
            {
                //read it line by line
                while (!reader.EndOfStream)
                {
                    string singleLine = reader.ReadLine();
                    if (singleLine == MANAGED_SECTION_HEADER)
                    {
                        //begining of managed section detected. Skip it.
                        while (!reader.EndOfStream)
                        {
                            singleLine = reader.ReadLine();
                            if (singleLine == MANAGED_SECTION_FOOTER)
                                break; //stop skipping
                        }
                    }
                    else
                        _sb.AppendLine(singleLine); //add to buffer
                }
            }
        }

        /// <summary>
        /// New record at managed section
        /// </summary>
        /// <param name="ipAddress">ip of host</param>
        /// <param name="hostName">name of host</param>
        public void AddRecord(string ipAddress, string hostName)
        {
            _managedSection.AppendLine(ipAddress + "\t" + hostName);
        }

        /// <summary>
        /// Finish formattion and write it back to file
        /// </summary>
        public void CommitHostsFile()
        {
            //finish managed section:
            _managedSection.Append(MANAGED_SECTION_FOOTER);
            //open hosts file:
            using (StreamWriter writer = File.CreateText(HostsFileFullName))
            {
                //Sequentaly write down:
                //1. original content except managed section
                writer.Write(_sb.ToString());
                //2. new content of managed section
                writer.WriteLine(_managedSection.ToString());
            }
        }

        /// <summary>
        /// Clears hosts file from Exapark managed section
        /// </summary>
        public void ClearHostsFile()
        {
            _sb = new StringBuilder();
            OpenHostsFile();

            using (StreamWriter writer = File.CreateText(HostsFileFullName))
            {
                // original content except managed section
                writer.Write(_sb.ToString());
            }
        }

        /// <summary>
        /// Full path to hosts file, inculded windows\system32
        /// </summary>
        private string HostsFileFullName
        {
            get { return Environment.SystemDirectory + HOSTS_RELATIVE_PATH; }
        }

        /// <summary>
        /// Original content buffer
        /// </summary>
        private StringBuilder _sb = new StringBuilder();

        /// <summary>
        /// Managed section buffer
        /// </summary>
        private readonly StringBuilder _managedSection;

        /// <summary>
        /// Mark of managed section start
        /// </summary>
        private const string MANAGED_SECTION_HEADER = "#Exapark managed section";

        /// <summary>
        /// Mark of managed section end
        /// </summary>
        private const string MANAGED_SECTION_FOOTER = MANAGED_SECTION_HEADER;

        /// <summary>
        /// Relative path to hosts file
        /// </summary>
        private const string HOSTS_RELATIVE_PATH = "\\drivers\\etc\\hosts";
    }
}
