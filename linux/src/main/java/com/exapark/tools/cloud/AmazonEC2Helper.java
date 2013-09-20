/**
 * 
 */
package com.exapark.tools.cloud;

import java.net.InetAddress;
import java.net.NetworkInterface;
import java.net.SocketException;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.List;

import org.apache.commons.lang.StringUtils;
import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;

import com.amazonaws.services.ec2.AmazonEC2Client;
import com.amazonaws.services.ec2.model.Address;
import com.amazonaws.services.ec2.model.AssociateAddressRequest;
import com.amazonaws.services.ec2.model.CreateTagsRequest;
import com.amazonaws.services.ec2.model.DeleteTagsRequest;
import com.amazonaws.services.ec2.model.DescribeAddressesResult;
import com.amazonaws.services.ec2.model.DescribeInstancesRequest;
import com.amazonaws.services.ec2.model.DescribeInstancesResult;
import com.amazonaws.services.ec2.model.Filter;
import com.amazonaws.services.ec2.model.Instance;
import com.amazonaws.services.ec2.model.Reservation;
import com.amazonaws.services.ec2.model.Tag;
import com.google.inject.Inject;
import com.google.inject.name.Named;

/**
 * @author Yury Shevchenko
 */
public class AmazonEC2Helper {

    /**
     * Bit mask of running instance
     */
    public static final byte RUNNING_STATE = 16;

    /**
     * tag name for instance name
     */
    public static final String TAG_INSTANCE_NAME = "Name";
    
    
    /**
     * tag name for EIP.
     */
    public static final String TAG_ELASTIC_IP = "ElasticIP";

    /**
     * Amazon client.
     */
    @Inject
    private AmazonEC2Client ec2;
    
    @Inject
    private @Named("FilterByTag")
    String filterByTag;

    /**
     * Log.
     */
    private final Log log = LogFactory.getLog(AmazonEC2Helper.class);

    /**
     * Get running instances from Amazon for configured region.
     * 
     * @return List of running instances
     */
    public List<Instance> getRunningInstances() {
        // list running instances
        final List<Instance> runningInstances = new ArrayList<Instance>();
        final DescribeInstancesResult serviceResult;
        if(!filterByTag.isEmpty()) {
        //Get the group value from the current running instance
        Instance currentInstance = getCurrentInstance();
        String currentInstanceGroupValue=getCurrentInstanceGroupValue(currentInstance);
        //log.info("currentInstancegroupvalue"+currentInstanceGroupValue);
        // ask service for descriptions with filter by group
        	serviceResult = ec2.describeInstances(new DescribeInstancesRequest().withFilters(new Filter("tag:"+filterByTag).withValues(currentInstanceGroupValue)));
        } else {
        	serviceResult = ec2.describeInstances();
        }
        // with all reservations
        // reservation is a group of instances launched with the same command
        final List<Reservation> reservations = serviceResult.getReservations();
        for (final Reservation reservation : reservations) {
            // these are not onle really running ones...
            final List<Instance> instances = reservation.getInstances();
            for (final Instance instance : instances) {
                // deal only with really running instances
                if (instance.getState().getCode().byteValue() == RUNNING_STATE) {
                    runningInstances.add(instance);
                }
            }
        }
        return runningInstances;
    }

    /**
     * Gets instance tag <code>name</code> or instance id if no tag found.
     * 
     * @param instance
     *            Amazon instance
     * @return Instance name
     */
    public String getInstanceName(Instance instance) {
        final List<Tag> tagList = instance.getTags();
        for (Tag tag : tagList) {
            if (tag.getKey().equalsIgnoreCase(TAG_INSTANCE_NAME)) return tag.getValue();
        }

        return instance.getInstanceId();
    }

    /**
     * Gets instance tag <code>ElasticIP</code> or NULL id if no tag found.
     * 
     * @param instance
     *            Amazon instance
     * @return ElasticIP or NULL
     */
    public String getElasticIPTagValue(Instance instance) {
        final List<Tag> tagList = instance.getTags();
        for (Tag tag : tagList) {
            if (tag.getKey().equalsIgnoreCase(TAG_ELASTIC_IP)) return tag.getValue();
        }

        return null;
    }

    /**
     * Associates ElasticIP to Instance.
     * 
     * @param instanceId
     *            InstanceId
     * @param publicIpAddress
     *            Elastic IP Address
     */
    public void setElasticIPToInstance(String instanceId, String publicIpAddress) {
        if (StringUtils.isNotBlank(instanceId) && StringUtils.isNotBlank(publicIpAddress)) {
            final AssociateAddressRequest request = new AssociateAddressRequest(
                    instanceId,
                    publicIpAddress);
            ec2.associateAddress(request);
        }
    }

