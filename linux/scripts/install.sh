#!/bin/bash

default_path="/opt/exapark"
default_interval="10"
default_filterbyTag="Group"
property_file="exapark.config.properties"
jar_file="Exapark Server Discoverer-1.2.jar"
stop_jar_file="Exapark\ Server\ Discoverer-1.2.jar"
public_key_param="AwsAccessKey"
secret_key_param="AwsSecretKey"
url_param="AwsServiceURL"
temp_dir="./tmp"
working_dir=`pwd`
startup_script="exaparkservice"
register_url="http://www.exapark.com/EDS/welcome_linux.html"
SERVICENAME="exaparkservice"

echo "Welcome to Exapark Server Discoverer Installer. This script will guide you throught te rest of installation process."

echo "Checking requirements:"

echo -n "Is java installed:"
if which java > /dev/null; then
	echo "yes"
else
	echo "not found."
	echo "This service requires java installed."
	exit
fi

echo -n "Is jar utility present:"
if which jar > /dev/null; then
	echo "yes"
else
	echo "not found."
	echo "This service requires jar utility (part of JDK) installed."
	exit
fi

echo -n "Writeable startup directory:"
if [ -w $working_dir ]; then
	echo "ok"
else
	echo "startup directory is not writable"
	echo "Run startup script from directoty you have write permission."
	exit
fi

read -p "Specify or accept default [$default_path] path for daemon binaries. Then press ENTER:" install_path

if ["$install_path" = ""]; then
	install_path="$default_path"
fi

if [ -d "$install_path" ]; then
	read -p "Choosen path already exists. Enter [y] and press ENTER to overwrite:" accept_overwrite
	if [ "$accept_overwrite" != "y" ]; then
		exit
	fi 
fi

read -p "Specify public access key for amazon account and press ENTER:" public_key

if [ -z $public_key ]; then
	echo "Public key is mandatory parameter. Service is useless without it. You can change your public key at any time. For this you have to update parameter [$public_key_param] in [$property_file] and put it in service binary: [$jar_file]"
fi

read -p "Specify secret access key for amazon account and press ENTER:" secret_key

if [ -z $secret_key ]; then
        echo "Secret key is mandatory parameter. Service is useless without it. You can change your secret key at any time. For this you have to update parameter [$secret_key_param] in [$property_file] and put it in service binary: [$jar_file]"
fi

read -p "Specify availability zone as one of [eu-west-1, us-west-1, us-west-2, ap-northeast-1, us-east-1, ap-southeast-1, sa-east-1] and ENTER:" zone

if [ -z $zone ]; then
        echo "Zone is mandatory parameter. Service is useless without it. You can change your zone at any time. For this you have to update parameter [$zone_param] in [$property_file] and put it in service binary: [$jar_file]"
else
	zone="http://ec2.$zone.amazonaws.com"
fi

echo "Installation in process:"

echo -n "Create temporal directory:"
if [ -d $temp_dir ]; then
rm -R $temp_dir
fi
mkdir $temp_dir
echo "done."

echo -n "Property file creation:"
#Create new property file
echo "$url_param=$zone" > $temp_dir/$property_file
echo "$public_key_param=$public_key" >> $temp_dir/$property_file
echo "$secret_key_param=$secret_key" >> $temp_dir/$property_file
echo "PollInterval=$default_interval" >> $temp_dir/$property_file
echo "FilterByTag=$default_filterbyTag" >> $temp_dir/$property_file
echo "HostsFile=/etc/hosts" >> $temp_dir/$property_file
echo "done."

echo -n "Startup script creation:"
echo "#!/bin/bash" >> $temp_dir/$startup_script
echo "#  /etc/rc.d/init.d/start_exapark.sh" >> $temp_dir/$startup_script
echo "# chkconfig: 345 25 75" >> $temp_dir/$startup_script
echo "# Source function library." >> $temp_dir/$startup_script
echo ". /etc/init.d/functions" >> $temp_dir/$startup_script
echo "# Source my configuration" >> $temp_dir/$startup_script
echo "if [ -f /etc/sysconfig/$SERVICENAME ]; then" >> $temp_dir/$startup_script
echo ". /etc/sysconfig/$SERVICENAME" >> $temp_dir/$startup_script
echo "fi" >> $temp_dir/$startup_script
echo "" >> $temp_dir/$startup_script

