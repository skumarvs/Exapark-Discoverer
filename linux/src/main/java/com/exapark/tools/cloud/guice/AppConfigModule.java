/**
 * 
 */
package com.exapark.tools.cloud.guice;

import java.io.IOException;
import java.io.InputStream;
import java.util.Properties;

import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;

import com.exapark.tools.cloud.Program;
import com.google.inject.AbstractModule;
import com.google.inject.name.Names;

/**
 * Application config guice module.
 * 
 * @author Yury Shevchenko
 */
public class AppConfigModule extends AbstractModule {

    /**
     * Logger.
     */
    private static final Log LOG = LogFactory.getLog(Program.class);

    /**
     * @see com.google.inject.AbstractModule#configure()
     */
    @Override
    protected void configure() {
        Properties properties = new Properties();
        InputStream is = this.getClass().getResourceAsStream("/exapark.config.properties");
        try {
            properties.load(is);
        } catch (IOException e) {
            LOG.debug("Unable to load exapark configuration properties file from classpath (exapark.config.properties).");
        }
        Names.bindProperties(binder(), properties);
    }
}
