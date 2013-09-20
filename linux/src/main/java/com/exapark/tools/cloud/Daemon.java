/*
 * Async daemon implementation
 */
package com.exapark.tools.cloud;

import java.util.List;

import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;

import com.amazonaws.services.ec2.model.Instance;
import com.amazonaws.services.ec2.model.Tag;
import com.google.inject.Inject;
import com.google.inject.Injector;
import com.google.inject.name.Named;

/**
 * Exapark server discoverer daemon thread.
 * 
 * @author Yury Shevchenko
 */
public class Daemon extends Thread {

    /** Name of daemon process */
    private static final String DAEMON_NAME = "Exapark Server Discoverer";

    /**
     * Logger.
     */
    private final Log log = LogFactory.getLog(Daemon.class);
    
    private int failCnt = 0;

    /**
     * Thread sleep interval.
     */
    @Inject
    private @Named("PollInterval")
    Long pollInterval;

    @Inject
    private Injector guice;

    /**
     * Constructor.
     */
    public Daemon() {
        super(DAEMON_NAME);
    }

    /**
     * Main cycle implemented here
     */
    @Override
    public void run() {
        try {
            log.info("Daemon starting up");
            while (true) {
                processIteration();
                sleep(pollInterval * 1000);
            }
        } catch (final Exception ex) {
            log.error("Error of runing daemon.", ex);
        }
    }

    /**
     * Method contains main business logic.
     */
    private void processIteration() throws Exception {
        try {
            AmazonEC2Helper ec2Helper = guice.getInstance(AmazonEC2Helper.class);
            final List<Instance> instances = ec2Helper.getRunningInstances();
            HostsManager hostsManager = guice.getInstance(HostsManager.class);
            hostsManager.processHosts(instances);
            final Instance instance = ec2Helper.getCurrentInstance();
            if (instance == null) {
                log.warn("Current instance not found");
                return;
            }
            if (ec2Helper.isElasticIPAssigned(instance)) {
                // ElasticIP assigned to instance update tags
                final Tag tag = new Tag(AmazonEC2Helper.TAG_ELASTIC_IP, instance.getPublicIpAddress());
                ec2Helper.addTagToInstance(instance.getInstanceId(), tag);
            } else {
                // ElasticIP is not assigned update tags
                final Tag tag = new Tag(AmazonEC2Helper.TAG_ELASTIC_IP, "");
                ec2Helper.addTagToInstance(instance.getInstanceId(), tag);
            }
            failCnt = 0;
        } catch (final Exception e) {
            if(failCnt >= 5) {
                throw e;
            } else {
                failCnt++;
                sleep(pollInterval * 1000);
                processIteration();
            }
        }
    }
}
