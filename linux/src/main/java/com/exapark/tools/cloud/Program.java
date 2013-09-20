/*
 * Main application entry point
 */
package com.exapark.tools.cloud;

import java.util.Arrays;

import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;

import com.amazonaws.services.ec2.AmazonEC2Client;
import com.exapark.tools.cloud.guice.AmazonEC2ClientProvider;
import com.exapark.tools.cloud.guice.AppConfigModule;
import com.google.inject.AbstractModule;
import com.google.inject.Guice;
import com.google.inject.Injector;

/**
 * Startup class
 */
public class Program {

    /**
     * Parameter name to start application in daemon mode.
     */
    private static final String PARAM_DEMON_MODE = "-d";

    /**
     * Logger.
     */
    private static final Log LOG = LogFactory.getLog(Program.class);

    /**
     * @param args
     *            command line parameters
     */
    public static void main(String[] args) {
        /*
         * Guice.createInjector() takes your Modules, and returns a new Injector instance.
         */
        Injector injector = Guice.createInjector(new AppConfigModule(), new AbstractModule() {

            @Override
            protected void configure() {
                bind(AmazonEC2Client.class).toProvider(AmazonEC2ClientProvider.class);
            }
        });

        try {
            if (Arrays.asList(args).contains(PARAM_DEMON_MODE)) {
                // Try to assign ElasticIP from tags to current instance if it's possible
                injector.getInstance(AmazonEC2Helper.class).assignElasticIPByTags();
                injector.getInstance(Daemon.class).start();
            } else {
                injector.getInstance(ConsoleApp.class).startApp();
                System.out.println("Press any key to exit");
                System.in.read();
            }
        } catch (Exception ex) {
            LOG.error("Application error", ex);
        }
    }
}