    /**
     * @return Current instance or NULL if instance not found
     */
    public Instance getCurrentInstance() {
        final DescribeInstancesResult serviceResult = ec2.describeInstances();
        final List<Reservation> reservations = serviceResult.getReservations();
        for (final Reservation reservation : reservations) {
            // these are not onle really running ones...
            final List<Instance> instances = reservation.getInstances();
            for (final Instance instance : instances) {
                // deal only with really running instances
                if (instance.getState().getCode().byteValue() == RUNNING_STATE
                        && checkIPAddress(instance.getPrivateIpAddress())) return instance;
            }
        }
        return null;
    }

    /**
     * Checks local IP.
     * 
     * @param ipAddr
     *            IP Address
     * @return TRUE if specified IP is equals to local one.
     */
    private boolean checkIPAddress(String ipAddr) {
        Enumeration<NetworkInterface> netInterfaces = null;
        try {
            netInterfaces = NetworkInterface.getNetworkInterfaces();
        } catch (final SocketException e) {
            log.warn("Unnable to retrieve local network interfaces", e);
        }
        if (netInterfaces != null && StringUtils.isNotBlank(ipAddr)) {
            for (; netInterfaces.hasMoreElements();) {
                final NetworkInterface netInterface = netInterfaces.nextElement();
                final Enumeration<InetAddress> addrs = netInterface.getInetAddresses();
                for (; addrs.hasMoreElements();) {
                    final InetAddress addr = addrs.nextElement();
                    if (ipAddr.equals(addr.getHostAddress())) return true;
                }
            }
        }

        return false;
    }

    /**
     * @param instance
     *            Amazon instance
     * @return TRUE if ElasticIP assigned to the current instance.
     */
    public boolean isElasticIPAssigned(Instance instance) {
        if (null != instance) {
            final DescribeAddressesResult describeAddressesResult = ec2.describeAddresses();
            final List<Address> addrs = describeAddressesResult.getAddresses();
            for (final Address address : addrs) {
                if (instance.getInstanceId().equalsIgnoreCase(address.getInstanceId())) return true;
            }
        }
        return false;
    }

    /**
     * @param elasticIP
     *            IP to check
     * @return TRUE if ElasticIP is free to assign.
     */
    public boolean isElasticIPFree(String elasticIP) {
        final DescribeAddressesResult describeAddressesResult = ec2.describeAddresses();
        final List<Address> addrs = describeAddressesResult.getAddresses();
        for (final Address address : addrs) {
            if (address.getPublicIp().equals(elasticIP)
                    && StringUtils.isBlank(address.getInstanceId())) return true;
        }

        return false;
    }

    /**
     * Retrieves current Amazon instance, check tags for ElasticIP and try to assign if it is
     * possible.
     */
    protected void assignElasticIPByTags() {
        final Instance instance = getCurrentInstance();
        if (null != instance) {
            final List<Tag> tags = instance.getTags();
            for (final Tag tag : tags) {
                if (tag.getKey().equals(TAG_ELASTIC_IP) && isElasticIPFree(tag.getValue())) {
                    setElasticIPToInstance(instance.getInstanceId(), tag.getValue());
                }
            }
        }
    }

    /**
     * Adds tag to specified instance.
     * 
     * @param instanceId
     *            InstanceId
     * @param tag
     *            Tag (key-value pair)
     */
    public void addTagToInstance(String instanceId, Tag tag) {
        CreateTagsRequest createTagsRequest = new CreateTagsRequest();
        createTagsRequest.withResources(instanceId);
        createTagsRequest.withTags(tag);
        ec2.createTags(createTagsRequest);
    }
    
    /**
     * Deletes tag from specified instance.
     * 
     * @param instanceId
     *            InstanceId
     * @param tag
     *            Tag (key-value pair)
     */
    public void removeTagFromInstance(String instanceId, Tag tag) {
        DeleteTagsRequest deleteTagsRequest = new DeleteTagsRequest();
        deleteTagsRequest.withResources(instanceId);
        deleteTagsRequest.withTags(tag);
        ec2.deleteTags(deleteTagsRequest);
    }

    /**
     * Gets instance tag <code>Group</code> or return empty if no tag found.
     * 
     * @param instance
     *            Amazon instance
     * @return Instance name
     */
    public String getCurrentInstanceGroupValue(Instance instance) {
        final List<Tag> tagList = instance.getTags();
        for (Tag tag : tagList) {
        //	log.info(tag.getKey()+tag.getValue());
            if (tag.getKey().equalsIgnoreCase(filterByTag)) return tag.getValue();
        }
        return null;
    }


}
