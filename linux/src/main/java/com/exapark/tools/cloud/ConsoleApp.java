/**
 * 
 */
package com.exapark.tools.cloud;

import java.util.List;

import com.amazonaws.services.ec2.model.Instance;
import com.google.inject.Inject;
import com.google.inject.Singleton;

/**
 * @author Yury Shevchenko
 */
@Singleton
public class ConsoleApp {

    /**
     * Amazon console client.
     */
    @Inject
    private AmazonEC2Helper ec2Helper;

    /**
     * Console application main method.
     */
    public void startApp() {
        final List<Instance> instances = ec2Helper.getRunningInstances();
        for (Instance instance : instances) {
            System.out.printf(
                    "%1$s %2$s" + System.getProperty("line.separator"),
                    ec2Helper.getInstanceName(instance),
                    instance.getPrivateIpAddress());
        }
    }
}
