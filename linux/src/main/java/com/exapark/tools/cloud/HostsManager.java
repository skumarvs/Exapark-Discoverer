/*
 * Host file management
 */
package com.exapark.tools.cloud;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.util.List;

import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;

import com.amazonaws.services.ec2.model.Instance;
import com.google.inject.Inject;
import com.google.inject.Singleton;
import com.google.inject.name.Named;

/**
 * Class interacts with hosts file (/etc/hosts)
 * 
 * @author Yury Shevchenko
 */
@Singleton
public class HostsManager {

    /**
     * Mark of managed section start.
     */
    private static final String MANAGED_SECTION_HEADER = "#Exapark managed section";

    /**
     * Mark of managed section end.
     */
    private static final String MANAGED_SECTION_FOOTER = MANAGED_SECTION_HEADER;

    /**
     * Buffer for original content/
     */
    private StringBuilder sb = new StringBuilder();

    /**
     * Buffer for managed section.
     */
    private StringBuilder managedSection = new StringBuilder();

    /**
     * Logger
     */
    private final Log log = LogFactory.getLog(HostsManager.class);

    /**
     * Amazon API Helper.
     */
    @Inject
    private AmazonEC2Helper ec2Helper;

    /**
     * Hosts file path.
     */
    @Inject
    private @Named("HostsFile") String hostsFile;

    /**
     * Retrieves list of running instances and poppulates /etc/hosts file.
     * 
     * @param instances
     *            Amazon instances list.
     */
    public void processHosts(List<Instance> instances) {
        openHostsFile();
        for (final Instance instance : instances) {
            addRecord(instance.getPrivateIpAddress(), ec2Helper.getInstanceName(instance));
        }
        commitHostsFile();
    }

    /**
     * Read and parse hosts file
     */
    private void openHostsFile() {
        sb = new StringBuilder();
        managedSection = new StringBuilder();
        managedSection.append(MANAGED_SECTION_HEADER);
        managedSection.append(System.getProperty("line.separator")); // line end
        
        // File opening
        BufferedReader reader = null;
        try {
            final File file = new File(hostsFile);
            reader = new BufferedReader(new FileReader(file));
            // read it line by line
            String singleLine = null;
            while ((singleLine = reader.readLine()) != null) {
                if (singleLine.equals(MANAGED_SECTION_HEADER)) {
                    // section start found. Skip it.
                    while ((singleLine = reader.readLine()) != null) {
                        if (singleLine.equals(MANAGED_SECTION_FOOTER)) break; // stop skipping
                    }
                } else {
                    sb.append(singleLine); // add to buffer
                    sb.append(System.getProperty("line.separator")); // line end
                }
            }
        } catch (final Exception ex) {
            log.error(
                    "Error of closing " + hostsFile + " file. Check file and daemon permissions.",
                    ex);
        } finally {
            try {
                if (reader != null) reader.close();
            } catch (final Exception ex) {
                log.error("Error of closing "
                        + hostsFile
                        + " file. Check file and daemon permissions.", ex);
            }

        }
    }

    /**
     * New record in managed section.
     * 
     * @param ipAddress
     *            IP Address
     * @param hostName
     *            Host name
     */
    private void addRecord(String ipAddress, String hostName) {
        managedSection.append(ipAddress);
        managedSection.append("\t");
        managedSection.append(hostName);
        managedSection.append(System.getProperty("line.separator"));
    }

    /**
     * Complete file changes and write it back.
     */
    private void commitHostsFile() {
        // finish managed section:
        managedSection.append(MANAGED_SECTION_FOOTER);
        // write debug information
        log.debug("managedSection:");
        log.debug(managedSection.toString());
        log.debug("Rest of file:");
        log.debug(sb.toString());
        // Откроем файл хостов:
        BufferedWriter writer = null;
        try {
            final File file = new File(hostsFile);
            writer = new BufferedWriter(new FileWriter(file, false));
            // Write to hosts file:
            // 1. original content except managed section
            writer.write(sb.toString());
            // 2. new content of managed section
            writer.write(managedSection.toString());
        } catch (final IOException ex) {
            log.error(
                    "Error of writing " + hostsFile + " file. Check file and daemon permissions.",
                    ex);
        } finally {
            try {
                if (writer != null) writer.close();
            } catch (final IOException ex) {
                log.error("Error of closing "
                        + hostsFile
                        + " file. Check file and daemon permissions.", ex);
            }
        }
    }
}