echo "start() {" >> $temp_dir/$startup_script
echo "echo -n 'Starting $SERVICENAME:' " >> $temp_dir/$startup_script
echo "daemon --pidfile=$SERVICENAME.pid java -jar '$install_path/$stop_jar_file' -d > '$install_path/logs/$SERVICENAME.log' 2>&1 &" >> $temp_dir/$startup_script
echo "r=\$?" >> $temp_dir/$startup_script
echo "if [ \$r -eq 0 ]; then" >> $temp_dir/$startup_script
echo "touch /var/lock/subsys/$SERVICENAME" >> $temp_dir/$startup_script
echo "fi" >> $temp_dir/$startup_script
echo "return \$r" >> $temp_dir/$startup_script
echo "}" >> $temp_dir/$startup_script

echo "" >> $temp_dir/$startup_script

echo "stop () {" >> $temp_dir/$startup_script
echo "echo -n 'Shutting down $SERVICENAME:'" >> $temp_dir/$startup_script
echo "pids=\$(ps -ef | grep \"$install_path/$stop_jar_file\" | awk '{print \$2}')" >> $temp_dir/$startup_script
echo "kill \$pids" >> $temp_dir/$startup_script
echo "rm -f /var/lock/subsys/$SERVICENAME" >> $temp_dir/$startup_script
echo "}" >> $temp_dir/$startup_script
echo "" >> $temp_dir/$startup_script

echo "case \$1 in" >> $temp_dir/$startup_script
echo "start"")" >> $temp_dir/$startup_script
echo "start" >> $temp_dir/$startup_script
echo ";;" >> $temp_dir/$startup_script
echo "stop"")" >> $temp_dir/$startup_script
echo "stop" >> $temp_dir/$startup_script
echo ";;" >> $temp_dir/$startup_script
echo "status"")" >> $temp_dir/$startup_script
echo "status $SERVICENAME" >> $temp_dir/$startup_script
echo ";;" >> $temp_dir/$startup_script
echo "restart"")" >> $temp_dir/$startup_script
echo "stop" >> $temp_dir/$startup_script
echo "start" >> $temp_dir/$startup_script
echo ";;" >> $temp_dir/$startup_script
echo "*"")" >> $temp_dir/$startup_script
echo "echo \"Usage: <servicename> start|stop|status|restart\"" >> $temp_dir/$startup_script
echo "exit 1" >> $temp_dir/$startup_script
echo ";;" >> $temp_dir/$startup_script
echo "esac" >> $temp_dir/$startup_script
echo "exit $?" >> $temp_dir/$startup_script


echo -n "Jar file update:"
cp "./$jar_file" $temp_dir
cd "$temp_dir"
jar uf "$jar_file" "$property_file"
cd "$working_dir"
echo "done."

echo -n "Copy binaries:"
if [ -d $install_path ]; then
sudo rm -Rf $install_path
fi
sudo mkdir $install_path
sudo cp "$temp_dir/$jar_file" $install_path/
sudo cp -R ./lib $install_path/
sudo cp $temp_dir/$startup_script $install_path/
sudo chmod 555 $install_path/$startup_script
sudo mkdir $install_path/logs
sudo chmod 777 $install_path/logs
sudo cp $install_path/$startup_script /etc/init.d
if [ -d "/etc/rc.d" ]; then
    sudo ln -s /etc/init.d/$startup_script /etc/rc.d/rc2.d/S99$startup_script
    sudo ln -s /etc/init.d/$startup_script /etc/rc.d/rc3.d/S99$startup_script
    sudo ln -s /etc/init.d/$startup_script /etc/rc.d/rc4.d/S99$startup_script
    sudo ln -s /etc/init.d/$startup_script /etc/rc.d/rc5.d/S99$startup_script
fi
if [ -d "/etc/rc2.d" ]; then
    sudo ln -s /etc/init.d/$startup_script /etc/rc2.d/S99$startup_script
    sudo ln -s /etc/init.d/$startup_script /etc/rc3.d/S99$startup_script
    sudo ln -s /etc/init.d/$startup_script /etc/rc4.d/S99$startup_script
    sudo ln -s /etc/init.d/$startup_script /etc/rc5.d/S99$startup_script
fi
echo "done."

sudo sed -i 's/Defaults.*requiretty/# Defaults requiretty/' /etc/sudoers

echo -n "Enable hosts file update:"
sudo chmod 644 /etc/hosts
echo "done."

echo -n "Cleaning up:"
rm -R $temp_dir
wget -q "$register_url"
echo "done."

#source $install_path/$startup_script

echo "Installation complete. Please review information in welcome_discoverer_l.html."
