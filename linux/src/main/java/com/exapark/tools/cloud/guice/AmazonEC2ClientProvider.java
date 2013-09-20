/**
 * 
 */
package com.exapark.tools.cloud.guice;

import com.amazonaws.auth.BasicAWSCredentials;
import com.amazonaws.services.ec2.AmazonEC2Client;
import com.google.inject.Inject;
import com.google.inject.Provider;
import com.google.inject.name.Named;

/**
 * @author Yury Shevchenko
 */
public class AmazonEC2ClientProvider implements Provider<AmazonEC2Client> {

    /**
     * URL for SOAN API Amazon.
     */
    private String awsServiceURL;

    /**
     * Public key to access Amazon.
     */
    private String awsAccessKey;

    /**
     * Secret key to access Amazon.
     */
    private String awsSecretKey;

    /**
     * Injection required parameters.
     * 
     * @param awsServiceURL
     *            URL for SOAN API Amazon
     * @param awsAccessKey
     *            Public key to access Amazon.
     * @param awsSecretKey
     *            Secret key to access Amazon.
     */
    @Inject
    public void injectParams(
            @Named("AwsServiceURL") String awsServiceURL, 
            @Named("AwsAccessKey") String awsAccessKey, 
            @Named("AwsSecretKey") String awsSecretKey) {
        this.awsServiceURL = awsServiceURL;
        this.awsAccessKey = awsAccessKey;
        this.awsSecretKey = awsSecretKey;
    }

    @Override
    public AmazonEC2Client get() {
        final BasicAWSCredentials credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
        // build connection
        final AmazonEC2Client ec2 = new AmazonEC2Client(credentials);
        // set configured URL
        ec2.setEndpoint(awsServiceURL);
        return ec2;
    }

}
