#########################################################################
# Copyright 2011 Cloud Sidekick
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#    http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#########################################################################

proc proc_name {} {
	return [lindex [info level -1]]
}
proc initialize_logfile {} {
	set ::LOGFILE_NAME "$::LOGFILES/[string tolower $::PROCESS_NAME].log"
}
proc output {args} {
	set output_string [lindex $args 0]
	set ::LOGFILE [open $::LOGFILE_NAME {WRONLY CREAT APPEND}]
	fconfigure $::LOGFILE -encoding utf-8
	puts $::LOGFILE "\n-> [clock format [clock seconds] -format "%Y-%m-%d %H:%M:%S"] $output_string"
	close $::LOGFILE
}
proc initialize {} {
	###
	### Set some globals stored in the ini file
	###

	set ::MY_PID [pid]
	set ::DELAY 3
	set ::LOOP 10
	set ::MODE on
	set ::MASTER 1

	set ::HOST_DOMAIN "$::tcl_platform(user)@[info hostname]"
}
proc connect_db {} {

	output "Connecting to $::CONNECT_SERVER $::CONNECT_DB $::CONNECT_PORT, user $::CONNECT_USER"
	if {[catch {set ::CONN [::mysql::connect -user $::CONNECT_USER -password $::CONNECT_PASSWORD -host $::CONNECT_SERVER -db $::CONNECT_DB -port $::CONNECT_PORT -multiresult 1 -multistatement 1]} errMsg]} {
		output "Could not connect to the database. Error message -> $errMsg"
		output "Exiting..."
		exit
	}
	output "Connected"

	# Get the CE node number
	set sql "select id from tv_application_registry where app_name = '$::PROCESS_NAME' and app_instance = '$::HOST_DOMAIN'"
	::mysql::sel $::CONN $sql
	if {[string length [set row [::mysql::fetch $::CONN]]] <= 0} {
		output "$::PROCESS_NAME ->$::HOST_DOMAIN<- has not been registered, registering..."
		register_app
		set sql "select id from tv_application_registry where app_name = '$::PROCESS_NAME' and app_instance = '$::HOST_DOMAIN'"
		::mysql::sel $::CONN $sql
		set row [::mysql::fetch $::CONN]
		set ::INSTANCE_ID [lindex $row 0]
	} else {
		set ::INSTANCE_ID [lindex $row 0]
		set sql "update application_registry set hostname = '[info hostname]', userid = '$::tcl_platform(user)', pid = [pid], platform = '$::tcl_platform(platform)' where id = $::INSTANCE_ID"
		::mysql::exec $::CONN $sql
	}
}
proc check_running {} {
	set sql "select master from tv_application_registry where id = $::INSTANCE_ID"	
	::mysql::sel $::CONN $sql
	set ::MASTER [lindex [::mysql::fetch $::CONN] 0]
}
proc update_heartbeat {} {
	set sql "update application_registry set heartbeat = now() where id = $::INSTANCE_ID" 
	::mysql::exec $::CONN $sql
}
proc startup {} {
	output "*****************************************************************"
	output "Starting $::PROCESS_NAME, PID = [pid].... "
	package require mysqltcl

	output "Database library loaded, starting initialization..."
	initialize
	output "Initialization complete, connecting to the database..."
	connect_db
	output "Database connection complete."
	#check_already_running
	initialize_process
	get_settings
	get_email_settings
	output "Mode is $::MODE"
	#set sql "insert into message (date_time_entered,process_type,status,msg_to,msg_from,msg_subject,msg_body) values (now(),1,0,'$::ADMIN_EMAIL','$::PROCESS_NAME','$::PROCESS_NAME $::HOST_DOMAIN started',concat('$::PROCESS_NAME service on $::HOST_DOMAIN was started at ' , now()))"
	#::mysql::exec $::CONN $sql
	check_running	
	while {"$::RUNNING" == "on"} {
		update_heartbeat
		if {"$::MODE" == "on" && "$::MASTER" == "1"} {
			main_process
		}
		after [expr $::LOOP * 1000] {set ::x 1}
		vwait ::x
		get_settings
		check_running	
	}
	::mysql::close $::CONN
}
proc get_email_settings {} {
	
        set sql "select admin_email from messenger_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [lindex [::mysql::fetch $::CONN] 0]
	set ::ADMIN_EMAIL [lindex $row 0]
	
}
proc register_app {} {
	output "Registering application..."

	set sql "insert into application_registry (app_name, app_instance, master, logfile_name, hostname, userid, pid, platform) values ('$::PROCESS_NAME', '$::HOST_DOMAIN',1, '[string tolower $::PROCESS_NAME].log', '[info hostname]', '$::tcl_platform(user)',[pid],'$::tcl_platform(platform)')" 
	::mysql::exec $::CONN $sql
	output "Application registered."
}
proc check_already_running {} {

	### this doesn't work because "exec" blows up if a command returns nothing
	### will need to change it somehow.  Try something like this:
	### http://coding.derkeiler.com/Archive/Tcl/comp.lang.tcl/2005-05/msg00402.html
	#set found_pid [exec ps U$::tcl_platform(user) -opid,command | grep poller.tcl | grep -v grep | grep -v PID]
	#if {[info exists found_pid]} {
	#	output "ERROR:  This CE node is already being polled for CE tasks. My process id = [pid], other poller process id = [$pid]."
	#	exit
	#}
}

proc read_config {} {
	set ::HOME $::env(CATO_HOME)
	lappend ::auto_path $::HOME/services/lib
	package require base64
	package require blowfish
	package require catocrypt
        set file "[file join $::HOME conf cato.conf]"
        if {![file exists $file]} {
                return -code error -level 1 "Error: $file could not be opened for reading"
        }
        set fp [open $file RDONLY]
        set file_data [read $fp]
        close $fp
        set data [split $file_data "\n"]
	set exit_flag 0
        foreach x $data {
                set x [string trim $x]
                if {"[string index $x 0]" != "#" && [string length $x] > 0} {
                        set key [string tolower [lindex $x 0]]
                        set value [lindex $x 1]
                        switch $key {
                                database {
                                        set ::CONNECT_DB $value
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					}
                                }
                                server {
                                        set ::CONNECT_SERVER $value
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					}
                                }
                                user {
                                        set ::CONNECT_USER $value
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					}
                                }
                                password {
                                        set password $value
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					}
                                }
                                port {
                                        set ::CONNECT_PORT $value
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					}
                                }
                                key {
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					} else {
						set ::SITE_KEY [decrypt_string $value {}]
					}
                                }
                                ses_access_key {
                                        set ::SES_ACCESS_KEY $value
                                }
                                ses_secret_key {
                                        set ::SES_SECRET_KEY $value
                                }
                                logfiles {
                                        set ::LOGFILES $value
					if {"$value" == ""} {
						puts "The parameter $key is required, no value found"
						set exit_flag 1
					}
                                }
                                tmpdir {
                                        set ::TMP $value
                                }
                        }
                }
        }
	if {$exit_flag == 1} {
                return -code error -level 1 "Error: Required conf file parameters are missing"
	}
        if {[info exists password]} {
                set ::CONNECT_PASSWORD [decrypt_string $password $::SITE_KEY]
        }
        return
}

proc main {} {

	initialize_logfile
	output "####################################### Starting up $::PROCESS_NAME #######################################"
	output "Home is $::HOME"

	set ::NAME "$::tcl_platform(user)@[info hostname]"
	output "Instance name is is $::NAME"
	set ::RUNNING on
	startup
}
