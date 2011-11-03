#!/usr/bin/env tclsh

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

set ::CATO_HOME [file dirname [file dirname [file dirname [file normalize $argv0]]]]
source $::CATO_HOME/services/bin/common.tcl

read_config

proc get_ecosystem_name {} {
	set proc_name get_ecosystem_name
	set sql "select ecosystem_name from ecosystem where ecosystem_id = '$::ECOSYSTEM_ID'"
	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	set ::ECOSYSTEM_NAME [lindex $row 0]
	return $::ECOSYSTEM_NAME
}
proc store_private_key {command} {
	set proc_name store_private_key

	get_xml_root $command
	set key_name [replace_variables_all [$::ROOT selectNodes string(name)]]
	set private_key [replace_variables_all [$::ROOT selectNodes string(private_key)]]
	del_xml_root
	if {"$key_name" == ""} {
		error_out "Keyname is required" 9999
	} 
	if {"$private_key" == ""} {
		error_out "Private Key value is required" 9999
	} 
        set sql "insert into cloud_account_keypair (keypair_id, account_id, keypair_name, private_key) values (uuid(),'$::CLOUD_ACCOUNT','$key_name','[encrypt_string $private_key $::SITE_KEY]')"
	$::db_exec $::CONN $sql
}
proc get_ecosystem_objects {command} {
	set proc_name get_ecosystem_tasks

	get_xml_root $command
	set object_type [$::ROOT selectNodes string(object_type)]
	set result_name [replace_variables_all [$::ROOT selectNodes string(result_name)]]
	del_xml_root
	if {"$object_type" == ""} {
		error_out "Get Ecosystem Objects requires an object type" 9999
	} 
	if {"$result_name" == ""} {
		error_out "Get Ecosystem Objects requires a variable name for the result" 9999
	} 
	array unset ::runtime_arr $result_name,*
        set sql "select do.ecosystem_object_id from ecosystem_object do where do.ecosystem_id = '$::ECOSYSTEM_ID' and do.ecosystem_object_type = '$object_type'"
        set  objects [$::db_query $::CONN $sql -list]
	set ii 0
        foreach object $objects {
		incr ii
		set_variable $result_name,$ii $object
        }
}

proc aws_get_result_var {result path} {
	set proc_name aws_get_result_var
	set xmldoc [dom parse -simple $result]
	set root [$xmldoc documentElement]
	set value [$root selectNodes string($path)]
	return $value	
}
proc aws_get_results {result path var_name} {
	set proc_name aws_get_results
	set xmldoc [dom parse -simple $result]
	set root [$xmldoc documentElement]
	set xml_no_ns [[$root removeAttribute xmlns] asXML]
	$root delete
	$xmldoc delete
	set xmldoc [dom parse -simple $xml_no_ns]
	unset xml_no_ns
	set root [$xmldoc documentElement]
	set items [$root selectNodes //$path]
	set ii 0
	foreach item $items {
		incr ii
		output "setting $var_name,$ii to [$item asXML]" 4
		set_variable "$var_name,$ii" [$item asXML]
	}
	$root delete
	$xmldoc delete
}
proc get_meta {name} {
        set proc_name get_my_ip
        package require http
        set tok [::http::geturl http://169.254.169.254/latest/meta-data/$name]
        set ip [::http::data $tok]
        ::http::cleanup $tok
        return $ip
}
proc register_security_group {apply_to_group port region} {
        set proc_name register_security_group
        package require tclcloud
        #if {"$::CLOUD_LOGIN_ID" == "" || "$::CLOUD_LOGIN_PASS" == ""} {
        #       error_out "Cloud account id or password is required" 9999
        #}
        #if {![info exists ::CSK_ACCOUNT]} {
        #       set sql "select value from customer_info where keyname = 'csk_account_num'"
        #       $::db_query $::CONN $sql
        #       set ::CSK_ACCOUNT [lindex [$::db_fetch $::CONN] 0]
        #}
        #if {![info exists ::CSK_SECURITY_GROUP]} {
        #       set sql "select value from customer_info where keyname = 'csk_security_group'"
        #       $::db_query $::CONN $sql
        #       set ::CSK_SECURITY_GROUP [lindex [$::db_fetch $::CONN] 0]
        #}

        set x [::tclcloud::connection new $::CLOUD_LOGIN_ID $::CLOUD_LOGIN_PASS]
	if {"$region" > ""} {
		set ip [get_meta public-ipv4]
	} else {
		set ip [get_meta local-ipv4]
	}
        #set params "GroupId $apply_to_group IpPermissions.1.Groups.1.UserId $::CSK_ACCOUNT IpPermissions.1.Groups.1.GroupId $::CSK_SECURITY_GROUP IpPermissions.1.IpProtocol tcp IpPermissions.1.FromPort $port IpPermissions.1.ToPort $port"
        set params "GroupId $apply_to_group IpPermissions.1.IpRanges.1.CidrIp $ip/32 IpPermissions.1.IpProtocol tcp IpPermissions.1.FromPort $port IpPermissions.1.ToPort $port"
        set cmd "$x  call_aws ec2 \"$region\" AuthorizeSecurityGroupIngress"
        lappend cmd $params
        catch {set  result [eval $cmd]} result
        output $result
}

proc gather_account_info {account_id} {
	set proc_name gather_account_info
	set sql "select ca.account_name, ca.provider, ca.login_id, ca.login_password from cloud_account ca where ca.account_id = '$account_id'"
	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	set ::CLOUD_NAME [lindex $row 0]
	set ::CLOUD_TYPE [lindex $row 1]
	set ::CLOUD_LOGIN_ID [lindex $row 2]
	set ::CLOUD_LOGIN_PASS [decrypt_string [lindex $row 3] $::SITE_KEY]
	if {"$::CLOUD_TYPE" == "Eucalyptus"} {
		set sql "select cloud_name, api_url from clouds where provider = '$::CLOUD_TYPE'"
		$::db_query $::CONN $sql
		while {[string length [set row [$::db_fetch $::CONN]]] > 0} {
			set ::CLOUD_ENDPOINTS($::CLOUD_TYPE,[lindex $row 0]) "[lindex $row 1]/services/Eucalyptus"
		}
	}
}
proc aws_Generic {product operation path command} {
	set proc_name aws_Generic
        get_xml_root $command
        set node [$::ROOT selectNodes /function]
        set params ""
        set instance_role ""
        set nodeName ""
        foreach kid [$node childNodes] {
                set params [aws_drill_in $kid $nodeName $params]
        }
	array unset ::NODE_CTR
		
	set node [$::ROOT selectNodes //result_name]
	set instance_role [$::ROOT selectNodes string(//instance_role)]
	set aws_region [$::ROOT selectNodes string(//aws_region)]
	set version [$::ROOT selectNodes //api_version]
	set var_name [$node selectNodes string(.)]
	del_xml_root
	package require tclcloud
	if {"$::CLOUD_LOGIN_ID" == "" || "$::CLOUD_LOGIN_PASS" == ""} {
		error_out "Cloud account id or password is required" 9999
	}
	if {"$::CLOUD_TYPE" == "Eucalyptus"} {
		if {"$aws_region" > ""} {
			if {[array names ::CLOUD_ENDPOINTS "Eucalyptus,$aws_region"] == ""} {
				error_out "Cloud region $aws_region does not exist. Either create a Eucalyptus Cloud definition or enter an existing cloud name in the region field" 9999
			}
			set endpoint $::CLOUD_ENDPOINTS(Eucalyptus,$aws_region)
			if {"$endpoint" == ""} {
				error_out "AWS error: Region $aws_region for Eucalyptus cloud not defined. Region name must match a valid cloud name." 9999
			}
		} else {
			set endpoint [lindex [array get ::CLOUD_ENDPOINTS Eucalyptus,*] 1]
			if {"$endpoint" == ""} {
				error_out "AWS error: A default cloud for Eucalyptus not defined. Create a valid cloud with endpoint url for Eucalyptus." 9999
			}
		}
	} else {
		set endpoint ""
	}
	output "::tclcloud::connection new $::CLOUD_LOGIN_ID $::CLOUD_LOGIN_PASS {$aws_region $endpoint}"
	lappend region_endpoint $aws_region $endpoint
        set x [::tclcloud::connection new $::CLOUD_LOGIN_ID $::CLOUD_LOGIN_PASS $region_endpoint]
        set cmd "$x  call_aws $product \"$aws_region\" $operation"

	lappend cmd $params
	lappend cmd $version
	if {$::DEBUG_LEVEL >= 3} {
		output $cmd
	}
	if [catch {set  result [eval $cmd]} result] {
		error_out "AWS error: $operation $params\012\012$result" 9999
	}
	if {$::DEBUG_LEVEL > 2} {
		insert_audit $::STEP_ID  "" "$operation $params\012OK\012$result" ""
	} else {
		insert_audit $::STEP_ID  "" "$operation $params\012OK" ""
	}

	output "result is $result" 4
	output "varname is $var_name" 4
	
	if {"$::ECOSYSTEM_ID" > ""} {
		output "got a ecosystem, operation is $operation"
		switch $operation {
			RunJobFlow {
				register_ecosystem_object $result $::ECOSYSTEM_ID aws_emr_jobflow {//JobFlowId} {} {} $aws_region
			}
			RunInstances {
				register_ecosystem_object $result $::ECOSYSTEM_ID aws_ec2_instance {//instancesSet/item/instanceId} $instance_role $x $aws_region
			}
			CreateAutoScalingGroup {
				register_ecosystem_object $command $::ECOSYSTEM_ID aws_as_group {//AutoScalingGroupName} {} {} $aws_region
			}
			CreateDomain {
				register_ecosystem_object $command $::ECOSYSTEM_ID aws_sdb_domain {//DomainName} {} {} $aws_region
			}
			CreateKeyPair {
				register_ecosystem_object $result $::ECOSYSTEM_ID aws_ec2_keypair {//keyName} {} {} $aws_region
			}
			CreateSecurityGroup {
				register_ecosystem_object $command $::ECOSYSTEM_ID aws_ec2_security_group {//GroupName} {} {} $aws_region
			}
			CreateLoadBalancer {
				register_ecosystem_object $command $::ECOSYSTEM_ID aws_elb_balancer {//LoadBalancerName} {} {} $aws_region
			}
			AllocateAddress {
				register_ecosystem_object $result $::ECOSYSTEM_ID aws_ec2_address {//publicIp} {} {} $aws_region
			}
			CreateDBInstance {
				register_ecosystem_object $result $::ECOSYSTEM_ID aws_rds_instance {//DBInstance/DBInstanceIdentifier} {} {} $aws_region
			}
		}
	} 
	set path $operation
	append path Response

	aws_get_results $result $path $var_name
	$x destroy
}

proc register_ecosystem_object {result ecosystem_id object_type path role api_conn $region} {
	set proc_name register_ecosystem_object
        set xmldoc [dom parse -simple $result]
        set root [$xmldoc documentElement]
	if {[$root hasAttribute xmlns]} {
		set xml_no_ns [[$root removeAttribute xmlns] asXML]
		$root delete
		$xmldoc delete
		set xmldoc [dom parse -simple $xml_no_ns]
		unset xml_no_ns
		set root [$xmldoc documentElement]
	}

	set instances [$root selectNodes $path]
	output "instances is $instances"
	foreach instance $instances {
		set sql "insert into ecosystem_object (ecosystem_id, ecosystem_object_id, ecosystem_object_type, added_dt) values ('$ecosystem_id','[$instance asText]','$object_type',$::getdate)"
		$::db_exec $::CONN $sql
		if {"$role" > ""} {
			set the_arg ""
			lappend the_arg ResourceId.1 [$instance asText] Tag.1.Key cato.role Tag.1.Value $role
			for {set counter 0} {$counter < 4} {incr counter} {
				if {[catch {$api_conn call_aws ec2 \"$region\" CreateTags $the_arg} errMsg]} {
					if {[string match "*does not exist*" $errMsg]} {
						output "Waiting five seconds to reattempt tagging"
						sleep 5
					} else {
						break
					}
				} else {
					break
				}
			}
		}
	}
        $root delete
        $xmldoc delete
}
proc aws_drill_in {node name params} {
	set proc_name aws_drill_in
        if {[string match -nocase {*.[mnx]} $name]} {
                incr ::NODE_CTR($name)
        }
        if {[$node hasChildNodes]} {
                foreach kid [$node childNodes] {
                        if {[[$node parentNode] hasAttribute is_array]} {
                                set nodeName $name
                                regsub \\.n$|\\.m$|\\.N$|\\.X$ $nodeName \.$::NODE_CTR($name) nodeName
                        } else {
                                if {"$name" > ""} {
                                        set nodeName  "$name.[$node nodeName]"
                                } else {
                                        set nodeName  "[$node nodeName]"
                                }
                        }
                        set params [aws_drill_in $kid $nodeName $params]
                }
        } elseif {"$name" != "result_name" && "$name" != "instance_role" && "$name" != "aws_region"} {
                set nodeValue [$node selectNodes string(.)]
		if {"$name" == "UserData"} {
			package require base64
			#output "encoding user data"
			set nodeValue [::base64::encode $nodeValue]
		}
                if {"$nodeValue" > ""} {
                        regsub -all "&amp;" $nodeValue {\&} nodeValue
                        regsub -all "&gt;" $nodeValue ">" nodeValue
                        regsub -all "&lt;" $nodeValue "<" nodeValue
                        lappend params $name $nodeValue
                }
        }
        return $params
}

proc get_aws_private_key {keyname} {
        set proc_name get_aws_private_key
	set sql "select private_key, passphrase from cloud_account_keypair cak where cak.account_id = '$::CLOUD_ACCOUNT' and cak.keypair_name = '$keyname'"
	$::db_query $::CONN $sql
	return [$::db_fetch $::CONN]
}
proc gather_aws_system_info {instance_id user_id region} {
        set proc_name gather_aws_system_info
        package require tclcloud
        set x [::tclcloud::connection new $::CLOUD_LOGIN_ID $::CLOUD_LOGIN_PASS]
        set cmd "$x call_aws ec2 \"$region\" DescribeInstances "
        set params "InstanceId $instance_id"
        lappend cmd $params
        lappend cmd {}
        set result [eval $cmd]
	output $result
        set xmldoc [dom parse -simple $result]
        set root [$xmldoc documentElement]
        set xml_no_ns [[$root removeAttribute xmlns] asXML]
        $root delete
        $xmldoc delete
        set xmldoc [dom parse -simple $xml_no_ns]
        unset xml_no_ns
        set root [$xmldoc documentElement]
        #if {!$root} {
        #       return ""
        #}
        set state [[[$root selectNodes {//instancesSet/item/instanceState/name}] childNode] data]
	if {"$state" != "running"} {
		return $state
	}
	
        set keyname [[[$root selectNodes {//instancesSet/item/keyName}] childNode] data]
        set security_group_id [[[$root selectNodes {//instancesSet/item/groupSet/item/groupId[1]}] childNode] data]
        set platform_node [$root selectNodes {//instancesSet/item/platform}]
        if {"$platform_node"> ""} {
                set platform [[[$root selectNodes {//instancesSet/item/platform}]  childNode] data]
        } else {
                set platform "linux"
        }
        set address [[[$root selectNodes {//instancesSet/item/ipAddress}]  childNode] data]
        output "$instance_id state = $state, keyname = $keyname, platform = $platform, address = $address" 3
        $root delete
        $xmldoc delete
        $x destroy
	set pk_pass [get_aws_private_key $keyname]

	set ::system_arr($instance_id,name) $instance_id
	set ::system_arr($instance_id,address) $address
	set ::system_arr($instance_id,port) ""
	set ::system_arr($instance_id,db_name) ""
	set ::system_arr($instance_id,conn_type) ""
	set ::system_arr($instance_id,userid) $user_id
	if {"[lindex $pk_pass 1]" > ""} {
		set ::system_arr($instance_id,password) [decrypt_string [lindex $pk_pass 1] $::SITE_KEY]
	} else {
		set ::system_arr($instance_id,password) ""
	}
	set ::system_arr($instance_id,priv_password) ""
	set ::system_arr($instance_id,domain) ""
	set ::system_arr($instance_id,conn_string) ""
        set ::system_arr($instance_id,private_key_name) $keyname
        if {"$pk_pass" > ""} {
                set ::system_arr($instance_id,private_key) [decrypt_string [lindex $pk_pass 0] $::SITE_KEY]
        } else {
                set ::system_arr($instance_id,private_key) ""
        }
	set ::system_arr($instance_id,security_group) $security_group_id
	unset pk_pass
	return $state
}

### END OF SITE SPECIFIC CODE

proc sql_cast {column} {
	if {"$::tcl_platform(platform)" == "windows"} {
		return "cast($column as varchar(max))"
	} else {
		return "$column"
	}
}
proc insert_audit {step_id command log connection} {
    set proc_name insert_audit

    global TASK_INSTANCE
    global AUDIT_TRAIL_ON
    output "$log" 1
    if {$AUDIT_TRAIL_ON > 0} {
	foreach sensitive $::SENSITIVE {
		set log [string map "$sensitive **********" $log]
		set command [string map "$sensitive **********" $command]
		#regsub -all ($sensitive) $log {*********} log
		#regsub -all ($sensitive) $command {*********} command
	}
        regsub -all "(')" $log "''" log
        regsub -all "(')" $command "''" command

        if {"$step_id" != ""} {
        set sql "insert into task_instance_log (task_instance, step_id, entered_dt, connection_name, log, command_text) values ($::TASK_INSTANCE,'$step_id', $::getdate,'$connection','$log','$command')"
        } else {
                set sql "insert into task_instance_log (task_instance, step_id, entered_dt, connection_name, log, command_text) values ($::TASK_INSTANCE,NULL, $::getdate,'$connection','$log','$command')"
        }

        if { [catch {$::db_exec $::CONN $sql} return_code ]} {
			error_out "ERROR: Unable to insert log entry. Return Code: {$return_code}" 9999
        }
    }
    if {$AUDIT_TRAIL_ON == 1} {
        set AUDIT_TRAIL_ON 0
    }
}

##################################################
#       end of insert_audit
##################################################

##################################################
#       procedure: output
#
#       This proc will send puts statements
#       to the appropriate logfile
#
##################################################

proc output {args} {
    set output_string [lindex $args 0]
    set debug_level [lindex $args 1]

    if {[string length $debug_level] == 0} {
        set debug_level 0
    }

    upvar proc_name proc_name

    if {$::DEBUG_LEVEL >= $debug_level} {
	foreach sensitive $::SENSITIVE {
		set output_string [string map "$sensitive **********" $output_string]
	}
        puts "\n[clock format [clock seconds] -format "%Y-%m-%d %H:%M:%S"] ($proc_name):: $output_string"
	flush stdout
    }
}

##################################################
#       end of output
##################################################

proc email_attach_file {filename file_data msg_id} {
	set proc_name email_attach_file

	set fileshort [file tail $filename]
	regsub -all "(')" $file_data "''" file_data
	#output [string range $file_data 0 6]
	if {"[string range $file_data 0 6]" == "-base64"} {
		set file_type base64
		set file_data [string range $file_data 7 end]
	} else {
		set file_type text
	}
	tdbc_query $::CONN "insert into message_data_file (msg_id, file_name, file_type, file_data) values ($msg_id, '$fileshort', '$file_type', convert(varbinary(max),'$file_data'))"

	#if {[file exists $filename]} {
	#	set fileshort [file tail $filename]
	#	set fp [open $filename r]
	#	set file_data [read $fp]	
	#	close $fp
	#	regsub -all "(')" $file_data "''" file_data
	#	tdbc_query $::CONN "insert into message_data_file (msg_id, file_name, file_type, file_data) values ($msg_id, '$fileshort', 'local', convert(varbinary(max),'$file_data'))"
		
	#} else {
	#	output "The file $filename does not exist"
	#}


}
proc send_email_2 {command} {
	set proc_name send_email

	regsub -all "&" $command "&amp;" command
	set xmldoc [dom parse $command]
	set root [$xmldoc documentElement]
	set to [$root selectNodes string(to)]
	set subject [$root selectNodes string(subject)]
	set body [$root selectNodes string(body)]
	set attachment [$root selectNodes string(attachment)]
	set attachment_data [$root selectNodes string(attachment_data)]
	$root delete
	$xmldoc delete
	#output "$to, $subject, $body"

	regsub -all "(')" $subject "''" subject
	regsub -all "(')" $body "''" body
	regsub -all "&gt;" $body ">" body
	regsub -all "&lt;" $body "<" body
	regsub -all "&gt;" $attachment_data ">" attachment_data
	regsub -all "&lt;" $attachment_data "<" attachment_data
	insert_audit $::STEP_ID "" "Inserting into message queue : TO:{$to} SUBJECT:{$subject} BODY:{$body}" ""
	### This section needs fixing up - 2011-06-30, PMD
	if {"$attachment" > ""} {
		set sql "insert into message (date_time_entered,process_type,status,msg_to,msg_from,msg_subject,msg_body) values (getdate(),1,-1,'$to','$::CE_NAME','$subject','$body')"
		$::db_exec $::CONN $sql
		set sql "select @@IDENTITY"
		$::db_exec $::CONN $sql
		if {"$attachment" > ""} {
			set msg_id [tdbc_fetchrow $::CONN]
			email_attach_file $attachment $attachment_data $msg_id
		}
		set sql "update message set status = 0 where msg_id = $msg_id"
		$::db_exec $::CONN $sql
	} else {
		set sql "insert into message (date_time_entered,process_type,status,msg_to,msg_from,msg_subject,msg_body) values (getdate(),1,0,'$to','$::CE_NAME','$subject','$body')"
		$::db_exec $::CONN $sql
	}
}

##################################################
# end of send_email
##################################################

proc pre_initialize {} {
	set proc_name pre_initialize
#!/usr/bin/env tclsh
	#lappend ::auto_path [file join $::CATO_HOME lib]
	set BIN_DIR [file join $::CATO_HOME services bin]
	#package require cato_common
	
	read_config
	set ::LOGFILES [file join $::LOGFILES ce]
	set ::DEBUG_LEVEL 2
}
proc initialize {} {
	set proc_name initialize
	###
	### Set some globals stored in the ini file
	###
	package require Expect
	package require tdom
	package require mysqltcl
	set ::db_query ::mysql::sel
	set ::db_exec ::mysql::exec
	set ::db_fetch ::mysql::fetch
	set ::db_disconnect ::mysql::close
	set ::getdate "now()"
	exp_internal 0 ;# Keep this at 0 unless you want to debug further


	#################################################
	#
	# Global variable setup
	#
	#################################################

	set ::HOST_DOMAIN "$::tcl_platform(user)@[info hostname]"
	set ::KEY_FILES ""
	set ::env(TERM) xterm
	set ::AUDIT_TRAIL_ON 2
	set ::TRAP_SQL_ERROR 0
	set ::TIMEOUT_VALUE 20
	set ::NUM_OPEN_CONN 0
	#set ::env(ORACLE_HOME) $::HOME/lib/oracleclient
	#set ::env(PATH) "$::env(ORACLE_HOME);$::env(PATH)"
	set ::MY_PID [pid]
	set ::handle_names ""
	set ::BREAK 0
	set ::INLOOP 0
	set ::SENSITIVE ""
	set ::CLOUD_ENDPOINTS() ""
	set ::runtime_arr(_AWS_REGION,1) ""

	#set ::FILTER_BUFFER 0
	#set ::TIMEOUT_CODEBLOCK "" 
	#set ::ORA_ERROR_CODE 0
	#set ::ORA_ERROR_MSG ""
	#set ::ORA_SQL_ROWS 0
	#set ::number_list ""

	##################################################
	# end of global variable setup
	##################################################

}

proc toascii { char } {
	set value ""
	scan $char %c value
	return $value
}

proc tochar { value } {
	return [format %c $value]
}

proc get_steps {task_id} {
	
	set proc_name get_steps

	#output "into get_steps" 4


	set sql "select step_id, upper(codeblock_name), step_order, function_name
			, [sql_cast function_xml]
			, output_parse_type, output_row_delimiter, output_column_delimiter
			, [sql_cast variable_xml]
		from task_step 
		where task_id = '$task_id'
			and commented = 0
		order by codeblock_name, step_order asc"

	#output $sql 2
	$::db_query $::CONN $sql
	while {[string length [set row [$::db_fetch $::CONN]]] > 0} {

		#output "DEBUG: $row" 4
		if {[catch {set ::step_arr([string tolower [lindex $row 0]]) $row} err_msg]} {
			switch -glob $err_msg {
				"unmatched open brace*" {
					error_out "Syntax Error -> Unbalanced openning curly brace found in command, remove or escape brace from command." 2000
				}
				default {
					error_out "Syntax Error -> $err_msg" 2001
				}
			}
		}
		lappend ::codeblock_arr($task_id,[lindex $row 1]) [string tolower [lindex $row 0]]
	}
#	output "checking for output producing steps" 2

	#output "out of get_steps" 4
		
}


proc get_task_params {} {
	
	set proc_name get_task_params

	set sql "select [sql_cast parameter_xml] from task where task_id = '$::TASK_ID'"
	retrieve_params	$sql

	set sql "select [sql_cast parameter_xml] from task_instance_parameter where task_instance = '$::TASK_INSTANCE'"
	retrieve_params	$sql
}
proc parse_input_params {params} {
	set proc_name parse_input_params

	regsub -all "&" $params "&amp;" params
	set xmldoc [dom parse $params]
	set root [$xmldoc documentElement]
	set parameter_nodes [$root selectNodes {/parameters/parameter}]
	foreach node $parameter_nodes {
		### 2010-01-19 - PMD - variable names case insensitive
		set name [string toupper [$node selectNodes string(name)]]
		set value_nodes [$node selectNodes {./values}]
		set is_encrypted [$node getAttribute encrypt ""]
		set ii 1
		array unset ::runtime_arr $name,*
		if {"$value_nodes" > ""} {
			foreach value_node [$value_nodes childNodes] {
				set value [fix [$value_node asText]]
				if {"$is_encrypted" == "true" && "$value" > ""} {
					set value [decrypt_string $value $::SITE_KEY]
					lappend ::SENSITIVE $value
				}
				output "value node $name, $value" 3
				set_variable $name,$ii $value
				incr ii
			}
		}
	}
	$root delete
	$xmldoc delete
}
proc retrieve_params {sql} {
	set proc_name retrieve_params

	$::db_query $::CONN $sql
	set params [lindex [$::db_fetch $::CONN] 0]
	output "Task parameters >$params<." 2
	if {"$params" > ""} {
		parse_input_params $params
	}
}

proc set_handle_var {var val} {
	set proc_name set_handle_var
	
	### 2010-01-19 - PMD - variable names case insensitive
		set var [string toupper $var]
		set ::HANDLE_ARR($var) "$val"	
}

proc refresh_handles {} {
	set proc_name refresh_handles
	
	if [info exists ::handle_names] {
		#for each "handle" in some array of handle names
		output "Refreshing handles..." 1
		foreach handle $::handle_names {
			set handle_instance $::HANDLE_ARR(#${handle}.INSTANCE)
			set sql "select ti.task_status, ti.started_dt, ti.completed_dt, ti.ce_node, ti.pid, 
				a.asset_id, a.asset_name, ti.task_id, t.task_name, ti.submitted_by, t.version, 
				t.default_version, ti.submitted_dt
				from tv_task_instance ti 
				join task t on ti.task_id = t.task_id 
				left outer join asset a on a.asset_id = ti.asset_id 
				where ti.task_instance = $handle_instance"
			$::db_query $::CONN $sql
			set row [$::db_fetch $::CONN]

			# set some variables ...
			set_handle_var "#${handle}.STATUS" [lindex $row 0]
			set_handle_var "#${handle}.STARTED_DT" [lindex $row 1]
			set_handle_var "#${handle}.COMPLETED_DT" [lindex $row 2]
			set_handle_var "#${handle}.CENODE" [lindex $row 3]
			set_handle_var "#${handle}.PID" [lindex $row 4]
			set_handle_var "#${handle}.ASSET" [lindex $row 5]
			set_handle_var "#${handle}.ASSET_NAME" [lindex $row 6]
			set_handle_var "#${handle}.TASK_ID" [lindex $row 7]
			set_handle_var "#${handle}.TASK_NAME" [lindex $row 8]
			set_handle_var "#${handle}.SUBMITTED_BY" [lindex $row 9]
			set_handle_var "#${handle}.TASK_VERSION" [lindex $row 10]
			set_handle_var "#${handle}.IS_DEFAULT" [lindex $row 11]
			set_handle_var "#${handle}.SUBMITTED_DT" [lindex $row 12]

			output "... {#$handle} refreshed." 1
		}	
	}
}

proc set_variable {variable set_string} {
	set proc_name set_variable

	#output "set_string is $set_string"

	set variable [replace_variables_all $variable]
	set set_string [replace_variables_all $set_string]
	#output "the variable is $variable set to $set_string" 

	if {"[string range $set_string 0 9]" == "array_diff"} {
		### 2010-01-19 - PMD - variable names case insensitive
		set array_1 [string toupper [lindex $set_string 1]]
		set array_2 [string toupper [lindex $set_string 2]]
		if {[string length $array_1] == 0 || [string length $array_2] == 0} {
			error_out "Syntax Error -> Set Variable array_diff: wrong number of arguments. Should be array_diff ARRAYNAME1 ARRAYNAME2."  2205
		}
		set set_1 ""
		foreach element_1 [array names ::runtime_arr $array_1,*] {
			lappend set_1 $::runtime_arr($element_1)
		}
		set set_2 ""
		foreach element_1 [array names ::runtime_arr $array_2,*] {
			lappend set_2 $::runtime_arr($element_1)
		}
		package require struct::set
		set set [::struct::set symdiff $set_1 $set_2]
		set ii 1
		foreach item $set {
			set ::runtime_arr($variable,$ii) $item
			incr ii
		}
	} elseif {"[string range $set_string 0 9]" == "array_same"} {
		### 2010-01-19 - PMD - variable names case insensitive
		set array_1 [string toupper [lindex $set_string 1]]
		set array_2 [string toupper [lindex $set_string 2]]
		if {[string length $array_1] == 0 || [string length $array_2] == 0} {
			error_out "Syntax Error -> Set Variable array_same: wrong number of arguments. Should be array_same ARRAYNAME1 ARRAYNAME2." 2206
		}
		set set_1 ""
		foreach element_1 [array names ::runtime_arr $array_1,*] {
			lappend set_1 $::runtime_arr($element_1)
		}
		set set_2 ""
		foreach element_1 [array names ::runtime_arr $array_2,*] {
			lappend set_2 $::runtime_arr($element_1)
		}
		package require struct::set
		set set [::struct::set intersect $set_1 $set_2]
		set ii 1
		foreach item $set {
			set ::runtime_arr($variable,$ii) $item
			incr ii
		}
	} elseif {"[string range $set_string 0 9]" == "array_join"} {
		### 2010-01-19 - PMD - variable names case insensitive
		set array_1 [string toupper [lindex $set_string 1]]
		set array_2 [string toupper [lindex $set_string 2]]
		if {[string length $array_1] == 0 || [string length $array_2] == 0} {
			error_out "Syntax Error -> Set Variable array_join: wrong number of arguments. Should be array_join ARRAYNAME1 ARRAYNAME2." 2207
		}
		set set_1 ""
		foreach element_1 [array names ::runtime_arr $array_1,*] {
			lappend set_1 $::runtime_arr($element_1)
		}
		set set_2 ""
		foreach element_1 [array names ::runtime_arr $array_2,*] {
			lappend set_2 $::runtime_arr($element_1)
		}
		package require struct::set
		set set [::struct::set union $set_1 $set_2]
		set ii 1
		foreach item $set {
			set ::runtime_arr($variable,$ii) $item
			incr ii
		}
	} elseif {[llength [split $variable ,]] > 1 && [string is integer [lindex [split $variable ,] 1]] == 1} {
		### 2010-01-19 - PMD - variable names case insensitive
		set variable [string toupper $variable]
		set ::runtime_arr([lindex [split $variable ,] 0],[lindex [split $variable ,] 1]) "$set_string"
		#output "setting, ::runtime_arr([lindex [split $variable ,] 0],[lindex [split $variable ,] 1]) = >$::runtime_arr([lindex [split $variable ,] 0],[lindex [split $variable ,] 1])<"
	} else {
		#output "setting ::runtime_arr($variable,1) $::runtime_arr($variable,1)"
		### 2010-01-19 - PMD - variable names case insensitive
		array unset ::runtime_arr $variable,*
		set variable [string toupper $variable]
		set ::runtime_arr($variable,1) "$set_string"
	}

	#output "out of $proc_name" 4
}

proc update_status {task_status} {
	set proc_name update_status

	global TASK_INSTANCE

	### PMD 2007-11-14 - removed interface type reference and logic

	output "Updating task instance {$::TASK_INSTANCE} to {$task_status}." 1

	switch -- $task_status {
		"Processing" {
			set additional ""
		}
		default {
			set additional ", completed_dt = $::getdate"
		}
	}
	set sql "update task_instance set task_status = '$task_status' $additional where task_instance = $::TASK_INSTANCE"

	$::db_exec $::CONN $sql
}
proc gather_system_info {asset_id} {
	set proc_name gather_system_info

	global SYSTEMS

	#output "into gather_system_info" 4

	lappend SYSTEMS $asset_id	

	set sql "select a.asset_name, a.address, a.port, a.db_name , a.connection_type, ac.username, ac.password, ac.domain, ac.privileged_password, a.conn_string 
		from asset a left outer join asset_credential ac on a.credential_id = ac.credential_id
		where asset_id = '$asset_id'"

	#output "DEBUG: $sql" 0

	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	#output "DEBUG: $row" 0

	set password [lindex $row 6]
	set p_password [lindex $row 8]

	if {[string length $p_password]} {
		set p_password [decrypt_string $p_password $::SITE_KEY]
	}
	if {[string length $password]} {
		set password [decrypt_string $password $::SITE_KEY]
	}

	set ::system_arr($asset_id,name) [lindex $row 0]
	set ::system_arr($asset_id,address) [lindex $row 1]
	set ::system_arr($asset_id,port) [lindex $row 2]
	set ::system_arr($asset_id,db_name) [lindex $row 3]
	set ::system_arr($asset_id,conn_type) [lindex $row 4]
	set ::system_arr($asset_id,userid) [lindex $row 5]
	set ::system_arr($asset_id,password) $password
	set ::system_arr($asset_id,priv_password) $p_password
	set ::system_arr($asset_id,domain) [lindex $row 7]
	set ::system_arr($asset_id,conn_string) [lindex $row 9]

	#output "out of gather_system_info" 4

}

proc error_out {error_msg error_type} {
	set proc_name error_out

	set AUDIT_TRAIL_ON 2
	
	#if {![info exists ::STEP_ID]} {
	#	set ::STEP_ID 0
	#}
	error $error_msg $error_msg $error_type
}

proc buildProxyHeaders {username password} {
	set proc_name buildProxyHeaders
	return [list "Authorization" \
	[concat "Basic" [base64::encode $username:$password]]]
}

proc httpCopyProgress {args} {
   puts -nonewline .
}

proc oracle_logon {user_id password oracle_sid address port conn_string} {
	set proc_name oracle_logon
	output "ORACLE_HOME is {$::env(ORACLE_HOME)}." 4
	package require Oratcl

	## Put in dynamic port logic
	## PMD 2005-06-02
	if {"$port" == "" || "$port" == "0"} {
		set port 1521
	}



        if {"$conn_string" > ""} {
                set conn_string "$user_id/$password@$conn_string"

        } else {
		set conn_string "$user_id/$password@(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=$address)(PORT=$port)))(CONNECT_DATA=(SID=$oracle_sid)))"
	}

	output "Attempting connection at address {$address} port {$port} for SID {$oracle_sid} user {$user_id}." 1
	#output $conn_string
	if {[catch {set spawn_id [oralogon $conn_string]} error_msg]} {
		if {[string match "ORA-12541*" $error_msg]} {
			if {$port == 1521} {
				set port 1526
			} else {
				set port 1521
			}
			output "Now attempting connection at address {$address} port {$port} for SID {$oracle_sid} user {$user_id}" 1
			set conn_string "$user_id/$password@(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=$address)(PORT=$port)))(CONNECT_DATA=(SID=$oracle_sid)))"
		}
		if {[catch {set spawn_id [oralogon $conn_string]} error_msg]} {
			error_out "Oracle logon error: $error_msg" 1000
		}
	}
	#oraconfig $spawn_id_arr($conn_name,handle) fetchrows 10000
	## The following line should be in place when bind variables are used.
	## Comment out this line when it's no longer needed.
	## PMD 2005-05-20
	oraautocom $spawn_id 1
	insert_audit $::STEP_ID "" "Oracle connection to host=$address, sid=$oracle_sid, user=$user_id established." ""
	return $spawn_id

}
proc sybase_logon {user_id password address port dbname} {
	set proc_name sybase_logon
	global TASK_INSTANCE

	# NOTE: must add tdsLevel=CS_TDS_42 support to libcentivia's DBLib 
	if {[catch {set sybconn [tdbc_connect "$user_id" "$password" "$address" "$dbname" "sqlserver" "$port" "command_engine.$::TASK_INSTANCE.$::MY_PID"]} error_msg]} {
		error_out "Sybase connection error: $error_msg" 1001
	}
	insert_audit $::STEP_ID "" "Sybase connection to host=$address, user=$user_id established." ""
	return $sybconn
}
proc odbc_logon {user_id password dsn} {
    set proc_name odbc_logon

	output "DEBUG: odbc_logon $user_id $dsn\n"  1
	package require tdbc::odbc

	if {[catch {set odbcconn [tdbc::odbc::connection create db[clock clicks] "DSN=$dsn;UID=$user_id;PWD=$password"]} error_msg]} {
		error_out "ODBC connection error: $error_msg" 1002
	}
	insert_audit $::STEP_ID "" "ODBC connection to datasource=$dsn, user=$user_id established." ""
	return $odbcconn

}
proc informix_logon {user_id password address port dbname} {
    set proc_name informix_logon
	global TASK_INSTANCE

	if {"$port" == ""} {
		set port I2
	}
	package require tdbc::odbc
	set conn_string "driver=IBM INFORMIX ODBC DRIVER;server=demo_on;host=$address;service=9088;uid=$user_id;pwd=$password;database=$dbname;protocol=onsoctcp"
	if {[catch {set odbcconn [tdbc::odbc::connection create dbconn[clock clicks] $conn_string]} error_msg]} {
		error_out "Informix ODBC connection error: $error_msg" 1002
	}
	insert_audit $::STEP_ID "" "Informix connection to host=$address, database=$dbname, port=$port user=$user_id established." ""
	return $odbcconn

}
proc ingres_logon {user_id password address port dbname} {
    set proc_name ingres_logon
	global TASK_INSTANCE

	if {"$port" == ""} {
		set port I2
	}
	package require tdbc::odbc
	set conn_string "driver=Ingres;servertype=ingres;server=@$address,wintcp,$port;uid=$user_id;pwd=$password;database=$dbname"
	if {[catch {set odbcconn [tdbc::odbc::connection create dbconn[clock clicks] $conn_string]} error_msg]} {
		error_out "Ingres ODBC connection error: $error_msg" 1002
	}
	insert_audit $::STEP_ID "" "Ingres connection to host=$address, database=$dbname, port=$port user=$user_id established." ""
	return $odbcconn

}
proc sqlserver_logon {user_id password address port dbname} {
    set proc_name sqlserver_logon
	global TASK_INSTANCE

	if {"$port" == ""} {
	#	set port 1433
		set port_string ""	
	} else {
		set port_string ",$port"
	}
	output "DEBUG: sqlserver_logon $user_id $address$port_string $dbname odbcsqlserver command_engine.$::TASK_INSTANCE.$::MY_PID\n" 2

	package require tdbc::odbc
	set conn_string "driver=SQL Server;server=$address$port_string;uid=$user_id;pwd=$password;database=$dbname;app=command_engine.$::TASK_INSTANCE.$::MY_PID"
	if {[catch {set odbcconn [tdbc::odbc::connection create dbconn[clock clicks] $conn_string]} error_msg]} {
		error_out "SQL Server ODBC connection error: $error_msg" 1002
	}
	insert_audit $::STEP_ID "" "SQL Server connection to host=$address$port_string, user=$user_id established." ""
	return $odbcconn

}

proc connect_windows {address namespace userid password domain} {
	set proc_name connect_windows

	if {"$domain" > ""} {
		output "Connection via a Windows Domain account {$domain, $userid, $namespace}." 1
		set userid $domain\\$userid
	} else {
		output "Connection via a Windows Local account {$userid, $namespace}." 1
		set userid localhost\\$userid
	}
	if {"$namespace" == "_MAIN"} {
		if [catch {twapi::connect_share \\\\$address -user $userid -password $password} error_code] {
			if {[string match "*User credentials cannot be used for local connections*" $error_code]} {
				if [catch {twapi::connect_share \\\\$address} error_code] {
					error_out "Windows connection error to host $address\012$error_code" 1003
				}
			}  else {
				error_out "windows connection error to host $address, user $domain $userid\012$error_code" 1003
			}
		}
		set conn_id _MAIN
	} else {
		set olocator [twapi::comobj WbemScripting.SWbemLocator]
		$olocator Security_ ImpersonationLevel 3
		if [catch {set owmi [$olocator ConnectServer $address root\\$namespace $userid $password]} error_code] {
			if {[string match "*User credentials cannot be used for local connections*" $error_code]} {
				if [catch {set owmi [$olocator ConnectServer $address root\\$namespace]} error_code] {
					error_out "windows connection error to host $address\012$error_code" 1003
				}
			}  else {
				error_out "windows connection error to host $address, user $domain $userid\012$error_code" 1003
			}
		}
		$olocator -destroy
		set conn_id $owmi
	}
	insert_audit $::STEP_ID "" "Connected to Windows host {$address} with user {$userid}." ""
	return $conn_id
}


##################################################
#	procedure: ftp_logon
#
#	Call this proc to logon to a system
#	using ftp
#
##################################################

proc ftp_logon {address userid password} {
	set proc_name ftp_logon
	upvar system system
	set timeout 60
	set send_slow {3 .0000001}
	set step_id 99999
	upvar spawn_id spawn_id
	output "logging onto $address using $userid" 1

	exp_send -s -- "ftp $address\r"
	expect {
		"): " {}
		timeout {

			set error_msg "Timeout waiting for logon prompt, possible wrong network address or network problem"
			error_out $error_msg 1004

		}
	}
	exp_send -s -- "$userid\r"
	expect {
		"assword:" {}
		timeout {

			set error_msg "Timeout waiting for password prompt"
			error_out $error_msg 1005

		}
                "denied" {
			set error_msg "ftp access denied to $address for user $userid"
			error_out $error_msg 1007
                }
                "incorrect" {
			set error_msg "User id is incorrect"
			error_out $error_msg 1006
                }
	}
	log_user 0
	exp_send -s -- "$password\r"

	if {$::DEBUG_LEVEL > -2} {
		log_user 1
	}
	expect {
                "Login failed" {
                        set error_msg "User id or password is incorrect or login failed for other reason."
                        error_out $error_msg 1007
                }
                "Login Failed" {

                        set error_msg "User id or password is incorrect or login failed for other reason."
			error_out $error_msg 1007
                }
                "Sorry, This Account is NOT Valid on this System" {

                        set error_msg "User id or password is incorrect or login failed for other reason."
			error_out $error_msg 1007

                }
                "denied" {
                        set error_msg "User id or password is incorrect or login failed for other reason."
			error_out $error_msg 1007

                }
                "incorrect" {

                        set error_msg "User id or password is incorrect or login failed for other reason."
			error_out $error_msg 1007

                }
		"ftp> " {
			#output "Found the ftp prompt." 3
                }
                timeout {

			set error_msg "Timeout waiting for ftp prompt."
			error_out $error_msg 1008

                }

	}	
}

proc telnet_logon {address userid password top telnet_ssh attempt_num private_key} {
	set proc_name telnet_logon

	upvar system system
        upvar spawn_id spawn_id
	set step_id 99999
	set timeout 30
	set send_slow {3 .0000001}
	set do_password 1	
	set passphrase_required 0
	#exp_internal 1
	#set ::env(TERM) cygwin 
	#set ::exp::winnt_debug 1

	if {$top == "yes"} {	;# This is the machine we will launch from
		if {[string compare $telnet_ssh "telnet"] == 0} {
			if {"[lindex $address 1]" > ""} {
				spawn telnet [lindex $address 0] [lindex $address 1]
			} else {
				#spawn telnet $address
				catch {spawn telnet $address} error_msg
				if {[string match "*could not create pipe*" $error_msg]} {
					global TASK_INSTANCE
					output "spawn errored out, going to reattempt...\012$error_msg" 0
					sleep 20
				spawn telnet $address
			}
			}

		} else {
			#output "spawning ssh..."
#output "before"
			#spawn bin/ssh -l $userid $address
			if {"$private_key" > ""} {
				package require uuid
				set key_file [::uuid::uuid generate].pem
				lappend ::KEY_FILES $key_file
				set fp [open $::TMP/$key_file w]
				puts $fp $private_key
				close $fp
				file attributes $::TMP/$key_file -permissions 0600
				spawn /usr/bin/ssh -i $::TMP/$key_file $userid@$address
			} else {
				spawn /usr/bin/ssh $userid@$address
			}
#output "after"
		}
	} else {		;# We're already on some other machine
		if {[string compare $telnet_ssh "telnet"] == 0} {
			exp_send -s -- "telnet $address\r"
		} else {
			exp_send -s -- "bin/ssh -l $userid $address\r"
		}
	}
	output "Opened ssh." 1

	if {[string compare $telnet_ssh "telnet"] == 0} {
		expect {
			"sername:" {}
			 {onnection reset by peer} {
				set error_msg "System connection refused. Cannot login."
				error_out $error_msg 1009
			}
			 {onnection refused} {
                                if {$attempt_num == 1} {
                                        close;wait
                                        return 1
                                } else {
					set error_msg "System connection refused. Cannot login."
                                        error_out $error_msg 1011
                                }
			}
			 {onnection closed by} {
				set error_msg "System connection refused. Cannot login."
				error_out $error_msg 1009
			}
                        "Connection timed out" {
                                if {$attempt_num == 1} {
                                        close;wait
                                        return 1
                                } else {
                                        set error_msg "Telnet Connection to $address timed out"
                                        error_out $error_msg 1010
                                }
                        }
                        "can not find channel" {
                                if {$attempt_num == 1} {
                                        close;wait
                                        return 1
                                } else {
                                        set error_msg "Cannot find channel error, possible resource problem"
                                        error_out $error_msg 1011
                                }
                        }
			"ogin: " {}
			timeout {

				if {$attempt_num == 1} {
					close;wait
					return 1
				} else {
					set error_msg "Timeout waiting for logon prompt, possible wrong network address or network problem"
					error_out $error_msg 1012
				}
			}
		}
		exp_send -s -- "$userid\r"
	}
	output "Looking for password prompt..." 1
	expect {
		-re "password will expire(.*)assword: " {
			insert_audit $::STEP_ID "$telnet_ssh password for userid {$userid} set to expire." ""
		}
		-re "expired|Old password:" {
			set error_msg "$telnet_ssh password for userid $userid is expired, cannot login."
			error_out $error_msg 1013
		}
		"No route to host" {
			#set error_msg "$telnet_ssh no route to host $address, check network or firewall settings"
			#error_out $error_msg 3000
			if {$attempt_num == 1} {
				close;wait
				return 1
			} else {
				set error_msg "$telnet_ssh connection to $address timed out after $attempt_num attempts with the error no route to host, check network or firewall settings"
				error_out $error_msg 1014
			}
		}
		"Connection timed out" {
			if {$attempt_num == 1} {
				close;wait
				return 1
			} else {
				set error_msg "Telnet or ssh connection to $address timed out"
				error_out $error_msg 1014
			}
		}
		-re "passphrase for key(.*):" {
			set passphrase_required 1
		}
		"assword: " {}
		"assword:" {}
                "yes/no" {
			send "yes\r"
                        expect {
				"password will expire" {
					insert_audit $::STEP_ID "$telnet_ssh password for userid {$userid} set to expire." ""
				}
				-re "expired|Old password:" {
					set error_msg "$telnet_ssh password for userid $userid is expired, cannot login."
					error_out $error_msg 1015
				}
				"assword:" {}
				-re "passphrase for key(.*):" {
					set passphrase_required 1
				}
				-re "denied|incorrect" {
					set error_msg "User id $userid or its password is incorrect"
					error_out $error_msg 1016

				}
				"Name or service not known" {
					set error_msg "Name or service not known - DNS entry invalid"
					error_out $error_msg 1017

				}
				-re {onnection reset by peer|onnection refused|onnection closed by} {
					set error_msg "System connection refused. Cannot login."
					error_out $error_msg 1018
				}
				### PMD - bug 182 - 2007-05-03
				"yes/no" {
					send "yes\r"
					expect {
						"password will expire" {
							insert_audit $::STEP_ID "$telnet_ssh password for userid {$userid} set to expire." ""
						}
						-re "expired|Old password:" {
							set error_msg "$telnet_ssh password for userid $userid is expired, cannot login."
							error_out $error_msg 1015
						}
						"assword: " {}
						"assword:" {}
						-re "passphrase for key(.*):" {
							set passphrase_required 1
						}
						-re "denied|incorrect" {
							set error_msg "User id $userid or its password is incorrect"
							error_out $error_msg 1016

						}
						"Name or service not known" {
							set error_msg "Name or service not known - DNS entry invalid"
							error_out $error_msg 1017
						}
						-re {onnection reset by peer|onnection refused|onnection closed by} {
							set error_msg "System connection refused. Cannot login."
							error_out $error_msg 1018
						}
						timeout {
							if {$attempt_num == 1} {
								close;wait
								return 1
							} else {

								set error_msg "Timeout waiting for password prompt"
								error_out $error_msg 1019
							}
						}
					}
				}
				-re "\\\$ $" {
					#output "Found the dollar prompt" 3
					set system_flag "UNIX"
					set do_password 0
				}
				timeout {
					if {$attempt_num == 1} {
						close;wait
						return 1
					} else {

						set error_msg "Timeout waiting for password prompt"
						error_out $error_msg 1019
					}
				}
			}
                }
                "Name or service not known" {
                        set error_msg "Name or service not known - DNS entry invalid"
                        error_out $error_msg 1017
                }
		 {Read from socket failed} {
			if {$attempt_num == 1} {
				close;wait
				return 1
			} else {
				set error_msg "System connection refused. Cannot login."
				error_out $error_msg 1011
			}
		}
		-re "onnection reset by peer" {
			set error_msg "Connection reset by peer. Cannot login."
			error_out $error_msg 9999
		}
		-re "onnection refused" {
			if {$attempt_num == 1} {
				close;wait
				return 1
			} else {
				set error_msg "System connection refused. Cannot login."
				error_out $error_msg 1011
			}
		}
		-re "onnection closed by" {
			if {$attempt_num == 1} {
				close;wait
				return 1
			} else {
				set error_msg "Connection closed by target. Cannot login."
				error_out $error_msg 9999
			}
		}
		-re "Host key verification failed" {
			### PMD - fix for bugs 345 and 356 - 2006-10-11 - remove host key and try again
			if {$attempt_num == 1} {
				output "REGENERATING KEY..." 1
				catch {exec ssh-keygen -R $address} error_msg
				output $error_msg 0
				close;wait
				return 1
			} else {
				set error_msg "ssh Host key verification failed"
				error_out $error_msg 1020
			}
		
		}
		### PMD - fix for bug 67 - 2006-07-13
		-re "Authentication failed" {
			set error_msg "User id $userid or its password is incorrect or access is denied."
			error_out $error_msg 1021
		}	
		-re "denied|incorrect" {
			set error_msg "User id $userid or its password is incorrect or access is denied."
			error_out $error_msg 1021
		
		}
		-re "\\\$ $" {
			#output "Found the dollar prompt" 3
			set system_flag "UNIX"
			set do_password 0
                }
		timeout {
			if {$attempt_num == 1} {
				close;wait
				return 1
			} else {
				set error_msg "Timeout waiting for password prompt"
				error_out $error_msg 1019
			}
		}
	}
	if {$passphrase_required == 1 && "$password" == ""} {
		error_out "ssh passphrase is required but no passphrase was supplied. Check the passphrase for ssh private key." 9999
	}
	if {$do_password == 1} {
		log_user 1
		exp_send -s -- "$password\r"
		if {$::DEBUG_LEVEL > -2} {
			log_user 1
		}
		expect {
			eof {
				error_out "ssh connection closed unexpectedly." 9999
			}
			-re "passphrase for key(.*):" {
				error_out "ssh passphrase is incorrect. Check the passphrase for the ssh private key." 9999
			}
			-re "Permission denied(.*)publickey" {
				error_out "ssh passphrase is incorrect. Check the passphrase for the ssh private key." 9999
			}
			-gl "TERM = (*) " {
				exp_send -s -- "vt100\r"	
				expect {
					"password will expire" {
						insert_audit $::STEP_ID "$telnet_ssh password for userid {$userid} set to expire." ""
					}
					-re "password has expired|Old password:" {
						set error_msg "$telnet_ssh password for userid $userid is expired, cannot login."
						error_out $error_msg 1013
					}
					-re "denied|incorrect|Login Failed|This Account is NOT Valid|denied" {
						set error_msg "User id $userid or its password is incorrect or access is denied"
						error_out $error_msg 1021
					
					}
					-re "\\\$|: $" {
						#output "Found the dollar prompt" 3
						set system_flag "UNIX"
					}
					-re "^\[A-z]:\\\\\(.*)>" {
						#output "Found the dos prompt" 3
						set system_flag "DOS"
					}
					-re ">" {
						#output "Found the prompt" 3
						set system_flag "UNIX"
					}
					timeout {

						if {$attempt_num == 1} {
							close;wait
							return 1
						} else {
							set error_msg "Timeout waiting for command line prompt"
							error_out $error_msg 1022
						}
					}

				}
			}
			-re "password will expire on (.*)$" {
				set expdate "[lrange $expect_out(1,string) 1 3] [lindex $expect_out(1,string) 5]"
				set expseconds [clock scan "Aug 20 09:24:02 2002"]
				set nowseconds [clock scan now]
				if {[expr ((($expseconds - $nowseconds) / 60) / 60) / 24] < 0} {
					insert_audit $::STEP_ID "$telnet_ssh pswd for user {$userid} expires in less than [expr ((($expseconds - $nowseconds) / 60) / 60)] hours!" ""
				} elseif {[expr ((($expseconds - $nowseconds) / 60) / 60) / 24] < 3} {
					insert_audit $::STEP_ID "$telnet_ssh password for userid {$userid} expires in less than [expr ((($expseconds - $nowseconds) / 60) / 60) / 24] days." ""
				} else {
				
					insert_audit $::STEP_ID "$telnet_ssh password for userid {$userid} expires in less than [expr ((($expseconds - $nowseconds) / 60) / 60) / 24] days." ""
				}

				expect {
					-gl "TERM = (*) " {
						exp_send -s -- "\r"	
						expect {
							-re "\\\$ $" {
								#output "Found the dollar prompt" 3
								set system_flag "UNIX"
							}
							-re "^\[A-z]:\\\\\(.*)>" {
								#output "Found the dos prompt" 3
								set system_flag "DOS"
							}
							-re ">" {
								#output "Found the prompt" 3
								set system_flag "UNIX"
							}
							timeout {

								if {$attempt_num == 1} {
									close;wait
									return 1
								} else {
									set error_msg "Timeout waiting for command line prompt"
									error_out $error_msg 1022
								}
							}

						}
					}
					-re "\\\$ $" {
						#output "Found the dollar prompt" 3
						set system_flag "UNIX"
					}
					-re "^\[A-z]:\\\\\(.*)>" {
						#output "Found the dos prompt" 3
						set system_flag "DOS"
					}
					-re ">" {
						#output "Found the prompt" 3
						set system_flag "UNIX"
					}
					timeout {

						if {$attempt_num == 1} {
							close;wait
							return 1
						} else {
							set error_msg "Timeout waiting for command line prompt"
							error_out $error_msg 1022
						}
					}
				}
			}
			-re "password has expired|Old password:" {
				set error_msg "$telnet_ssh password for userid $userid is expired, cannot login."
				error_out $error_msg 1015
			}
			-re "Connection to (.*) closed." {
				if {$attempt_num == 1} {
					close;wait
					sleep 2
					return 1
				} else {
					set error_msg "Connection to $address closed."
					error_out $error_msg 1023
				}
			
			}
			"Authentication failed" {
				set error_msg "$telnet_ssh user id $userid authentication failed"
				error_out $error_msg 1024
			}
			"Permission denied" {
				set error_msg "$telnet_ssh user id $userid logon permission denied"
				error_out $error_msg 1025
			}
			"Received disconnect from" {
				if {$attempt_num == 1} {
					close;wait
					sleep 2
					return 1
				} else {
					set error_msg "Received disconnect from $address"
					error_out $error_msg 1026
				}
			}
			"Cannot open file for output: /tmp" {
				if {$attempt_num == 1} {
					close;wait
					sleep 2
					return 1
				} else {
					set error_msg "Cannot open file for output: /tmp"
					error_out $error_msg 1027
				}
			}
			-re "incorrect|Login Failed|This Account is NOT Valid" {
				set error_msg "User id $userid or its password is incorrect or access is denied."
				error_out $error_msg 1021
			
			}
			-re "To continue press ENTER|Select desired option:" {
				### Loading Local initialization file ...
				exp_send -s -- ""
				expect {
					-re "telnet> $" {}
					timeout {
						if {$attempt_num == 1} {
							close;wait
							return 1
						} else {
							set error_msg "Timeout waiting for command line prompt"
							error_out $error_msg 1022
						}
					}

				}
				exp_send -s -- "send brk\r"
				expect {
					-re "\\\$ $" {
						#output "Found the dollar prompt" 3
						set system_flag "UNIX"
					}
					-re ">|% $" {
						#output "Found the prompt" 3
						set system_flag "UNIX"
					}
					timeout {
						if {$attempt_num == 1} {
							close;wait
							return 1
						} else {
							set error_msg "Timeout waiting for command line prompt."
							error_out $error_msg 1022
						}
					}

				}
			}
				
			-re "\\\$ $" {
				#output "Found the dollar prompt" 3
				set system_flag "UNIX"
			}
			-re "^\[A-z]:\\\\\(.*)>" {
				#output "Found the dos prompt" 3
				set system_flag "DOS"
			}
			-re "\\\$\\\) $" {
				#output "Found the prompt" 3
				set system_flag "UNIX"
			}
			### PMD - fix to discount > and % in banners - 2006-01-28
			#-re ">|% $" {
			#	#output "Found the prompt" 3
			#	set system_flag "UNIX"
			#}
			"> $" {
				set system_flag "UNIX"
			}
			"% $" {
				set system_flag "UNIX"
			}
			### end of fix
			timeout {
				if {$attempt_num == 1} {
					close;wait
					return 1
				} else {
					set error_msg "Timeout waiting for command line prompt."
					error_out $error_msg 1022
				}
			}
		}	
	}
	if {[info exists system_flag] == 0} {
		if {$attempt_num == 1} {
			close;wait
			return 1
		} else {
			set error_msg "Unable to determine unix or dos telnet type."
			error_out $error_msg 1028
		}
	}
	if {[string compare $system_flag UNIX] == 0} {
		exp_send -s -- "unset PROMPT_COMMAND;export PS1='PROMPT>'\r"
		expect {
			-re "PROMPT>(.*)\012PROMPT>$" {
			}
			timeout {
				if {$attempt_num == 1} {
					close;wait
					return 1
				} else {
					set error_msg "Time out resetting command line prompt."
					set error_type "Login Error"
					error_out $error_msg $error_type
				}
			}
		}
		exp_send -s -- "stty -onlcr;export PS2='';stty -echo;unalias ls\r"
		expect {
			"export: Command not found." {
				exp_send -s -- "set editor=vi;set prompt=PROMPT\\>;set columns=10000\r"
				expect {
					-re "PROMPT>$" {
					}
					timeout {
						if {$attempt_num == 1} {
							close;wait
							return 1
						} else {
							set error_msg "Time out resetting command line prompt."
							error_out $error_msg 1029
						}

					}
				}
			}
			-re "PROMPT>$" {
				#exp_send -s -- "set horizontal-scroll-mode off;export COLUMNS=10000;stty cols 10000;export HISTFILE='';stty -echo\r"
				exp_send -s -- "export COLUMNS=10000;stty cols 10000;export HISTFILE=''\r"
				expect {
					-re "PROMPT>$" {
					}
				}
			}
			timeout {
				if {$attempt_num == 1} {
					close;wait
					return 1
				} else {
					set error_msg "Time out resetting command line prompt."
					error_out $error_msg 1029
				}

			}
		}
	} else { 
		exp_send -s -- "PROMPT=PROMPT\$G\r"
		expect {
			"export: Command not found." {
                                exp_send -s -- "set prompt=PROMPT\\>\r"
                                expect {
                                        -re "PROMPT>$" {
                                        }
                                        timeout {
                                                if {$attempt_num == 1} {
                                                        close;wait
                                                        return 1
                                                } else {
                                                        set error_msg "Time out resetting command line prompt"
                                                        error_out $error_msg 1029
                                                }

					}
				}
			}
			-re "PROMPT(.*)PROMPT>" {
			}
			timeout {

				if {$attempt_num == 1} {
					close;wait
					return 1
				} else {
					set error_msg "Time out resetting command line prompt."
					error_out $error_msg 1029
				}
			}
		}
	}
	insert_audit $::STEP_ID "" "$telnet_ssh connection to host=$address, user=$userid established." ""
	return 0
		
}

proc connect_dos {} {
	set proc_name connect_dos

	# Debug MBM
	#exp_internal 1
	
	set step_id 99999
	set timeout 5
	set send_slow {3 .0000001}


	set nt_prompt "PROMPT\$G"
	set nt_prompt_re "PROMPT>"

	spawn cmd 
        
	
	send -s -- "set PROMPT=$nt_prompt\r"

	expect {
		-re $nt_prompt_re {
		}
		timeout {
		set error_msg "Timed-out resetting DOS prompt. Exiting."
		error_out $error_msg 1029

		}

	}
	return $spawn_id
}


proc sftp_logon {conn_id address userid password} {
	set proc_name sftp_logon
	set timeout 60
	set send_slow {3 .0000001}
	set spawn_id $conn_id


	exp_send -s -- "sftp $userid@$address\r"
	expect {
		 {failure in name resolution} {
			set error_msg "Failure in name resolution."
			error_out $error_msg 1100
		}
		### PMD - fix for bug 67 - 2006-07-13
		"assword:" {}
                "yes/no" {
			send "yes\r"
                        expect {
				"password will expire" {
					insert_audit $::STEP_ID "sftp password for userid {$userid} set to expire." ""
				}
				-re "expired|Old password:" {
					set error_msg "sftp password for userid $userid is expired, cannot login."
					error_out $error_msg 1102
				}
				"assword:" {}
				-re "denied|incorrect" {
					set error_msg "User id $userid or its password is incorrect or access is denied."
					error_out $error_msg 1103

				}
				"Name or service not known" {
					set error_msg "Name or service not known - DNS entry invalid."
					error_out $error_msg 1104
				}
				-re {onnection reset by peer|onnection refused|onnection closed by} {
					set error_msg "System connection refused. Cannot login."
					error_out $error_msg 1105
				}
				timeout {
					set error_msg "Timeout waiting for password prompt."
					error_out $error_msg 1106
				}
			}
                }
                "Name or service not known" {
                        set error_msg "Name or service not known - DNS entry invalid."
                        error_out $error_msg 1104
                }
		-re "onnection reset by peer|onnection refused|onnection closed by" {
			set error_msg "System connection refused. Cannot login."
			error_out $error_msg 1105
		}
		-re "Host key verification failed" {
			set error_msg "sftp Host key verification failed."
			error_out $error_msg 1107
		
		}
		-re "denied|incorrect" {
			set error_msg "User id $userid or its password is incorrect or access is denied."
			error_out $error_msg 1103
		
		}
                "sftp>" {
                }
		timeout {
			set error_msg "Timeout waiting for password prompt"
			error_out $error_msg 1106
		}
	}
 
	log_user 0
	exp_send -s -- "$password\r"

	if {$::DEBUG_LEVEL > -2} {
		log_user 1
	}
	expect {
		-re "password has expired|Old password:" {
			set error_msg "$telnet_ssh password for userid $userid is expired, cannot login."
			error_out $error_msg 1102
		}
		-re "Connection to (.*) closed." {
			set error_msg "Connection to $address closed."
			error_out $error_msg 1108 
		
		}
		"Permission denied" {
			set error_msg "User id $userid or its password is incorrect or access is denied."
			error_out $error_msg 1103
		
		}
		"Received disconnect from" {
			set error_msg "Received disconnect from $address"
			error_out $error_msg 1109
		}
		-re "incorrect|Login Failed|This Account is NOT Valid" {
			set error_msg "User id $userid or its password is incorrect or access is denied."
			error_out $error_msg 1103
		
		}
                "sftp> " {
                }
		timeout {
			set error_msg "Timeout waiting for sftp prompt."
			error_out $error_msg 1110
		}
	}	
}

##################################################
#	end of telnet_logon
##################################################


##################################################
#	procedure: release_system
#
#	Call this proc to exit a system
#
##################################################

proc release_system {conn_name} {
	set proc_name release_system

	set send_slow {3 .0000001}
	set timeout 1
	output "******** Logging out {$conn_name} ********" 1


	if {[info exists ::connection_arr($conn_name,handle)]} {
		incr ::NUM_OPEN_CONN -1
		set spawn_id $::connection_arr($conn_name,handle)
		set system $::connection_arr($conn_name,system))
		set conn_type $::connection_arr($conn_name,conn_type)

		switch -- $conn_type {
			"oracle" {
				oraroll $spawn_id
				catch {oralogoff $spawn_id}
			}	
			"sqlserver" {
				#catch {tdbc_disconnect $spawn_id}
				catch {$spawn_id close}
			}	
			"windows" {

				catch {twapi::disconnect_share \\\\$system -updateprofile}
				foreach name [array names ::connection_arr $conn_name,handle,*] {
					output "releasing the connection internally named $name"
					if {"$name" != "$conn_name,handle,_MAIN"} {
						$::connection_arr($name) -destroy
						array unset ::connection_arr $name
					}
				} 
			}	
			default {	
				set exit_text ""
				switch -re -- "$conn_type" {
					telnet|ssh|su {
						set exit_text "exit"
					}
					sftp|ftp {
						set exit_text "quit"
					}
				}
				set timeout 1
				output "Sending {$exit_text} to spawn_id {$spawn_id}, conn_type {$conn_type}." 1
				if {[catch {exp_send -s -- "$exit_text\r"} return_code]} {
					output "The $exit_text returned - {$return_code}." 1
					catch {close -i $spawn_id;wait -i $spawn_id}
					return
				} else {
					expect {
						"Direct LOGOUT performed ." {
							catch {close -i $spawn_id;wait -i $spawn_id}
							insert_audit $::STEP_ID "logout" $expect_out(buffer) ""
						}
						"terminating]" {
							catch {close -i $spawn_id;wait -i $spawn_id}
							insert_audit $::STEP_ID "logout" $expect_out(buffer) ""
							#output "closed telnet" 4
						}
						"BYE BYE" {
							catch {close -i $spawn_id;wait -i $spawn_id}
							insert_audit $::STEP_ID "logout" $expect_out(buffer) ""
							#output "closed ssh" 4
						}
						"closed." {
							catch {close -i $spawn_id;wait -i $spawn_id}
							insert_audit $::STEP_ID "logout" $expect_out(buffer) ""
							#output "closed ssh" 4
						}
						timeout {
							catch {close -i $spawn_id;wait -i $spawn_id}
							output "Timeout." 0
						}
					}
				}
			}

		}
	}
}	
##################################################
#	end of release_system
##################################################

#proc is_number {var_list} {
#	global number_list
#	foreach this_var $var_list {
#		lappend number_list $this_var
#	}
#}
proc replace_variables_all {this_string} {
	set proc_name replace_variables_all
	while {[regexp {.*\[\[.*\]\]} $this_string] == 1 || [regexp ".*csk_calc\\(.*\\)" $this_string] == 1 || [regexp ".*csk_encrypt\\(.*\\)" $this_string] == 1} {
		set this_string [replace_variables $this_string]
	}
	if {[info exists ::ECO_REG]} {
		unset ::ECO_REG
	}
	return $this_string
}

proc replace_encrypt {the_string} {
        set proc_name replace_encrypt

        set first_index [string first "csk_encrypt\(" $the_string]
	set last_index [string first ")" $the_string $first_index]

        set little_string [replace_variables_all [string range $the_string [expr $first_index + 12] [expr $last_index -1]]]
	set string_to_encrypt [lindex $little_string 0]
	set key [lindex $little_string 1]
output "string to key $key" 1
	if {"$key" == ""} {
		set key "C3*e@n%]t&i#v!@i+|a"
	}
	set encrypted_string [encrypt_string $string_to_encrypt $key]
        return [string replace $the_string $first_index $last_index $encrypted_string]

}
proc replace_calc {the_string} {
        set proc_name replace_calc

        set first_index [string first "csk_calc\(" $the_string]
        set right_p 0
        set left_p 1
        for {set ii [expr $first_index + 5]} {$ii <= [string length $the_string]} {incr ii} {
                set char [string index $the_string $ii]
                #output "char is $char"
                if {"$char" == ")"} {
                        incr right_p
                } elseif {"$char" == "("} {
                        incr left_p
                }
                if {$right_p == $left_p} {
                        #output "we're in balance"
                        break
                }
        }
        #output "$ii is ii, $first_index is first index"
        if {$right_p != $left_p} {
                error_out "Unbalanced parenthesis in the calc function >$the_string<" 2002
        }

        if {[catch {set little_string [expr [string range $the_string [expr $first_index + 4] $ii]]} errorMsg]} {
		error_out "calc function error, invalid syntax, [string range $the_string [expr $first_index + 4] $ii]" 2003
	}
        return [string replace $the_string $first_index $ii $little_string]

}

proc next_close {s i} {
	set proc_name next_close
	return [string first "\]\]" $s $i]	
}
proc next_open {s i} {
	set proc_name next_open
	return [string first "\[\[" $s $i]	
}
proc lookup_asset_id {asset_name} {
	set proc_name lookup_asset_id

	set sql "select asset_id from asset where asset_name = '$asset_name'"
	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	set asset_id [lindex $row 0]
	if {"$asset_id" == ""} {
		output "Asset $asset_name is not a defined asset in the database." 0
	}
	return $asset_id
}
proc get_asset_attribute {var_name index} {
	set proc_name get_asset_attribute

	output "Getting Asset Attribute $var_name,$index ..." 1
	set asset [string range [lindex [split $var_name .] 0] 1 end]
	set attribute [lindex [split $var_name .] 1] 
	if {"$asset" == "_ASSET"} {
		set asset $::SYSTEM_ID
	} elseif {![is_guid $asset]} {
		set asset [lookup_asset_id $asset]
	}
	output "Asset is >$asset>, Attribute is >$attribute<, Index is >$index<" 1
	if {"$asset" > "" && "$attribute" > ""} {
		if {"$index" == "*"} {

			set sql "select count(*) from asset_attribute aa
				join lu_asset_attribute_value laav on laav.attribute_value_id = aa.attribute_value_id
				join lu_asset_attribute laa on laa.attribute_id = laav.attribute_id
					and aa.asset_id = '$asset'
					and laa.attribute_name = '$attribute'"
			$::db_query $::CONN $sql
			set row [$::db_fetch $::CONN]
			set var_value [lindex $row 0]
			if {"$var_value" == ""} {
				set var_value 0
			}
		} else {
			set sql "select laav.attribute_value from asset_attribute aa
				join lu_asset_attribute_value laav on laav.attribute_value_id = aa.attribute_value_id
				join lu_asset_attribute laa on laa.attribute_id = laav.attribute_id
					and aa.asset_id = '$asset'
					and laa.attribute_name = '$attribute'"
			if {"$index" == ""} {
				set index 0
			}
			#for lookups in the tcl array index is 1 based, and that's what the calling function sends, but for the rowset below it's 0 based?
			#Patrick should double check this, but subtracting 1 seems to set it right
			if {$index > 0} {
				set index [expr $index - 1]
			}
			$::db_query $::CONN $sql
			set row [$::db_fetch $::CONN]
			set var_value [lindex [lindex $row $index] 0]
		}
	} else {
		set var_value ""
	}
	return $var_value
}

#####################################################
#
#	procedure: replace_variables
#
#	This proc replaces variables found in a string
#	(command line) with the actual values
#
#####################################################

proc replace_variables {the_string} {
	set proc_name replace_variables

	global runtime_arr

	if {[info exists task_name] == 0} {
		global TASK_NAME
		set task_name $TASK_NAME
	}
	set count 0
	#output "looking in $the_string"
	if {[regexp ".*csk_encrypt\\(.*\\)" $the_string] == 1} {
		set the_string [replace_encrypt $the_string]
	}
	if {[regexp {.*\[\[.*\]\]}  $the_string] == 1 || [regexp ".*csk_calc\\(.*\\)" $the_string] == 1} {
		#output "found something"

		set subst_var ""
		set first_index [string first "\[\[" $the_string]
		set first_index_c [string first "csk_calc(" $the_string]
		#output "first_index = $first_index, first_index_c = $first_index_c"
		#output "$first_index > $first_index_c"
		if {$first_index > $first_index_c} {
			#output "variable replacement"
			incr first_index
			set second_index [expr [next_close $the_string 0] - 1]
			set first_index [expr $second_index - [next_open [string reverse [string range $the_string 0 [expr $second_index - 1]]] 0] - 1]
			#output "first index = $first_index, second index = $second_index"
			### 2010-01-19 - PMD - variable names case insensitive
			#set variable [string toupper [string range $the_string [expr $first_index + 1] $second_index]]
			set variable [string range $the_string [expr $first_index + 1] $second_index]
			#output "Replacing variable ->$variable<- in the string ->$the_string<-" 1

			if {[regexp {.*\[\[.*\]\]}  $variable] == 1 || [regexp ".*csk_calc\\(.*\\)" $variable] == 1} {

				#output "There's a variable inside of the another" 0
				#set first_index_v [expr [string first "\[\[" $variable] + 1]
				#set second_index_v [expr [string length $variable] - [string first "\]\]" [string reverse $variable]] - 3]
				#set second_index_v [expr [string length $variable] - [string first "\]\]" [string reverse $variable]] - 3]

				set variable [replace_variables $variable]
				#unset first_index_v second_index_v

			}
			#regsub -all {[ \r\t\n]+} [string toupper $variable] "" variable
			regsub -all {[ \r\t\n]+} $variable "" variable
			#output "first index is $first_index, second is $second_index, variable is >$variable<" 2


			if {[string first ",*" $variable] > 0} {
				set variable [string range $variable 0 [expr [string first ",*" $variable] - 1]]
				if {"[string index $variable 0]" == "@"} {
					set subst_var [get_asset_attribute $variable "*"]
				} else {
					#output "the variable to count is $variable"
					set subst_var [count_variable_array $variable]
				}
				set the_string [string range $the_string 0 [expr $first_index - 2]]$subst_var[string range $the_string [expr $second_index + 3] end]
			} else {
				if {[string first "," $variable] > 0} {
					set array_index [lindex [split $variable ","] 1]
					if {![string is integer $array_index]} {
						error_out "Variable substitution error, the index value $array_index in the substitution string $variable is not an integer. The array index must be a positive integer. Possibly missing brackets around $array_index variable?" 2004
					} 
					set variable [lindex [split $variable ","] 0]
					#output "array_index is $array_index, the variable is $variable" 0

					if {[string length $array_index] == 0} {
						set array_index 1
					}
				} else {
					set array_index 1
				}      
				switch -nocase -glob -- $variable {
					"#*" {
						#if the variable name starts with a "#", it's a "handle", and we replace it from a different array
						#IF it's there...
						if [info exists ::HANDLE_ARR($variable)] {
							refresh_handles
							set subst_var $::HANDLE_ARR($variable)
						}
					}
					"@*" {
						#if the variable name starts with a "@", it's an ecosystem registry var
						set subst_var [get_ecosystem_registry [string tolower [string range $variable 1 end]]]
					}
					"~*" {
						output "Variable is $variable"
						if {[string first "." $variable] > 0} {
							set var_name [string range $variable [expr [string first "." $variable] + 1 ] end]	
						} else {
							set var_name ""
						}
						set variable [string range [lindex [split $variable "."] 0] 1 end]
						if [info exists ::runtime_arr([string toupper $variable],$array_index)] {
							set xml $::runtime_arr([string toupper $variable],$array_index)
							if {"$var_name" > ""} {
								set subst_var [aws_get_result_var $xml $var_name]
							} else {
								set subst_var $xml
							}
							output "$var_name = $subst_var" 4
						} else {
							set subst_var ""
						}
					}
					"_TASK_INSTANCE" {
						set subst_var $::TASK_INSTANCE
					}
					"_ECOSYSTEM_NAME" {
						if {"$::ECOSYSTEM_NAME" == "" && "$::ECOSYSTEM_ID" > ""} {
							set subst_var [get_ecosystem_name]
						} else {
							set subst_var ""
						}
					}
					"_UUID2" -
					"_UUID_V2" {
						package require uuid
						set subst_var [::uuid::uuid generate]
						regsub -all -- "-" $subst_var "" subst_var
					}
					"_UUID" {
						package require uuid
						set subst_var [::uuid::uuid generate]
					}
					"_CLOUD_LOGIN_PASS" {
						set subst_var $::CLOUD_LOGIN_PASS
						lappend ::SENSITIVE $::CLOUD_LOGIN_PASS
					}
					"_CLOUD_LOGIN_ID" {
						set subst_var $::CLOUD_LOGIN_ID
						lappend ::SENSITIVE $::CLOUD_LOGIN_ID
					}
					"_TASK_NAME" {
						set subst_var $::TASK_NAME
					}
					"_TASK_VERSION" {
						set subst_var $::TASK_VERSION
					}
					"_SUBMITTED_BY_EMAIL" {
						if {[info exists ::SUBMITTED_BY_EMAIL] == 1} {
							set subst_var $::SUBMITTED_BY_EMAIL
						} else {
							set sql "select username from users where user_id = '$::SUBMITTED_BY'"
							$::db_query $::CONN $sql
							set row [$::db_fetch $::CONN]
							set subst_var [lindex $row 0]
							set ::SUBMITTED_BY_EMAIL $subst_var
						}
					}
					"_SUBMITTED_BY" {
						if {[info exists ::SUBMITTED_BY_NAME] == 1} {
							set subst_var $::SUBMITTED_BY_NAME
						} else {
							set sql "select username from users where user_id = '$::SUBMITTED_BY'"
							$::db_query $::CONN $sql
							set row [$::db_fetch $::CONN]
							set subst_var [lindex $row 0]
							set ::SUBMITTED_BY_NAME $subst_var
						}
					}
					_ASSET {
						set subst_var $::SYSTEM_ID
					}
					_DATE {
						set subst_var [clock format [clock seconds]]
					}
					_DATE(*) {
						set format_string [string range $variable 6 [expr [string length $variable] - 2]]
						output "the format string is $format_string]" 1
						if {"$format_string" == ""} {
							set subst_var [clock format [clock seconds]]
						} else {
							set subst_var [clock format [clock seconds] -format "$format_string"]
						}
					}
					default {
						if {[string first "." $variable] > 0} {
							set var_name [string range $variable [expr [string first "." $variable] + 1 ] end]	
output "var name is $var_name"
							set variable [lindex [split $variable "."] 0]
output "variable is $variable"
							if [info exists ::runtime_arr([string toupper $variable],$array_index)] {
								set xml $::runtime_arr([string toupper $variable],$array_index)
								if {"$var_name" > ""} {
									set subst_var [aws_get_result_var $xml $var_name]
								} else {
									set subst_var $xml
								}
								output "$var_name = $subst_var" 4
							} else {
								set subst_var ""
							}
						} else {
							if [info exists ::runtime_arr([string toupper $variable],$array_index)] {
								#output "the runtime variable used is ::runtime_arr($variable,$array_index) -> $::runtime_arr($variable,$array_index)"
								set subst_var $::runtime_arr([string toupper $variable],$array_index)
							} else {
								#output "the runtime variable ::runtime_arr($variable,$array_index) doesn't exist"
									set subst_var ""
							}	
						}
					}
				}
				#output "the string is $the_string"
				set the_string [string range $the_string 0 [expr $first_index - 2]]$subst_var[string range $the_string [expr $second_index + 3] end]

			}
			#output "DEBUG: After the substitution, string is $the_string" 0
		} elseif {$first_index_c > -1} {
			set the_string [replace_calc $the_string]
		}
	} 
	return $the_string
}

##################################################
#	end of replace_variables
##################################################
proc if_function {command} {
	set proc_name if_function

	set return_command ""

	#regsub -all "&" $command "&amp;" command
	set xmldoc [dom parse $command]
	set root [$xmldoc documentElement]

	set return_code 0
	set tests [$root selectNodes /function/tests/test]
	set result 0
	foreach test $tests { 
		
		set if_test [$test selectNodes string(eval)]
		regsub -all "&amp;" $if_test {\&} if_test
		regsub -all "&gt;" $if_test ">" if_test
		regsub -all "&lt;" $if_test "<" if_test
		output "Testing {$if_test}." 2

		if {[catch {set result [expr $if_test]} errorMsg]} {
			error_out "Syntax error in the following comparison ->\n$if_test\n\nPossible unbalanced comparison or illegal comparison operators used." 2005
		}
		if {$result == 1} {
			output "{$if_test} is positive." 3
			set return_command [$test selectNodes string(action)]
			if {"$return_command" == ""} {
				error_out "Error: 'IF' action is empty... action is required." 2019
			}
			break
		}
	}
	if {$result == 0} {
		output "Processing 'Else' condition..." 3
		set return_command [$root selectNodes string(else)]
	}
	$xmldoc delete
	return $return_command
}
#####################################################
#
#	procedure: is_guid
#
#	Tests a string to determine if it's a guid / uuid
#
#####################################################
proc is_guid {guid} {
	if {[string length $guid] != 36} {
		set return_code 0
	} elseif {[string index $guid 8] != "-"} {
		set return_code 0
	} elseif {[string index $guid 13] != "-"} {
		set return_code 0
	} elseif {[string index $guid 18] != "-"} {
		set return_code 0
	} elseif {[string index $guid 23] != "-"} {
		set return_code 0
	} elseif {[catch {binary format H* [string map {- {}} $guid]}]} {
		set return_code 0
	} else {
		set return_code 1
	}
	return $return_code
}

#####################################################
#
#	procedure: count_variable_array
#
#	This proc determines the number row in a variable array
#
#####################################################

proc count_variable_array {variable} {
	set proc_name count_variable_array

	#output [array names ::runtime_arr]
	if {"[string index $variable 0]" == "~"} {
		if {[string first $variable "."] > 0} {
			set count [llength [array names ::runtime_arr [string toupper $variable].*,*]]
		} else {
			set count [llength [array names ::runtime_arr [string toupper $variable],*]]
		}
	} else {
		set count [llength [array names ::runtime_arr [string toupper $variable],*]]
	}
        return $count
}

##################################################
#	end of count_variable_array
##################################################

proc process_buffer {output_buffer} {
	set proc_name process_buffer
	###
	### We need to find out if there is an runtime variable(s) to be
	### populated for this step. We'll find out the number of variables
	###

	output [lindex $::step_arr($::STEP_ID) 8] 1
	if {"[lindex $::step_arr($::STEP_ID) 8]" > ""} {
		set xmldoc [dom parse [lindex $::step_arr($::STEP_ID) 8]]
		set root [$xmldoc documentElement]
		set variable_nodes [$root selectNodes  {/variables/variable}]
	}

	if {[info exists variable_nodes]} {
		if {$::DEBUG_LEVEL >= 3} {
			set msg_text ""
		}
		set parse_type [lindex $::step_arr($::STEP_ID) 5]
		set row_delimiter [lindex $::step_arr($::STEP_ID) 6]
		set col_delimiter [lindex $::step_arr($::STEP_ID) 7]
		if {$row_delimiter > 0} {

			set row_count [llength [split $output_buffer [tochar $row_delimiter]]]
			if {$row_count > 1} {
				set output_buffer [split $output_buffer [tochar $row_delimiter]]
			}
			if {$::DEBUG_LEVEL >= 3} {
				set msg_text "Output Processing by delimiter, row delimiter is ascii $row_delimiter >[tochar $row_delimiter]<, number of rows = $row_count."
			}
		} else {
			set row_count 1
		}

		output "Column delimiter is ascii $col_delimiter >[tochar $col_delimiter]<." 1

		for {set jj 0} {$jj < $row_count} {incr jj} {
			if {$row_count > 1} {
				set split_line [lindex $output_buffer $jj]
			} else {
				set split_line $output_buffer
			}
			if {$::DEBUG_LEVEL >= 3} {
				set msg_text "$msg_text\n\nRow [expr $jj + 1] is:\n$split_line\n"
			}
			foreach the_node $variable_nodes {
				set type [$the_node selectNodes string(type)]
				### 2010-01-19 - PMD - variable names case insensitive
				set name [string toupper [$the_node selectNodes string(name)]]
				set column_value ""
				if {$jj == 0} {
					array unset ::runtime_arr $name,*
				}
				if {"$type" == "delimited" && [info exists del_split_line] == 0} {
					set del_split_line [split $split_line [tochar $col_delimiter]]
				}
				switch -- $type {
					delimited {
						set column_value ""
						set position [$the_node selectNodes string(position)]
						output "Setting column {$position} by delimiter." 2
						set column_value "[lindex $del_split_line [expr $position - 1]]"
						set ::runtime_arr($name,[expr $jj + 1]) $column_value
						if {$::DEBUG_LEVEL >= 3} {
							set msg_text "$msg_text\n\tsetting variable '$name,[expr $jj + 1]' using delim ascii $col_delimiter, col num $position: >$column_value<"  
						}
					}
					range {
						output "Setting column {$name} by range." 2
						set begin_pos -1
						set end_pos -1
						if {$::DEBUG_LEVEL >= 3} {
							set msg_text2 ""
						}
						set begin_pos [$the_node selectNodes string(range_begin)]
						if {"$begin_pos" == ""} {
							set prefix [$the_node selectNodes string(prefix)]
							set begin_pos [string first $prefix $split_line]
							if {$begin_pos > -1} {
								set begin_pos [expr $begin_pos + [string length $prefix]]
							}
							if {$::DEBUG_LEVEL >= 3} {
								set msg_text2 " prefix '$prefix'"
							}
						} else {
							if {$::DEBUG_LEVEL >= 3} {
								set msg_text2 " begin position '$begin_pos'"
							}
							if {![string is integer $begin_pos]} {
								error_out "The beginning position expected either an integer. Found '$begin_pos'." 9999
							}
							set begin_pos [string trim [expr [$the_node selectNodes string(range_begin)] - 1]]
						}
						set end_pos [$the_node selectNodes string(range_end)]
						if {"$end_pos" == ""} {
							set suffix [$the_node selectNodes string(suffix)]
							if {$::DEBUG_LEVEL >= 3} {
								set msg_text2 "$msg_text2, suffix '$suffix'"
							}
							set end_pos [string first $suffix [string range $split_line $begin_pos end]]
							output "Found suffix at position >$end_pos<." 1
							set end_pos [expr $end_pos + $begin_pos - 1]
						} else {
							set end_pos_orig $end_pos 
							set end_pos [string tolower [string trim $end_pos]]
							if {$::DEBUG_LEVEL >= 3} {
								set msg_text2 "$msg_text2, end position '$end_pos'"
							}
							if {[string index $end_pos 0] == "+"} {
								set end_pos [string range $end_pos 1 end]
								if {[string is integer $end_pos]} {
									set end_pos [expr $begin_pos + $end_pos -1]
								} else {
									error_out "The end position expected either 'end' , '+' and an integer or an integer. Found '$end_pos_orig'." 9999
								}
							} elseif {"$end_pos" != "end"} {
								if {[string is integer $end_pos]} {
									set end_pos [expr $end_pos - 1]
								} else {
									error_out "The end position expected either 'end' , '+' and an integer or an integer. Found '$end_pos_orig'." 9999
								}
							}
							unset end_pos_orig	
						}
						output "The begin position is {$begin_pos} and the end position is {$end_pos}." 2
						output "The begin position starts at [string range $split_line $begin_pos end]." 2

						if {$begin_pos > -1 && $end_pos > -1} {
							set column_value "[string range $split_line $begin_pos $end_pos]"
						}
						set ::runtime_arr($name,[expr $jj + 1]) $column_value
						if {$::DEBUG_LEVEL >= 3} {
							set msg_text "$msg_text\n\tsetting variable '$name,[expr $jj + 1]' using range$msg_text2 : >$column_value<"  
						}
					}
					regex {
						output "Setting column $name by regexp" 2
						set regex_string [$the_node selectNodes string(regex)]

						regexp -line -- $regex_string $split_line column_value
						set ::runtime_arr($name,[expr $jj + 1]) $column_value
						if {$::DEBUG_LEVEL >= 3} {
							set msg_text "$msg_text\n\tsetting variable '$name,[expr $jj + 1]' using regexp '$regex_string': >$column_value<"  
						}
					}
					xpath {
						regsub -all "&" $output_buffer "&amp;" output_buffer
						set xmldoc2 [dom parse $output_buffer]
						set root2 [$xmldoc2 documentElement]
						foreach the_node $variable_nodes {
							set name [string toupper [$the_node selectNodes string(name)]]
							set path [$the_node selectNodes string(xpath)]
							set row [$root2 selectNodes $path datatype]
							output "type is $datatype" 1
							if {"$datatype" == "empty"} {
								puts "xpath path invalid or empty node"
							} else {
								set jj 1
								foreach node $row {
									switch -- $datatype {
										nodes {
											set column_value [$node asText]
											output "value is $column_value" 1
										}
										attrnodes {
											set column_value [lindex $node 1]
											output "value is $column_value" 1
										}
										default {
											output "unknown datatype $datatype" 1
										}
									}
									set ::runtime_arr($name,$jj) $column_value
									if {$::DEBUG_LEVEL >= 3} {
										set msg_text "$msg_text\n\tsetting variable '$name,$jj' using xpath '$path': >$column_value<"  
									}
									incr jj
								}
							}
						}
						$xmldoc2 delete
					}
				}
				unset -nocomplain del_split_line
			}	

		}
		if {$::DEBUG_LEVEL >= 3} {
			insert_audit $::STEP_ID "" $msg_text ""
		}
		$root delete
		$xmldoc delete
	}
}


#####################################################
proc get_ecosystem_registry {key} {
	set proc_name get_ecosystem_registry
	set return_string ""
	if {![info exists ::ECO_REG]} {
		set sql "select registry_xml from object_registry where object_id = '$::ECOSYSTEM_ID'"
		$::db_query $::CONN $sql
		set ::ECO_REG [lindex [$::db_fetch $::CONN] 0]
	}
	if {"$::ECO_REG" > ""} {
		set dataset_doc [dom parse $::ECO_REG]
		set dataset_root [$dataset_doc documentElement]
		set return_string [$dataset_root selectNodes string(/registry/$key)]
		set key_node [$dataset_root selectNodes /registry/$key]
		if {"$key_node" > ""} {
			set is_encrypted [$key_node getAttribute encrypt ""]
			if {"$is_encrypted" == "true"} {
				set return_string [decrypt_string $return_string $::SITE_KEY]
				lappend ::SENSITIVE $return_string
			}
		}
		$dataset_root delete
		$dataset_doc delete
	}
	return $return_string
}

proc set_ecosystem_registry {command} {
	set proc_name set_ecosystem_registry

	set sql "select registry_xml from object_registry where object_id = '$::ECOSYSTEM_ID'"
	$::db_query $::CONN $sql
	set registry_xml [lindex [$::db_fetch $::CONN] 0]
	if {"$registry_xml" == ""} {
		set dataset_doc [dom createDocument registry]
		set dataset_root [$dataset_doc documentElement]
		output "Creating ecosystem registry" 4
		set new_flag 1
	} else {
		set dataset_doc [dom parse $registry_xml]
		set dataset_root [$dataset_doc documentElement]
		set new_flag 0
	}	

	regsub -all "&" $command "&amp;" command
	set xmldoc [dom parse $command]
	set root [$xmldoc documentElement]
	set pairs [$root selectNodes pair]
	set output_buf "Added the following key, value pairs to the dataset"

	foreach pair $pairs {
		set key [string tolower [$pair selectNodes string(key)]]
		set value [$pair selectNodes string(value)]
		set output_buf "$output_buf\nkey $key, value $value"
		set node [$dataset_root selectNodes $key]
		if {"$node" == ""} {
			set node [$dataset_doc createElement $key]
			$node appendChild [$dataset_doc createTextNode $value]
			$dataset_root appendChild $node
		} else {
			if {[$node hasChildNodes]} {
				[$node firstChild] nodeValue $value
			} else {
				$node appendChild [$dataset_doc createTextNode $value]
			}
		}
			
	}
	$root delete
	$xmldoc delete

	set new_xml [$dataset_root asXML]
	if {"$new_xml" != ""} {
		if {$new_flag == 0} {
			set sql "update object_registry set registry_xml = '$new_xml' where object_id = '$::ECOSYSTEM_ID'"
			#output "Update using: >$sql<" 4
			$::db_exec $::CONN $sql
		} else {
			set sql "insert into object_registry (object_id, registry_xml) values ('$::ECOSYSTEM_ID','$new_xml')"
			#output "Insert using: >$sql<" 4
			$::db_exec $::CONN $sql
		}
	}
	$dataset_root delete
	$dataset_doc delete
	insert_audit $::STEP_ID  "dataset" "$output_buf" ""
}
proc set_dataset {command} {
	set proc_name set_dataset

	if {![info exists ::DATASET]} {
		create_dataset_xml
	}
	set dataset_root [$::DATASET documentElement]
	
	set subnode [$::DATASET createElement dataset]
	$dataset_root appendChild $subnode

	regsub -all "&" $command "&amp;" command
	set xmldoc [dom parse $command]
	set root [$xmldoc documentElement]
	set pairs [$root selectNodes pair]
	set output_buf "Added the following key, value pairs to the dataset"

	foreach pair $pairs {
		set key [string tolower [$pair selectNodes string(key)]]
		set value [$pair selectNodes string(value)]
		set output_buf "$output_buf\nkey $key, value $value"
		set node [$::DATASET createElement $key]
		$node appendChild [$::DATASET createTextNode $value]
		$subnode appendChild $node
	}
	$root delete
	$xmldoc delete
	insert_audit $::STEP_ID  "dataset" "$output_buf" ""
}
proc store_dataset {} {
	set proc_name store_dataset

	if {[info exists ::DATASET]} {
		set sql "delete from task_instance_dataset where task_instance = $::TASK_INSTANCE"
		$::db_exec $::CONN $sql
		regsub -all "(')" [$::DATASET asXML] "''" dataset
		set sql "insert into task_instance_dataset (task_instance,dataset) values ($::TASK_INSTANCE,'$dataset')"
		$::db_exec $::CONN $sql
		insert_audit $::STEP_ID "dataset" "Storing dataset:\n$dataset" ""
	}
}
proc create_dataset_xml {} {
	set proc_name create_dataset_xml

	set ::DATASET [dom createDocument task_data]
	set root [$::DATASET documentElement]
}
#####################################################

proc dos_cmd {command} {
	set proc_name dos_cmd

	get_xml_root $command
	set the_command [replace_variables_all [$::ROOT selectNodes string(command)]]
	del_xml_root

	regsub -all "&amp;" $the_command {\&} the_command
	regsub -all "&gt;" $the_command ">" the_command
	regsub -all "&lt;" $the_command "<" the_command
	
	set cmd1 [lindex $the_command 0]
	#set cmd2 [lrange $the_command 1 end]
	set cmd2 [string range $the_command [expr [string length $cmd1] +1] end]
	set cmd3 [auto_execok $cmd1]
	if {"$cmd3" == ""} {
		set cmd3 $cmd1
	}
	set exec_cmd "exec $cmd3 $cmd2"

	#set exec_cmd "exec cmd /c $the_command"
	#set exec_cmd "exec $the_command"
	output "The dos command is >$exec_cmd<" 1

	catch {eval $exec_cmd} output_buffer
	if {"$output_buffer" == "child process exited abnormally"} {
		output "received $output_buffer from the dos command, ignoring" 1
		set output_buffer [string map {"child process exited abnormally" ""} $output_buffer]
	}

	output "$output_buffer" 1
	insert_audit $::STEP_ID  "" "$cmd3 $cmd2\012$output_buffer" ""

	if {[lindex $::step_arr($::STEP_ID) 8] > 0} {
		process_buffer $output_buffer
	}
}
proc lookup_shared_cred {shared_cred} {
	set proc_name lookup_shared_cred

	set sql "select username, password from asset_credential where credential_name = '$shared_cred'"
	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	return $row
}
proc fix {the_string} {
	set proc_name fix
	regsub -all "&amp;" $the_string {\&} the_string
	regsub -all "&gt;" $the_string ">" the_string
	regsub -all "&lt;" $the_string "<" the_string
	return $the_string
}
proc winrm_cmd {command} {
	set proc_name winrm_cmd

	get_xml_root $command
	set the_command [replace_variables_all [$::ROOT selectNodes string(command)]]
	set timeout [replace_variables_all [$::ROOT selectNodes string(timeout)]]
	del_xml_root

	regsub -all "&amp;" $the_command {\&} the_command
	regsub -all "&gt;" $the_command ">" the_command
	regsub -all "&lt;" $the_command "<" the_command
	
	set address [lindex $the_command 0]
	set shared_cred [lindex $the_command 1]
	set command [lindex $the_command 2]
	if {"$timeout" == ""} {
		set timeout 120
	}

	output "The dos command is >$command<"
	package require tclwinrm
	if {[array names ::TWINRM_CONN "$address,$shared_cred"] == ""} {
		set user_pass [lookup_shared_cred $shared_cred]
		output "user pass is $user_pass"
		set ::TWMINRM_CONN($address,$shared_cred) [tclwinrm::connection new http $address 5985 [lindex $user_pass 0] [decrypt_string [lindex $user_pass 1] $::SITE_KEY]]
	}
	if {$::DEBUG_LEVEL > 2} {
		set debug 1
	} else {
		set debug 0
	}

	set output_buffer [$::TWMINRM_CONN($address,$shared_cred) cmd_line $command $timeout $debug]

	output "$output_buffer"
	insert_audit $::STEP_ID  "" "$command\012$output_buffer" ""

	if {[lindex $::step_arr($::STEP_ID) 8] > 0} {
		process_buffer $output_buffer
	}
}

proc win_cmd {command} {
	set proc_name win_cmd

	get_xml_root $command
	set conn_name [replace_variables_all [$::ROOT selectNodes string(conn_name)]]
	set type [replace_variables_all [$::ROOT selectNodes string(type)]]
	set sub_type [replace_variables_all [$::ROOT selectNodes string(command)]]
	set win_command [replace_variables_all [$::ROOT selectNodes string(parameter_0)]]
	set win_command_1 [replace_variables_all [$::ROOT selectNodes string(parameter_1)]]
	set win_command_2 [replace_variables_all [$::ROOT selectNodes string(parameter_2)]]
	del_xml_root

	output "Connection name $conn_name, $type, $win_command" 1
	if {"$type" == "Security" || "$type" == "Registry"} {
		set type "$type $sub_type"
	}
	if {[string match "Registry*" $type]} {
		set namespace default
	} elseif {"$type" == "WMI"} {
		set namespace [string tolower $win_command_1]
	} else {
		set namespace "_MAIN"
	}
	if {"[array names ::connection_arr $conn_name,handle]" > ""} {
		set namespace_list $::connection_arr($conn_name,namespaces)
		output "namespace list = $namespace_list" 1
		set system $::connection_arr($conn_name,system)
		if {[lsearch -nocase $namespace_list $namespace] == -1} {
			set ::connection_arr($conn_name,handle,$namespace) [connect_system $system windows $namespace]
			lappend ::connection_arr($conn_name,namespaces) $namespace
		}
		if {"$namespace" != "_MAIN"} {
			set spawn_id $::connection_arr($conn_name,handle,$namespace)
		}
	} else {
		error_out "The windows connection $conn_name has not been established. Check the connection name or the new_connection function" 2007
	}
	set output_buffer ""
	set column_names ""
	switch -- $type {
		"WMI" {
			output "Windows Function WMI namespace = $win_command_1 query = $win_command" 1
			set output_buffer [get_wmi_query $spawn_id $win_command_1 $win_command $win_command_2]
			output $output_buffer 1
			set column_names [lindex $output_buffer 0]			
			#set mark [expr [llength $first_row] / 2]
			#set column_names [lreverse [lrange $first_row 0 [expr $mark - 1]]]
			#set output_buffer "[list [lrange $first_row $mark end]] [lrange $output_buffer 1 end]"
			#set output_buffer [lrange $output_buffer 1 end]
			set output_buffer [lindex $output_buffer 1]
			#output "column names = >$column_names<"
			#output "buf = >$output_buffer<"
		}
		"Registry Get Keys" {
			output "Windows Function Registry GetKeys $win_command" 1
			set column_names "{Subkey Name}"
			set output_buffer [get_reg $spawn_id $win_command "" key]
		}
		"Registry Get Key Values" {
			output "Windows Function Registry GetKeyValues $win_command" 1
			set column_names "{Valuename} {Value}"
			set output_buffer [get_reg $spawn_id $win_command "" value]
		}
		"Registry Get Values" {
			output "Windows Function Registry GetValue $win_command $win_command_1" 1
			set column_names "{Value}"
			set output_buffer [get_reg $spawn_id $win_command $win_command_1 value]
		}
		"Security Get Password Policy" {
			output "Windows Function Security GetPasswordPolicy" 1
			set column_names "{Max Age} {Min Age} {Min Len} {Force Logoff} {History Len} {Lockout Duration} {Lockout Threshold} {Lockout Window}"
			set output_buffer [get_password_policy $::system_arr($system,address)]
		}
		"Security Get Audit Policy" {
			output "Windows Function Security AuditPolicy" 1
			set column_names "{System Events} {Logon Events} {Object Access} {Privilege Use} {Process Tracking} {Policy Change} {Account Management} {Directory Service Access} {Account Logon Events}"
			if {[catch {set output_buffer [get_audit_policy $::system_arr($system,address)]} errMsg]} {
				error_out "Windows Security Policy Error -> $errMsg [twapi::map_windows_error $errMsg]" 2008
			}
		}
		"Security Get Users" {
			output "Windows Function Security Get Users" 1
			set column_names "{Username}"
			set buffer [twapi::get_users -system $::system_arr($system,address)]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
		"Security Get User Properties" {
			output "Windows Function Security Get User Properties $win_command" 1
			set column_names "{Username} {FullName} {User ID} {SID} {Comment} {Status} {Accoutn Expires} {Password Expired} {Password Age} {Bad Password Count} {Last Logon} {Last Logoff} {Number of Logons} {Privilege Level} {Home Directory} {Script Path} {Profile}"
			set buffer [twapi::get_user_account_info $win_command -full_name -comment -home_dir -script_path -profile -password_age -password_expired -status -acct_expires -sid -num_logons -last_logon -last_logoff -user_id -bad_pw_count -name -priv -system $::system_arr($system,address)]
			foreach {name value} $buffer {
				set user_props($name) $value
			}
			unset buffer
			set output_buffer "{[list $user_props(-name) $user_props(-full_name) $user_props(-user_id) $user_props(-sid) $user_props(-comment) $user_props(-status) $user_props(-acct_expires) $user_props(-password_expired) $user_props(-password_age) $user_props(-bad_pw_count) $user_props(-last_logon) $user_props(-last_logoff) $user_props(-num_logons) $user_props(-priv) $user_props(-home_dir) $user_props(-script_path) $user_props(-profile)]}"
		}
		"Security Get User Rights" {
			output "Windows Function Security Get User Rights $win_command" 1
			set column_names "{Right}"
			set buffer [twapi::get_account_rights $win_command -system $::system_arr($system,address)]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
		"Security Get Local Groups" {
			output "Windows Function Security Get Local Groups" 1
			set column_names "{Groupname}"
			set buffer [twapi::get_local_groups -system $::system_arr($system,address)]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
		"Security Get User Groups" {
			output "Windows Function Security Get User Groups $win_command" 1
			set column_names "{Groupname}"
			set buffer [twapi::get_user_local_groups_recursive $win_command -system $::system_arr($system,address)]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
		"Security Get Group Members" {
			output "Windows Function Security Get Group Members $win_command" 1
			set column_names "{Username}"
			set buffer [twapi::get_local_group_members  $win_command -system $::system_arr($system,address)]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
		"Security Get Group Properties" {
			output "Windows Function Security Get Group Properties $win_command" 1
			set column_names "{Groupname} {SID} {Comment}"
			set buffer [twapi::get_local_group_info  $win_command -name -sid -comment -system $::system_arr($system,address)]
			foreach {name value} $buffer {
				set group_props($name) $value
			}
			unset buffer
			set output_buffer "{[list $group_props(-name) $group_props(-sid) $group_props(-comment)]}"
		}
		"Security Get Users with Right" {
			output "Windows Function Security Users with Right $win_command" 1
			set column_names "{Username}"
			set buffer [twapi::find_accounts_with_right $win_command -system $::system_arr($system,address) -name]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
		"Security Get Group Rights" {
			output "Windows Function Security Get Group Rights $win_command" 1
			set column_names "{Right}"
			set buffer [twapi::get_account_rights  $win_command -system $::system_arr($system,address)]
			foreach value $buffer {
				if {"$output_buffer" > ""} {
					append output_buffer "\012{{$value}}"
				} else {
					append output_buffer "{{$value}}"
				}
			}
			unset buffer
		}
	}
	regsub -all {{\\}} $output_buffer {{}} output_buffer
	#output ">>>$output_buffer<<<"
	insert_audit $::STEP_ID "" "$type $win_command $win_command_1\012\012$column_names\012\012$output_buffer" ""
	#output ">[lindex $::step_arr($::STEP_ID) 8]<"
	set doing_variables 0
	if {"[lindex $::step_arr($::STEP_ID) 8]" > ""} {
		set xmldoc [dom parse [lindex $::step_arr($::STEP_ID) 8]]
		set root [$xmldoc documentElement]
		set variable_nodes [$root selectNodes  {/variables/variable}]
		set doing_variables 1
		foreach the_node $variable_nodes {
			set name [string toupper [$the_node selectNodes string(name)]]
			array unset ::runtime_arr $name,*
		}
	}
	output "Number of rows to process [llength $output_buffer]" 1
	output "Output buffer is $output_buffer" 2
	for {set row_num 0} {$row_num < [llength $output_buffer]} {incr row_num} {
		set row [lindex $output_buffer $row_num]
		output "Row # $row_num -> $row" 2
		if {$doing_variables == 1} {
			foreach the_node $variable_nodes {
				### 2010-01-19 - PMD - variable names case insensitive
				set name [string toupper [$the_node selectNodes string(name)]]
				set column_value ""
				set position [expr [$the_node selectNodes string(position)] - 1]
				set ::runtime_arr($name,[expr $row_num + 1]) [lindex $row $position]
				output "setting $name,[expr $row_num +1] = [lindex $row $position]" 2
			}
		}
	}
	if {$doing_variables == 1} {
		$xmldoc delete
	}
}
proc this_sleep {command} {
	set proc_name this_sleep
	get_xml_root $command
	set seconds [replace_variables_all [$::ROOT selectNodes string(seconds)]]
	del_xml_root

	if {"$seconds" == "" || [string is integer $seconds] != 1} {
		error_out "Sleep seconds parameter must be an integer" 9999
	} 
	insert_audit $::STEP_ID  "" "Sleeping {$seconds} seconds..." ""
	sleep "$seconds"
}
proc log_msg {command} {
	set proc_name log_msg

	if {$::AUDIT_TRAIL_ON == 0} {
		set ::AUDIT_TRAIL_ON 1
	}
	get_xml_root $command
	set message [replace_variables_all [$::ROOT selectNodes string(message)]]
	del_xml_root
	insert_audit $::STEP_ID  "" $message ""
}

proc new_connection {connection_system conn_name conn_type} {
	set proc_name new_connection
	set db_type_flag ""
	set asset_name $connection_system
	
	if {"$conn_type" != "ssh - ec2"} {
		if {[is_guid $connection_system] && ![info exists ::system_arr($connection_system,name)]} {
			output "getting a new system's info" 1
			gather_system_info $connection_system
		} elseif {![is_guid $connection_system]} { 
			set sql "select asset_id from asset where asset_name = '$connection_system'"
			#output $sql
			$::db_query $::CONN $sql
			set connection_system_2 [$::db_fetch $::CONN]
			if {"$connection_system_2" > ""} {
				gather_system_info $connection_system_2
			} else {
				error_out "New Connection error:\nThe asset ($connection_system) is not a valid Asset defined in the database. The asset definition must exist before a new connection can be established." 2010
			}
			set connection_system $connection_system_2
			unset connection_system_2
		}
	} else {	
		set user_id ""
		if {[string match "*@*" $connection_system]} {
			set user_id [lindex [split $connection_system @] 0]
			set connection_system [lindex [split $connection_system @] 1]
		}
		if {"$user_id" == ""} {
			error_out "The user id value is required for a connection type of ssh - ec2, example: root@$connection_system" 9999
		}
		for {set ii 0} {$ii < 20} {incr ii} {
			sleep 1
			set state [gather_aws_system_info $connection_system $user_id $::runtime_arr(_AWS_REGION,1)]
			if {"$state" == "running"} {
				break
			} elseif {"$state" == "pending"} {
				sleep 10
			} else {
				error_out "The instance $connection_system is not in a running or pending state. Current state is $state. Cannot connect" 9999
			}
		}
		if {"$state" == "pending"} {
			error_out "The instance $connection_system has been stuck in a pending state for 220 seconds. Check the status of the instance" 9999
		}
		register_security_group $::system_arr($connection_system,security_group) 22 $::runtime_arr(_AWS_REGION,1)
	}	
	insert_audit $::STEP_ID "" "Connecting to ($asset_name)... " "$connection_system"

	if {"$conn_type" == "windows"} {
		package require ac_win_api
		set namespace _MAIN
		set ::connection_arr($conn_name,handle) [connect_system $connection_system $conn_type $namespace]
		lappend ::connection_arr($conn_name,namespaces) _MAIN
	} else {
		set ::connection_arr($conn_name,handle) [connect_system $connection_system $conn_type ""]
	}

	set ::connection_arr($conn_name,system) $connection_system
	set ::connection_arr($conn_name,conn_type) $conn_type
	output "New Connection Type = {$conn_type}, connection name {$conn_name}, connection asset id {$connection_system}." 1
	if {"$db_type_flag" == "O"} {
		set ::connection_arr($conn_name,ora_handle) [oraopen $::connection_arr($conn_name,handle)]
		oraconfig $::connection_arr($conn_name,ora_handle) fetchrows 10000
	}
	unset -nocomplain db_type_flag conn_name connection_system conn_type 
}

proc transfer {command} {
	set proc_name transfer

	get_xml_root $command
	set from_asset [$::ROOT selectNodes string(from_asset)]
	set from_file [$::ROOT selectNodes string(from_file)]
	set to_asset [$::ROOT selectNodes string(to_asset)]
	set to_file [$::ROOT selectNodes string(to_file)]
	set mode [$::ROOT selectNodes string(mode)]
	set cmd [$::ROOT selectNodes string(command)]
	del_xml_root
	set found_flag 0
	set conn_name $from_asset,$to_asset,transfer,$mode
	output "Conn name is $conn_name\nand conn names are [array names ::connection_arr]" 1
	if {"[array names ::connection_arr $conn_name,handle]" > ""} {
		output "connection by that name already exists. Using it." 1
		set found_flag 1
	} else {
		output "New transfer connection" 1
	}
	if {$found_flag == 0} {
		new_connection $from_asset $conn_name ssh
	}
	if {[info exists ::connection_arr($conn_name,handle)]} {
		set conn_id $::connection_arr($conn_name,handle)
		set system $::connection_arr($conn_name,system)
	} else {
		error_out "The telnet or ssh connection $conn_name has not been established. Check the connection name or the new_connection function" 2013
	}
	if {![is_guid $to_asset]} { 
		set sql "select asset_id from asset where asset_name = '$to_asset'"
		#output $sql
		$::db_query $::CONN $sql
		set to_asset_2 [$::db_fetch $::CONN]
		if {"$to_asset_2" == ""} {
			error_out "$mode Connection error:\nThe asset ($to_asset) is not a valid Asset defined in the database. The asset definition must exist before a new connection can be established." 2010
		}
		set to_asset $to_asset_2
	}
	gather_system_info $to_asset
	set to_address $::system_arr($to_asset,address) 
	set to_userid $::system_arr($to_asset,userid) 
	set to_password $::system_arr($to_asset,password) 
	output "transfer mode is $mode" 1
	switch -- $mode {
		SCP {
			#exp_send -s -- "scp user\r"
		}
		SFTP {
			if {$found_flag == 0} {
				output "Connecting to $to_address using $to_userid using sftp" 1
				sftp_logon $conn_id $to_address $to_userid $to_password
				insert_audit $::STEP_ID  "" "sftp connection to $to_address with user $to_userid established." ""
			}
			transfer_file $conn_id $from_file $to_file sftp $cmd
		}
		FTP {
			if {$found_flag == 0} {
				output "Connecting to $to_address using $to_userid using ftp" 1
				ftp_logon $to_address $to_userid $to_password
				insert_audit $::STEP_ID  "" "ftp connection to $to_address with user $to_userid established." ""
			}
			transfer_file $conn_id $from_file $to_file ftp $cmd
		}
		RCP {
		}
	}
}

proc transfer_file {conn_id from_file to_file ftp_type cmd} {
	set proc_name send_file
	set timeout 600
	set send_slow {3 .0000001}
	set spawn_id $conn_id


	output "Starting transfer of file $from_file" 1
	set timeout 600

	### PMD - 2007-10-02 - bug 340 - in case path has pound signs escape them
	#set path [string map {# \\#} $from_path]
	#if {"$ftp_type" == "sftp"} {
		#exp_send "put $from_path/$from_file\r"
	#} else {
		exp_send "$cmd $from_file $to_file\r"
	#}
	expect {
		-glob {Permission denied*ftp>} {
			set error_msg "Transfer of file $from_file failed. Permission denied, check read permissions on remote file. Please check logs\n$expect_out(buffer)"
			error_out $error_msg "File Transfer Error"
		}
		-glob {No such file or directory*ftp>} {
			set error_msg "Transfer of file $from_file failed.  No such file or directory. Please check logs\n$expect_out(buffer)" 
			error_out $error_msg "File Transfer Error"
		}
		"ftp>" {
			insert_audit $::STEP_ID  "" "$expect_out(buffer)\nTransfer complete." ""
		}
		timeout {
			set error_msg "Transfer of file $from_file failed. Transfer exceeded 10 minute threshold, timed out. Please check logs\n$expect_out(buffer)" 
			error_out $error_msg 9999
		}
	}
}
proc end_task {command} {
	set proc_name end_task
	get_xml_root $command
	set message [$::ROOT selectNodes string(message)]
	set status [$::ROOT selectNodes string(status)]
	del_xml_root
	insert_audit $::STEP_ID  "" "Ending task with a status of {$status}, message:\n$message" ""
	if {"$status" == "Error"} {
		error_out "Erroring task with message:\n$message" 9999
	}
	release_all
	update_status $status
	exit
}
proc get_xml_root {xml_doc} {
	set proc_name get_xml_root
	regsub -all "&" $xml_doc "&amp;" xml_doc
	set ::XMLDOC [dom parse $xml_doc]
	set ::ROOT [$::XMLDOC documentElement]
}
proc del_xml_root {} {
	set proc_name del_xml_root
	$::ROOT delete
	$::XMLDOC delete
	unset ::ROOT ::XMLDOC
}
proc http_command {command} {
	set proc_name http_command

	package require http
	package require tls
	::http::register https 443 ::tls::socket

	get_xml_root $command
	set url [replace_variables_all [$::ROOT selectNodes string(url)]]
	set type [$::ROOT selectNodes string(type)]
	output "http command of type $type, url $url" 1

	set query ""
	switch -- $type {
		"GET" {
			catch {set token [::http::geturl $url -timeout [expr 10 * 1000]]} error_code
		}
		"POST" {
			set pairs [$::ROOT selectNodes  {//pair}]
			if {[info exists pairs]} {
				set post_string ""
				foreach the_node $pairs {
					set key [string trim [replace_variables_all [$the_node selectNodes string(key)]]]
					set value [replace_variables_all [$the_node selectNodes string(value)]]
					set query  "$query&[::http::formatQuery $key]=[::http::formatQuery $value]"
				}
			}
			if {"$query" > ""} {
				set query [string range $query 1 end]
			}
			catch {set token [::http::geturl $url -timeout [expr 60 * 1000] -query $query]} error_code
		}
		#"HEAD" {
		#}
	}
	del_xml_root

	if {[string match "::http::*" $error_code] == 0} {
		set output_buffer $error_code
		output "http $type error: $url\012$error_code" 1
	} else {
		if {"[::http::status $token]" != "ok" || [::http::ncode $token] != 200} {
			set output_buffer "http $type error: $url\012[::http::status $token] [::http::code $token] [::http::data $token]"
			error_out $output_buffer 2011
		
		} else {
			set output_buffer [::http::data $token]
			output $output_buffer 1
		}
		
	}
	if {[info exists token] == 1} {
		::http::cleanup $token 
		#unset token
	}
	if {[lindex $::step_arr($::STEP_ID) 8] > 0} {
		process_buffer $output_buffer
	}
	insert_audit $::STEP_ID  "" "http $type $url\012$query\012$output_buffer" ""
}
proc new_connection_command {command} {
	set proc_name new_connection_command

	#output "Creating a new connection..."
	get_xml_root $command
	set conn_type [$::ROOT selectNodes string(conn_type)]
	set conn_name [replace_variables_all [$::ROOT selectNodes string(conn_name)]]
	set connection_system [replace_variables_all [$::ROOT selectNodes string(asset)]]
	del_xml_root
	#output "$conn_type, $conn_name, $connection_system"

	set found_flag 0
	if {"[array names ::connection_arr $conn_name,handle*]" > ""} {
		insert_audit $::STEP_ID "" "A connection by the name $conn_name already exists, closing the previous one and openning a new connection" ""
		release_connection $conn_name
	}
	new_connection $connection_system $conn_name $conn_type
	insert_audit $::STEP_ID  "" "New connection named $conn_name to asset $connection_system created with a connection type $conn_type" ""
}
#####################################################
#
#	procedure: run_commands
#
#	Runs the step commands against the system
#
#####################################################

proc run_commands {task_name codeblock} {
	set proc_name run_commands

	#output "Entering $proc_name" 3

#	global SYSTEMS
#	global spawn_id
#	global TASK_NAME
#	global TASK_INSTANCE
#	global DB_NAME
#	global AUDIT_TRAIL_ON
#	global TRAP_SQL_ERROR
	#global FILTER_BUFFER
#	global SUBMITTED_BY
#	global TIMEOUT_VALUE
#	global spawn_id_arr
#	global connection_arr
#	global exp_internal
#	global response_arr
#	global runtime_arr
#	global at_conn_arr


#	set send_slow {3 .00001}
#	set subtasklist ""

#	output "The step array size is $arr_size" 1
#	output "The step ids are [array names step_arr_local]"
#	output "The response array size is $arr_size_r" 1
#	output "The system variable array size is $arr_size_s" 1
	
	
	###
	### Let's start going through the array of steps
	###
	output "##### Processing Codeblock {$codeblock}" 1
	
	if {![info exists ::codeblock_arr($task_name,$codeblock)]} {
		insert_audit 0 "" "Codeblock {$codeblock} does not have any steps, returning..." ""
		return
	}

	foreach codeblock_step $::codeblock_arr($task_name,$codeblock) {
		process_step $codeblock_step $task_name
		if {$::BREAK == 1 && $::INLOOP == 1} {
			break
		}

#		set compare_to 1
#		set increment 1
#		set initial 0
#		set loop_test "<"
#		set loop_var "loop_count"
#		set loop_var2 "\$loop_count"

		#set ::STEP_LOG ""
#		for {set $loop_var $initial} {[expr $loop_var2 $loop_test $compare_to]} {incr $loop_var $increment} {
#		}
	}

	#output "Exiting $proc_name" 2
}
proc process_step {step_id task_name} {
	set proc_name process_step
	set ::STEP_ID [string tolower $step_id]
	
	output "**************************************************************"
	output "**** PROCESSING STEP $::STEP_ID" 
	output "**************************************************************"



	
	set function_name [lindex $::step_arr($::STEP_ID) 3]
	set command [lindex $::step_arr($::STEP_ID) 4]
	output "Command is {$function_name}" 1
	output "Full Commmand: {$command}" 6
	#set_logger "Command is $function_name" 2

	switch -glob -- $function_name {
		"while" {
		}
		"loop" {
		}
		"if" {
			set command [replace_variables_all $command]
			regsub -all "&" $command "&amp;" command
			set returned_step_id [if_function $command]

			if {"$returned_step_id" > ""} {
				set ::STEP_ID [string tolower $returned_step_id]
				set function_name [lindex $::step_arr($::STEP_ID) 3]
				set command [replace_variables_all [lindex $::step_arr($::STEP_ID) 4]]
				output "'IF' Action Command is {$function_name}" 1
				output "'IF' Action Full Commmand: {$command}" 6
			}
		}
		http_command {}
		log_msg {}
		set_variable {}
		run_task {}
		new_connection {}
		sql_exec {}
		win_cmd {}
		dos_cmd {}
		aws_get_instance {}
		store_private_key {}
		get_ecosystem_objects {}
		default {
			set command [replace_variables_all $command]
		}
	}
	###
	### Now let's eval the command type. If it's a function, let's
	### perform the function and continue on to the next step
	###

	switch -glob -- $function_name {
	       "aws_*" {
		      set aws_split [split $function_name "_"]
		      aws_Generic [lindex $aws_split 1] [lindex $aws_split 2] {} $command
	       }
		"store_private_key" {
			store_private_key $command
		}
		"get_ecosystem_objects" {
			get_ecosystem_objects $command
		}
		"set_variable" {
			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set variable_nodes [$root selectNodes  {//variable}]
			foreach the_node $variable_nodes {
				set variable_name [replace_variables_all [$the_node selectNodes string(name)]]
				set modifier [$the_node selectNodes string(modifier)]
				set value [replace_variables_all [$the_node selectNodes string(value)]]
				switch -exact -- $modifier {
					TO_UPPER {
						set value [string toupper $value]
					}
					TO_LOWER {
						set value [string tolower $value]
					}
					TO_BASE64 {
						package require base64
						set value [::base64::encode -wrapchar "" [encoding convertto unicode $value]]
					}
					FROM_BASE64 {
						package require base64
						set value [::base64::decode $value]
					}
					default {
					}
				}
				
				output "$variable_name, $value"
				set_variable $variable_name $value
			}
			$root delete
			$xmldoc delete

			unset variable_name value
		}
		"scriptlet" {
		}
		"transfer" {
			transfer $command
		}
		"read_file" {
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set filename [$root selectNodes string(filename)]
			set start [$root selectNodes string(start)]
			set num_chars [$root selectNodes string(num_chars)]
			$root delete
			$xmldoc delete
			if {[catch {set fp [open $filename]} err_msg]} {
				error_out "File read error:\012$err_msg" 2204
			}
			if {"$start" > ""} {
				incr $start -1
			} else {
				set start 0
			}	
			seek $fp $start
			if {"$num_chars" > ""} {
				set output_buffer [read $fp $num_chars]
			} else {
				set output_buffer [read $fp]
			}
			close $fp
			insert_audit $::STEP_ID  "" "read file $output_buffer" ""
			if {[lindex $::step_arr($::STEP_ID) 8] > 0} {
				process_buffer $output_buffer
			}
		}
		"win_cmd" {
			win_cmd $command
		}
		"sql_exec" {
			sql_exec $command
		}
		"cancel_task" {
			cancel_tasks $command
		}
		"if" {
		}
		"get_date" {
			output "we have a get_date" 1
			### PMD - fix for get_date looking for epoch seconds, for HPUX - 2006-01-28
			if {"%s" == "[lrange $command 2 end]"} {
				set_variable [lindex $command 1] [clock seconds]
			} else {
				set_variable [lindex $command 1] [clock format [clock seconds] -format "[lrange $command 2 end]"]
			}
			### end of fix
		}
		"run_task" {
			launch_run_task
		}
		"wait_for_tasks" {
			wait_for_tasks
		}
		"length" {
			set_variable [lindex $command 1] [string length [string range $command [string length "length [lindex $command 1] "] end]]
		}
		"substring" {
			
			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set source [$root selectNodes string(source)]
			set variable_name [$root selectNodes string(variable_name)]
			set start [$root selectNodes string(start)]
			set end [$root selectNodes string(end)]
			$root delete
			$xmldoc delete
			if {"$variable_name" == "" || "$start" == "" || "$end" == ""} {
				error_out "Substring error: Missing required data - variable, start index and end index are all required fields" 2208
			}
			if {[string is integer $start] == 0} {
				error_out "Substring error: start index must be integer" 2209
			}
			incr start -1
			if {[string is integer $end] == 0 && "[string index $end 0]" != "+" && "[string range $end 0 2]" != "end"} {
				error_out "Substring error: end index must be integer, +integer, end or end+-integer" 2210
			}
			if {[string is integer $end] == 1} {
				incr end -1
			}
			set set_string [string range $source $start $end]
			set_variable $variable_name $set_string
			if {$::DEBUG_LEVEL >= 3} {
				insert_audit $::STEP_ID "" "Substring set variable {$variable_name} to {$set_string}." ""
			}
			unset variable_name set_string start end source
		}
		"drop_connection" {

			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set conn_name [$root selectNodes string(conn_name)]
			$root delete
			$xmldoc delete
			output "Dropping connection named $conn_name" 1
			if {[info exists ::connection_arr($conn_name,handle)]} {
				release_connection $conn_name
			}
			unset conn_name
		}
		"end" {
			end_task $command
		}
		"new_connection" {
			new_connection_command $command
		}
		"send_email" {

			send_email_2 $command

		}
		"dataset" {
			set_ecosystem_registry $command
		}
		"get_instance_handle" {
			get_instance_handle $command
		}
		"log_msg" {
			log_msg $command
		}
		"http" {
			http_command $command
		}
		"parse_text" {
			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set output_buffer [$root selectNodes string(text)]
			insert_audit $::STEP_ID  "" "Parsing text: {$output_buffer}" ""
			if {[lindex $::step_arr($::STEP_ID) 8] > 0} {
				process_buffer $output_buffer
			}
			$root delete
			$xmldoc delete
		}
		"clear_variable" {
			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set variable_nodes [$root selectNodes  {//variable}]
			set var_set ""
			foreach the_node $variable_nodes {
				set variable_name [replace_variables_all [$the_node selectNodes string(name)]]
				### 2010-01-19 - PMD - variable names case insensitive
				set variable_name [string toupper $variable_name]
				output "unsetting $variable_name" 1
				array unset ::runtime_arr $variable_name,*
				lappend var_set $variable_name
			}
			$root delete
			$xmldoc delete
			insert_audit $::STEP_ID  "" "Cleared variables: {$var_set}." ""
		}
		"sleep" {
			this_sleep $command
		}
				 "set_debug_level" {
					output "script function is set_debug_level" 2
                    regsub -all "&" $command "&amp;" command
					set xmldoc [dom parse $command]
					set root [$xmldoc documentElement]
					set debug_level [$root selectNodes string(debug_level)]
					$root delete
					$xmldoc delete
                    output "setting debug_level to $debug_level" 1

					#global ::DEBUG_LEVEL
                    set ::DEBUG_LEVEL $debug_level
                    if {$::DEBUG_LEVEL == -2} {
                            log_user 0
                    } else {
                            log_user 1
                    }
                    continue
                }
		"break_loop" {
					output "Breaking out of loop" 1
			insert_audit $::STEP_ID  "" "Breaking out of loop." ""
			if {$::INLOOP == 1} {
				set ::BREAK 1
			}
		}
		"while" {
			while_loop $command $task_name
		}
		"loop" {
			for_loop $command $task_name
		}
		"codeblock" {

			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set new_codeblock [string toupper [$root selectNodes string(codeblock)]]
			if {"$new_codeblock" == ""} {
				error_out "Codeblock name empty, value is required." 2017
			}
			set step_id $::STEP_ID
			run_commands $task_name $new_codeblock
			set ::STEP_ID [string tolower $step_id]

			unset new_codeblock

		}
		"subtask" {
			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set orig_subtask_id [$root selectNodes string(original_task_id)]
			set subtask_version [$root selectNodes string(version)]
			if {"$orig_subtask_id" == ""} {
				error_out "Subtask name empty, value is required." 2018
			}
			if {"$subtask_version" > ""} {
				set sql "select task_id from task where original_task_id = '$orig_subtask_id' and version = '$subtask_version'"
			} else {
				set sql "select task_id from task where original_task_id = '$orig_subtask_id' and default_version = 1"
			}
			$::db_query $::CONN $sql
			set subtask_id [$::db_fetch $::CONN]

			if {[llength [array names ::codeblock_arr $subtask_id,*]] == 0} {

				output "New subtask" 1
				get_steps $subtask_id
			
			}

			#### PMD 2006-03-30 Fix for input variables to subtasks
			#array unset prompt_arr $task_name_1,*
			#### End of fix
			#foreach param_default [lrange $command 2 end] {
			#	
			#	set param_name [lindex [split $param_default "="] 0]
			#	set param_value [lindex [split $param_default "="] 1]
			#	global prompt_arr
			#	set prompt_arr($task_name_1,$param_name,1) "$param_value"
			#	output "setting subtask input var $param_name to be $param_value" 1
			#}
			if {[llength [array names ::codeblock_arr $subtask_id,*]] > 0} {
						output "subtask_id is $subtask_id, version is $subtask_version" 1
				set step_id $::STEP_ID
				run_commands $subtask_id MAIN
					set ::STEP_ID [string tolower $step_id]
			} else {
				error_out "Subtask not found or no steps exist." 2012
			}

			unset subtask_id subtask_version

		}
		"dos_cmd" {
			winrm_cmd $command
		}
		"cmd_line" {
			regsub -all "&" $command "&amp;" command
			set xmldoc [dom parse $command]
			set root [$xmldoc documentElement]
			set conn_name [$root selectNodes string(conn_name)]
			set cmd_timeout [$root selectNodes string(timeout)]
			set command [fix [$root selectNodes string(command)]]
			set positive_response [fix [$root selectNodes string(positive_response)]]
			set negative_response [fix [$root selectNodes string(negative_response)]]
			$root delete
			$xmldoc delete

			#regsub -all "&amp;" $command {\&} command
			#regsub -all "&gt;" $command ">" command
			#regsub -all "&lt;" $command "<" command

			if {"$function_name" == "dos_cmd"} {
				set conn_name DOS
				if {[info exists ::connection_arr(DOS,handle)]} {
					set spawn_id $::connection_arr(DOS,handle)
				} else {
					
					set ::connection_arr(DOS,handle) [connect_dos]
					set ::connection_arr(DOS,conn_type) DOS
					set ::connection_arr(DOS,system) ""
					set spawn_id $::connection_arr(DOS,handle)
				}
			} else {
				if {[info exists ::connection_arr($conn_name,handle)]} {
					set spawn_id $::connection_arr($conn_name,handle)
					set system $::connection_arr($conn_name,system))
				} else {
					error_out "The telnet or ssh connection {$conn_name} has not been established. Check the connection name or the new_connection function." 2013
				}
			}
			output "$conn_name, $cmd_timeout, $command, $positive_response, $negative_response" 4

	
			###
			### Let's setup the expected responses
			###

			if {"$positive_response" == ""} {
				set positive_response "PROMPT>|ftp> "
			} else {
				set positive_response [replace_variables_all $positive_response]
			}
			if {"$negative_response" == ""} {
				set negative_response "This is a default response you shouldnt get it"
			} else {
				set negative_response [replace_variables_all $negative_response]
			}



			###
			### Now we're ready to send the commands to the spawned process
			###

			output "Sending $command to the process" 0
	
			set send_slow {3 .0000001}
			set MATCH_MAX 10485760
			if {[catch {match_max $MATCH_MAX} return_code]} {
				error_out "Communication channel not open to connection." 2014
			}

			if {"$cmd_timeout" > ""} {
				set timeout $cmd_timeout
			} else {
				set timeout $::TIMEOUT_VALUE
			}
			set output_buffer ""

			exp_send -s -- "$command\r"
			#trace variable xxx w filter_buffer
			set timed_out_flag 0
			while 1 {
				expect {
					-re "$negative_response" {
								output "**ERROR**" 1
						#if {$FILTER_BUFFER == 0} {
							set output_buffer $expect_out(buffer)
						#} else {
						#	set xxx $expect_out(buffer)
						#	set output_buffer $xxx
						#	unset xxx
						#}
						release_all
						error_out "Negative response condition met: $output_buffer" 2015
					}
					-re "$positive_response" {
						output "**OK**" 1
						if {[string length $output_buffer] == 0} {
							set prompt_len [string length $expect_out(0,string)]
						}
						break
					}
					full_buffer {
						output "Reseting buffer" 1
						if {[string length $output_buffer] == 0} {
							set prompt_len [string length $expect_out(0,string)]
						}
						set output_buffer $output_buffer$expect_out(buffer)
					}
					timeout {
						#global TIMEOUT_CODEBLOCK
						#if {$TIMEOUT_CODEBLOCK == ""} {
									output "**TIMED OUT**" 1
							if {[info exists expect_out(buffer)]} {
								set output_buffer $output_buffer$expect_out(buffer)
							} else {
								expect *
								set output_buffer $output_buffer$expect_out(buffer)
							} 
							release_all
							insert_audit $::STEP_ID  "$command" "TIMEOUT on command:\012$command\012$output_buffer" $conn_name
							update_status Error
							set error_msg "TIMEOUT while performing the command: $command\012$output_buffer\012 for step id $::STEP_ID"
							error_out $error_msg 2016
						#} else {
						#	set output_buffer $output_buffer$expect_out(buffer)
						#	output "**TIMED OUT**, calling codeblock $TIMEOUT_CODEBLOCK"
						#	insert_audit $::STEP_ID  "$command" "TIMEOUT on command:\012$output_buffer... calling codeblock {$TIMEOUT_CODEBLOCK}." $conn_name
						#	#set command $TIMEOUT_CODEBLOCK
						#	set timed_out_flag 1
						#	break
						#}
					}
				}
			}
			#if {$timed_out_flag == 1} {
			#	set code_block $TIMEOUT_CODEBLOCK
			#	if {[lsearch -exact $BLOCKLIST "$task_name+_+$code_block"] == -1} {
			#		output "The NEW codeblock is -> $code_block" 1
			#		get_codeblock_steps task_name step_arr_1 $code_block $system $task_version
			#		global step_arr_$task_name+_+$code_block
			#		array set step_arr_$task_name+_+$code_block [array get step_arr_1]
			#		lappend BLOCKLIST "$task_name+_+$code_block"
			#	} else {
			#		output "The OLD codeblock is -> $code_block" 1
			#		global step_arr_$task_name+_+$code_block
			#		array set step_arr_1 [array get step_arr_$task_name+_+$code_block]
			#	}
			#	run_commands step_arr_1 $num_systems $system $task_name $task_version
			#	unset step_arr_1
			#}
			set output_buffer [string map {PROMPT2> ""} $output_buffer$expect_out(buffer)]
			set prompt_len [string length $expect_out(0,string)]
			if {[string length $output_buffer] == 0} {
				set prompt_len [string length $expect_out(0,string)]
			}
			if {[info exists  expect_out]} {
				unset expect_out
			}

			#output "The length of the buffer is [string length $output_buffer]" 1
			#output "The length of the command is [string length '$command']" 1
			#output "The first carriage return is at index [string first "\012" $output_buffer]" 1
			#output "Stripping [string length '$command'] characters off the front of the buffer" 1
			#output "Stripping  [string first "\012" $output_buffer] characters off the front of the buffer" 1
			#output "The prompt length is $prompt_len" 1
			#output "Stripping [expr $prompt_len + 1] characters off the end of the buffer (including carriage return)" 1
			output "The buffer is ->$output_buffer<-" 1

			## put the command in the special log column for user-friendly debugging
			## shows the command after replacement, but not piled in the log field with the results
			insert_audit $::STEP_ID "$command" "$command\012$output_buffer" $conn_name
			##OLD ->insert_audit $::STEP_ID "$command" "$command\012$output_buffer" $conn_name
			
			set output_buffer [string range $output_buffer 0 [expr [string length $output_buffer] - $prompt_len - 2]]
			while {[string first "  " $output_buffer] > -1} {
				regsub -all "(  )" $output_buffer " " output_buffer
			}
			#output "The buffer after removing multiple spaces, commands, and prompts is: $output_buffer" 0
			output "The buffer is ->$output_buffer<-" 1
	
			if {[lindex $::step_arr($::STEP_ID) 8] > 0} {
				process_buffer $output_buffer
			}
			unset conn_name
		}
		default {
			error_out "The command $function_name is not a valid command" 3000
		}
	}
	
	unset -nocomplain output_buffer
	#output [info vars]
}
##################################################
#	end of run_commands
##################################################

#####################################################
#
#	procedure: connect_system
#
#	This proc performs the meat of the task logic
#
#####################################################

proc connect_system {system conn_type namespace} {

	set proc_name connect_system

	set spawn_id ""

	###
	### determine the number of systems to login to
	###
	global NUM_OPEN_CONN
	if {$NUM_OPEN_CONN > 19} {
	
		#error_out "Maximum number of open connections exceeded: $NUM_OPEN_CONN." 3001
		output "Warning maximum number of open connections exceeded: $NUM_OPEN_CONN, continuing." 0
	} else {
		incr NUM_OPEN_CONN
	}

	output  "Going into system $::system_arr($system,address) userid $::system_arr($system,userid) with conn type of $conn_type" 1

	switch -exact -- $conn_type {
		windows {
			#package require agenttcl
			set spawn_id [connect_windows $::system_arr($system,address) $namespace $::system_arr($system,userid) $::system_arr($system,password) $::system_arr($system,domain)]
		}
		telnet {
			set timeout_flag [telnet_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password) yes telnet 1 ""]
			if {$timeout_flag > 0} {
				set timeout_flag [telnet_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password) yes telnet 2 ""]
			}
		}
		"ssh - ec2" {
                        if {"$::system_arr($system,private_key)" == ""} {
                                error_out "The private key \"$::system_arr($system,private_key_name)\" was not found. Add the private key for key name \"$::system_arr($system,private_key_name)\" to the cloud account \"$::CLOUD_NAME\"" 3000
                        }
			for {set ii 1} {$ii < 11} {incr ii} {
				if {$ii == 10} {
					set flag 2
				} else {
					set flag 1
				}
				set timeout_flag [telnet_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password) yes ssh $flag $::system_arr($system,private_key)]
				if {"$timeout_flag" != "1"} {
					break
				}
				sleep 10
			}
		}
		ssh {
			set timeout_flag [telnet_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password) yes ssh 1 $::system_arr($system,private_key)]
			if {$timeout_flag > 0} {
				sleep 20
				set timeout_flag [telnet_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password) yes ssh 2 $::system_arr($system,private_key)]
			}
		}
		sybase {
			set spawn_id [sybase_logon  sqlserver_logon  $::system_arr($system,userid) $::system_arr($system,password) $::system_arr($system,address) $::system_arr($system,port) $::system_arr($system,db_name)]
			upvar db_type_flag db_type_flag
			set db_type_flag "SYB"
		}
		"informix odbc" {
			set spawn_id [informix_logon  $::system_arr($system,userid) $::system_arr($system,password) $::system_arr($system,address) $::system_arr($system,port) $::system_arr($system,db_name)]
			upvar db_type_flag db_type_flag
			set db_type_flag "ODBC"
		}
		sqlserver {
			set spawn_id [sqlserver_logon  $::system_arr($system,userid) $::system_arr($system,password) $::system_arr($system,address) $::system_arr($system,port) $::system_arr($system,db_name)]
			upvar db_type_flag db_type_flag
			set db_type_flag "S"
		}
		oracle {
			set spawn_id [oracle_logon  $::system_arr($system,userid) $::system_arr($system,password) $::system_arr($system,db_name) $::system_arr($system,address) $::system_arr($system,port) $::system_arr($system,conn_string)]
			upvar db_type_flag db_type_flag
			set db_type_flag "O"
		}
		sftp {
			sftp_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password)
		}
		ftp {
			ftp_logon $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,password)
		}
		http {
			set spawn_id $::system_arr($system,address)
		}
	}
	task_conn_log $::system_arr($system,address) $::system_arr($system,userid) $::system_arr($system,conn_type)
	return $spawn_id
}

##################################################
#	end of connect_system
##################################################

#####################################################
#
#	procedure: process_task
#
#	This proc performes the meat of the task logic
#
#####################################################

proc process_task {} {
	set proc_name process_task

	upvar system system

	###
	### Let's setup the task instance variables
	###


	global SYSTEM_NAME
	global SYSTEM_ID
	global TASK_NAME
	global TASK_VERSION
	global TASK_ID
	global TASK_INSTANCE
	global SUBMITTED_BY
	global SYSTEMS
	set ::TEST_RESULT ""
	set ::STEP_ID ""

	
	set sql "select A.task_instance, B.task_name, A.asset_id, 
             C.asset_name, A.submitted_by, 
             B.task_id, B.version, A.debug_level, A.schedule_instance,
			 A.ecosystem_id, A.account_id
             from tv_task_instance A 
		join task B on A.task_id = B.task_id
		left outer join asset C on A.asset_id = C.asset_id
             where  A.task_instance = $::TASK_INSTANCE"

	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	if {[string length $row] == 0} {
		error_out "Task instance number not found in the task instance table." 3002
	}
	set ::TASK_NAME [lindex $row 1]
	set ::SYSTEM_ID [lindex $row 2]
	set ::SYSTEM_NAME [lindex $row 3]
	set ::SUBMITTED_BY [lindex $row 4]
	set ::TASK_ID [lindex $row 5]
	set ::TASK_VERSION [lindex $row 6]
	set ::DEBUG_LEVEL [lindex $row 7]
	set ::SCHEDULE_INSTANCE [lindex $row 8]
	set ::ECOSYSTEM_ID [lindex $row 9]
	set ::CLOUD_ACCOUNT [lindex $row 10]

	output "Task Name $::TASK_NAME - Version $::TASK_VERSION (DEBUG LEVEL: $::DEBUG_LEVEL), Ecosystem id: $::ECOSYSTEM_ID" -99

	if {"$::CLOUD_ACCOUNT" > "" && "$::CLOUD_ACCOUNT" != "null"} {
		gather_account_info $::CLOUD_ACCOUNT
	}
	set ::ECOSYSTEM_NAME ""

	###
	### Get info on the system itself
	###
	if {[string length $::SYSTEM_ID] > 0 && [info exists ::system_arr($::SYSTEM_ID,name)] == 0} {
		gather_system_info $::SYSTEM_ID
	}

	get_steps $TASK_ID
	get_task_params

	run_commands $TASK_ID MAIN

	###
	### Logout of the systems
	###

	store_dataset

}
##################################################
#	end of process_task
##################################################

##################################################
#	procedure: release_connection
#
#	This proc will close all open connections
#
##################################################

proc release_connection {conn_name} {
	set proc_name release_connection

	output "releasing $conn_name" 1

	release_system $conn_name

	array unset ::connection_arr $conn_name,handle 
	array unset ::connection_arr $conn_name,system
	array unset ::connection_arr $conn_name,conn_type
	array unset ::connection_arr $conn_name,namespaces
	#parray ::connection_arr
}

##################################################
#	end of release_connection
##################################################

##################################################

##################################################
#	procedure: release_all
#
#	This proc will close all open connections
#
##################################################

proc release_all {} {
	set proc_name release_all
	output "Releasing all connections" 3
	foreach conn_name [array names ::connection_arr *,handle] {
		set conn_name [lindex [split $conn_name ,] 0]
		catch {release_connection $conn_name} err
		if {"$err" > ""} {
			output "Disconnecting from $conn_name errored out ->$err<-" 0
			output "Continuing..."
		}

	}
	foreach key_file $::KEY_FILES {
		output "Deleting key file $key_file" 4
		file delete $::TMP/$key_file
	}
	#global FTP_SPAWN_ID
	#if {[info exists FTP_SPAWN_ID]} {
	#	drop_ftp_conn
	#}
}

##################################################
#	end of release_all
##################################################

proc sql_oracle_pl {conn sql conn_name} {
        set proc_name sql_oracle_pl
        global TRAP_SQL_ERROR
	#global ORA_ERROR_MSG
	#global ORA_ERROR_CODE
	#set ORA_ERROR_MSG ""
	#set ORA_ERROR_CODE 0

        regsub -all "\r\n" $sql "\n" sql
        catch {oraplexec $conn $sql} error_msg
        set output_buffer ""
        set ora_status [lindex [oramsg $conn all] 0]
        output $ora_status 1
        if {"$error_msg" > "0"} {
                set output_buffer $error_msg
                global TRAP_SQL_ERROR
		set ORA_ERROR_CODE [lindex $ora_status 0]
		set ORA_ERROR_MSG [lindex $ora_status 1]
                if {$TRAP_SQL_ERROR == 0} {
                        set error_msg "Oracle PL/SQL error:\012$sql\012[lindex $ora_status 1]\012$error_msg"
                        error_out $error_msg 2100
                } else {
			output $error_msg 0
		}
		return
        }
	global AUDIT_TRAIL_ON
        if {$AUDIT_TRAIL_ON == 2} {
		insert_audit $::STEP_ID  "" "PL/SQL: $sql" "$conn_name"
		output "$::STEP_ID PL/SQL: $sql" 1
	} else {
		output "$::STEP_ID PL/SQL: $sql" 1
	}
        output "$::STEP_ID PL/SQL :$sql" 1
}

proc sql_oracle {conn type sql conn_name command} {
        set proc_name sql_oracle
        global TRAP_SQL_ERROR
	#global ORA_ERROR_MSG
	#global ORA_ERROR_CODE
	#set ORA_ERROR_MSG ""
	#set ORA_ERROR_CODE 0
	#global ORA_SQL_ROWS
	set ORA_SQL_ROWS 0

        #regsub -all "\r\n" $sql "\n" sql
	if {$type == 1} {
		# non binding sql
		#output "Regular Oracle SQL"
		catch {orasql $conn $sql} error_msg
        } else {
		# binding sql
		#output $sql
		if {[oramsg $conn sqltype] == 8} {
			catch {eval [subst "oraplexec $conn $sql"]} error_msg
		} else {
			get_xml_root $command
			set pairs [$::ROOT selectNodes  {//pair}]
			set sql ""
			if {[info exists pairs]} {
				
				foreach the_node $pairs {
					set key [string trim [replace_variables_all [$the_node selectNodes string(key)]]]
					set value [replace_variables_all [$the_node selectNodes string(value)]]
					lappend sql :$key $value
				}
			}
			del_xml_root
			output "bind exec sql is $sql" 1
			catch {eval [subst "orabindexec $conn $sql"]} error_msg
		}
		output [oracols $conn all] 2
	}
        set num_of_fields 0
        set row_num 0
        set ora_status [lindex [oramsg $conn all] 0]
        if {[lindex $ora_status 0] || "$error_msg" > "0"} {
		output "Oracle returned status of $ora_status" 1
		set error_msg "$error_msg [lindex $ora_status 1]"
                set output_buffer [lindex $ora_status 1]
                global TRAP_SQL_ERROR
                if {$TRAP_SQL_ERROR == 0} {
                        set error_msg "Oracle sql error:\012$sql\012[lindex $ora_status 1]\012$error_msg"
                        error_out $error_msg 2101
                } else {
			output $error_msg 0
		}
		set ORA_ERROR_CODE [lindex $ora_status 0]
		set ORA_ERROR_MSG [lindex $ora_status 1]
		insert_audit $::STEP_ID "$sql" "$error_msg" $conn_name
		##OLD ->insert_audit $::STEP_ID  "" "sql_exec $sql\012$error_msg" "$conn_name"
                return
        }
        global AUDIT_TRAIL_ON
	if {$AUDIT_TRAIL_ON == 2} {
		set buffer ""
		set buffer $buffer\012[oracols $conn name]
	}
	if {"[lindex $::step_arr($::STEP_ID) 8]" > ""} {
		set xmldoc [dom parse [lindex $::step_arr($::STEP_ID) 8]]
		set root [$xmldoc documentElement]
		set variable_nodes [$root selectNodes  {/variables/variable}]
	}
	if {[info exists variable_nodes]} {
		foreach the_node $variable_nodes {
			set name [string toupper [$the_node selectNodes string(name)]]
			array unset ::runtime_arr $name,*
		}
	}
        if {[lindex $ora_status 5] == 1} {
                while {[orafetch $conn -datavariable row] == 0} {
                        incr row_num
			#output $row
			if {$AUDIT_TRAIL_ON == 2} {
				set buffer "$buffer\012$row"
			}
			if {[info exists variable_nodes]} {
				foreach the_node $variable_nodes {
					### 2010-01-19 - PMD - variable names case insensitive
					set name [string toupper [$the_node selectNodes string(name)]]
					set position [expr [$the_node selectNodes string(position)] - 1]
					set ::runtime_arr($name,$row_num) [lindex $row $position]
					output "setting $name,$row_num = [lindex $row $position]" 2
				}
			}
                }
        }
	set ORA_SQL_ROWS [oramsg $conn rows]
        if {$AUDIT_TRAIL_ON == 2} {
		insert_audit $::STEP_ID "$sql" "$sql\012$buffer\012$ORA_SQL_ROWS rows affected." $conn_name
		##OLD ->insert_audit $::STEP_ID  "" "sql_exec $buffer\012$ORA_SQL_ROWS rows affected" "$conn_name"
		output "$::STEP_ID sql_exec $sql\012$buffer\012$ORA_SQL_ROWS rows." 1
	} else {
		output "$::STEP_ID sql_exec $sql\012$ORA_SQL_ROWS rows." 1
	}
		
}

proc sql_exec_odbc {conn conn_name sql} {
	set proc_name sql_exec_odbc

	output "sql_exec_odbc $conn $conn_name $sql\n" 1
	regsub -all "\r\n" $sql "\n" sql
	if { [catch {set stmt [$conn prepare $sql]} error_msg ]} {
		output "DEBUG: error_msg = $error_msg\n" 1
		if {[string match "invalid command*" $error_msg] == 1} {
			error_out "Invalid ODBC database connection." 2110
		}
		if {"$error_msg" > ""} {
			global TRAP_SQL_ERROR
			if {$TRAP_SQL_ERROR == 0} {
				set error_msg "ODBC sql error:\012$sql\012$error_msg"
				error_out $error_msg 2111
			} else {
				output $error_msg 0
			}
			insert_audit $::STEP_ID "$sql" "$error_msg" $conn_name
		}
	}
	if { [catch {set result [$stmt execute]} error_msg ]} {
		output "DEBUG: error_msg = $error_msg\n" 0
		if {[string match "invalid command*" $error_msg] == 1} {
			error_out "Invalid SQL Server database connection." 2110
		}
		if {"$error_msg" > ""} {
			global TRAP_SQL_ERROR
			if {$TRAP_SQL_ERROR == 0} {
				set error_msg "SQL Server sql error:\012$sql\012$error_msg"
				error_out $error_msg 2111
			} else {
				output $error_msg 0
			}
			insert_audit $::STEP_ID "$sql" "$error_msg" $conn_name
		}
	}
	set all_rows ""
	set output_buffer ""
	set all_rows [$result allrows -as lists]
	output "all_rows = $all_rows" 2
	output "row count = [llength $all_rows]" 2
	output [lindex $::step_arr($::STEP_ID) 8] 2
	if {"[lindex $::step_arr($::STEP_ID) 8]" > ""} {
		set xmldoc [dom parse [lindex $::step_arr($::STEP_ID) 8]]
		set root [$xmldoc documentElement]
		set variable_nodes [$root selectNodes  {/variables/variable}]
		foreach the_node $variable_nodes {
			set name [string toupper [$the_node selectNodes string(name)]]
			array unset ::runtime_arr $name,*
		}
	}
	for {set row_num 0} {$row_num < [llength $all_rows]} {incr row_num} {
		set row [lindex $all_rows $row_num]
		set output_buffer "$output_buffer$row\012"
		if {[info exists variable_nodes]} {
			foreach the_node $variable_nodes {
				### 2010-01-19 - PMD - variable names case insensitive
				set name [string toupper [$the_node selectNodes string(name)]]
				set column_value ""
				set position [expr [$the_node selectNodes string(position)] - 1]
				set ::runtime_arr($name,[expr $row_num + 1]) [lindex $row $position]
				output "setting $name,[expr $row_num + 1]= [lindex $row $position]" 2
			}
		}
	}
	if {[info exists xmldoc]} {
		$root delete
		$xmldoc delete
	}
	insert_audit $::STEP_ID "$sql" "$sql\012$output_buffer\012$row_num rows returned." $conn_name
	##OLD ->insert_audit $::STEP_ID  "" "$sql\012$output_buffer\012$row_num rows returned" "$conn_name"
}
proc sql_exec_mssql {conn conn_name sql} {
	set proc_name sql_exec_mssql

	output "sql_exec_mssql $conn_name $sql\n" 1
	regsub -all "\r\n" $sql "\n" sql
	if { [catch {tdbc_query $conn $sql} error_msg ]} {
		output "DEBUG: error_msg = $error_msg\n" 0
		if {[string match "invalid command*" $error_msg] == 1} {
			error_out "Invalid SQL Server database connection." 2110
		}
		if {"$error_msg" > ""} {
			global TRAP_SQL_ERROR
			if {$TRAP_SQL_ERROR == 0} {
				set error_msg "SQL Server sql error:\012$sql\012$error_msg"
				error_out $error_msg 2111
			} else {
				output $error_msg 0
			}
			insert_audit $::STEP_ID "$sql" "$error_msg" $conn_name
		}
	}
	set all_rows ""
	set output_buffer ""
	set all_rows [tdbc_fetchset $conn]
	output "all_rows = $all_rows" 2
	output "row count = [llength $all_rows]" 2
	output [lindex $::step_arr($::STEP_ID) 8] 2
	if {"[lindex $::step_arr($::STEP_ID) 8]" > ""} {
		set xmldoc [dom parse [lindex $::step_arr($::STEP_ID) 8]]
		set root [$xmldoc documentElement]
		set variable_nodes [$root selectNodes  {/variables/variable}]
		foreach the_node $variable_nodes {
			set name [string toupper [$the_node selectNodes string(name)]]
			array unset ::runtime_arr $name,*
		}
	}
	for {set row_num 0} {$row_num < [llength $all_rows]} {incr row_num} {
		set row [lindex $all_rows $row_num]
		set output_buffer "$output_buffer$row\012"
		if {[info exists variable_nodes]} {
			foreach the_node $variable_nodes {
				### 2010-01-19 - PMD - variable names case insensitive
				set name [string toupper [$the_node selectNodes string(name)]]
				set column_value ""
				set position [expr [$the_node selectNodes string(position)] - 1]
				set ::runtime_arr($name,[expr $row_num + 1]) [lindex $row $position]
				output "setting $name,[expr $row_num + 1]= [lindex $row $position]" 2
			}
		}
	}
	if {[info exists xmldoc]} {
		$root delete
		$xmldoc delete
	}
	insert_audit $::STEP_ID "$sql" "$sql\012$output_buffer\012$row_num rows returned." $conn_name
	##OLD ->insert_audit $::STEP_ID  "" "$sql\012$output_buffer\012$row_num rows returned" "$conn_name"
}

proc sql_exec {command} {
	set proc_name sql_exec

	get_xml_root $command
	set conn_name [replace_variables_all [$::ROOT selectNodes string(conn_name)]]
	set sql [replace_variables_all [$::ROOT selectNodes string(sql)]]
	set mode [replace_variables_all [$::ROOT selectNodes string(mode)]]
	set ora_handle [replace_variables_all [$::ROOT selectNodes string(handle)]]
	del_xml_root
	regsub -all "&gt;" $sql ">" sql
	regsub -all "&lt;" $sql "<" sql
	

	output "$conn_name, $sql" 1
	if {[info exists ::connection_arr($conn_name,handle)]} {
		set system $::connection_arr($conn_name,system))
	} else {
		error_out "The database connection $conn_name has not been established. Check the connection name or the new_connection function" 2020
	}
	#output "spawn id is $spawn_id" 
	if {"$::connection_arr($conn_name,conn_type)" == "oracle"} {
		output "doing a $mode oracle" 1
		switch -- $mode {
			"PREPARE" {
				set ::connection_arr($conn_name,handle,$ora_handle) [oraopen $::connection_arr($conn_name,handle)]
				oraparse $::connection_arr($conn_name,handle,$ora_handle) $sql
			}
			"RUN" {
				#### This needs fixing up, currently broke
				if {![info exists ::connection_arr($conn_name,handle,$ora_handle)]} {
					error_out "SQL Execute error, RUN Mode: The Oracle run handle name $ora_handle is not defined for the connection named $conn_name" 9999
				}
				sql_oracle $::connection_arr($conn_name,handle,$ora_handle) 0 $sql $conn_name $command
			}
			"SQL" {
				sql_oracle $::connection_arr($conn_name,ora_handle) 1 $sql $conn_name ""
			}
			"PL/SQL" {
				sql_oracle_pl $::connection_arr($conn_name,ora_handle) $sql $conn_name ""
			}
			"BEGIN" {
				oraautocom $::connection_arr($conn_name,handle) 0
				insert_audit $::STEP_ID  "" "Transaction started on connection {$conn_name}." ""
			}	
			"COMMIT / BEGIN" {
				oracommit $::connection_arr($conn_name,handle)
				oraautocom $::connection_arr($conn_name,handle) 0
				insert_audit $::STEP_ID  "" "Transaction committed and a new one started on connection {$conn_name}." ""
			}	
			"COMMIT" {
				oracommit $::connection_arr($conn_name,handle)
				oraautocom $::connection_arr($conn_name,handle) 1
				insert_audit $::STEP_ID  "" "Transaction committed on connection {$conn_name}." ""
			}	
			"ROLLBACK" {
				oraroll $::connection_arr($conn_name,handle)
				oraautocom $::connection_arr($conn_name,handle) 1
				insert_audit $::STEP_ID  "" "Transaction rolled back on connection {$conn_name}." ""
			}	
		}
	} elseif {"$::connection_arr($conn_name,conn_type)" == "sqlserver" || "$::connection_arr($conn_name,conn_type)" == "sybase"} {
		output "SQL Server, $mode" 1
		switch -- $mode {
			"SQL" {
				set handle $::connection_arr($conn_name,handle)
				sql_exec_odbc $handle $conn_name $sql
			}
			"BEGIN" {
				set handle $::connection_arr($conn_name,handle)
				$handle begintransaction
				insert_audit $::STEP_ID  "" "Transaction started on connection {$conn_name}." ""
			}	
			"COMMIT / BEGIN" {
				set handle $::connection_arr($conn_name,handle)
				$handle commit
				$handle begintransaction
				insert_audit $::STEP_ID  "" "Transaction committed and a new one started on connection {$conn_name}." ""
			}
			"COMMIT" {
				set handle $::connection_arr($conn_name,handle)
				$handle commit
				insert_audit $::STEP_ID  "" "Transaction committed on connection {$conn_name}." ""
			}	
			"ROLLBACK" {
				set handle $::connection_arr($conn_name,handle)
				$handle rollback
				insert_audit $::STEP_ID  "" "Transaction rolled back on connection {$conn_name}." ""
			}	
		}
	} elseif {"$::connection_arr($conn_name,conn_type)" == "ingres odbc" || "$::connection_arr($conn_name,conn_type)" == "odbc dsn" || "$::connection_arr($conn_name,conn_type)" == "informix odbc"} {
		set handle $::connection_arr($conn_name,handle)
		sql_exec_odbc $handle $conn_name $sql
	}
}

proc drop_ftp_conn {} {
        set proc_name drop_ftp_conn
	global FTP_SPAWN_ID
	global FTP_TYPE
	set spawn_id $FTP_SPAWN_ID
        set timeout 2
        set send_slow {100 .0000000000001}
	if {[catch {exp_send -s -- "quit\r"} return_code]} {
		output "The quit returned - $return_code" 2
		catch {close -i $spawn_id;wait -i $spawn_id}
		return
	} else {
		expect {
			"Goodbye." {
				catch {close -i $spawn_id;wait -i $spawn_id}
				insert_audit $::STEP_ID "logout" $expect_out(buffer) ""
				#output "closed ssh" 4
			}
			timeout {
				catch {close -i $spawn_id;wait -i $spawn_id}
				output "timeout" 0
			}
		}
	}
	global TASK_INSTANCE
	file delete -force file_read/$::TASK_INSTANCE/$::MY_PID
	catch {file delete -force file_read/$::TASK_INSTANCE}
	unset FTP_SPAWN_ID FTP_TYPE

}

proc get_wmi_query {connection namespace sql max_rows} {
	set proc_name get_wmi_query

	if [catch {set result_set [$connection ExecQuery $sql WQL 48]} error_msg] {
		error_out "Invalid WMI Query.  Check query syntax or object names: $sql\012$error_msg" 2200
	}
	#set row_count [$result_set Count]
	set first_row 1
	set row_num 1
	if {"$max_rows" == ""} {
		set max_rows 9999
	}
	set column_header " "
	set output_buffer ""
	#set output_buffer \{
	if [catch {$result_set -iterate row {
		set propSet [$row Properties_]
		set row_buffer ""
		set first_column 1
		$propSet -iterate column {
			if {$first_row} {
				if {$first_column} {
					set column_header "{[$column name]}"
				} else {
					set column_header "$column_header {[$column name]}"
				}
			}
			if {[$column CIMType] == 11} {
				if {[$column value] == 1} {
					set col_value TRUE
				} else {
					set col_value FALSE
				}
			} else {
				set col_value [$column value]
			}
			if {$first_column} {
				set row_buffer "{$col_value}"
				set first_column 0
			} else {
				set row_buffer "$row_buffer {$col_value}"
			}
			$column -destroy
		}
		if {$first_row} {
			#set output_buffer $output_buffer{$column_header}\012{$row_buffer}
			set output_buffer "{$row_buffer}"
			set first_row 0
		} else {
			set output_buffer $output_buffer\012{$row_buffer}
		}
		$row -destroy
		$propSet -destroy
		if {$row_num == $max_rows} {
			break
		}
		incr row_num
	}} error_msg] {
		error_out "Invalid WMI Query.  Check query syntax or object names: $sql\012$error_msg" 2200
	}
	$result_set -destroy
	#set output_buffer $output_buffer\}
	return [list $column_header $output_buffer]
}
proc get_reg {connection path get_name key_or_value} {
	set proc_name get_reg_key_values

	set oreg [$connection Get StdRegProv]

	set hive_name [lindex [split $path \\] 0] 
	set sSubKeyName [join [lrange [split $path \\] 1 end] \\]
	switch -re $hive_name {
		HKCR|HKEY_CLASSES_ROOT {
			set hDefKey 2147483648
		}
		HKCU|HKEY_CURRENT_USER {
			set hDefKey 2147483649
		}
		HKLM|HKEY_LOCAL_MACHINE {
			set hDefKey 2147483650
		}
		HKU|HKEY_USERS {
			set hDefKey 2147483651
		}
		HKCC|HKEY_CURRENT_CONFIG {
			set hDefKey 2147483653
		}
		default {
			error_out "Invalid hive name {$hive_name}.\012Valid hive names are HKEY_CLASSES_ROOT (HKCR), HKEY_CURRENT_USER (HKCU), HKEY_LOCAL_MACHINE (HKLM), HKEY_USERS (HKU) or HKEY_CURRENT_CONFIG (HKCC)." 2201
		}
	}


	### Get list of methods as we will need to retrieve parameter
	### type information for each method we want to use.

	set omethods [$oreg Methods_]

	### We want a WMI input parameter object specific to
	### the EnumValues method.  The twapi COM -with option
	### saves us from having to delete intermediate
	### objects

	output "after Before Access" 1
	set oinparam [$omethods -with {{Item "CheckAccess"} {Inparameters}} SpawnInstance_]
	$oinparam hDefKey $hDefKey
	$oinparam sSubKeyName $sSubKeyName
	#$oinparam uRequired 9
	$oinparam uRequired 131072
	
	set ooutparam [$oreg ExecMethod_ "CheckAccess" [$oinparam -interface]]
	output "after Check Access" 1
	output  "Check access return code = [$ooutparam bGranted]" 1
	if {![$ooutparam bGranted]} {
		if {[$ooutparam bGranted] == 0} {
			set out_message "Invalid registry path $path"
			insert_audit $::STEP_ID "" $out_message ""
		} else {
			error_out "Registry access error [twapi::map_windows_error [$ooutparam bGranted]]. Return code: [$ooutparam bGranted]." 2202
		}
	}
	$oinparam -destroy
	$ooutparam -destroy

	if {"$key_or_value" == "value"} {
		set enum_method EnumValues
	} else {
		set enum_method EnumKey
	}

	set oinparam [$omethods -with {{Item $enum_method} {Inparameters}} SpawnInstance_]

	$oinparam hDefKey $hDefKey
	$oinparam sSubKeyName $sSubKeyName
	
	set ooutparam [$oreg ExecMethod_ $enum_method [$oinparam -interface]]

	set anames [$ooutparam sNames]
	#set output_buffer "\{"
	set output_buffer ""
	set first 1
	if {"$key_or_value" == "value"} {
		set atypes [$ooutparam Types]
	} else { ;# key
		foreach value_name $anames {
			if {!$first} {
				set output_buffer "$output_buffer\012{{$value_name}}"
			} else {
				set output_buffer "$output_buffer{{$value_name}}"
				set first 0
			}
		}
		#set output_buffer "$output_buffer\}"
		$oinparam -destroy
		$ooutparam -destroy
		$omethods -destroy 
		### we're done since we're just getting the subkey names
		return $output_buffer
	}
	$oinparam -destroy
	$ooutparam -destroy

	### Destroy what we do not need.

	
	if {"$get_name" > ""} {
		set index [lsearch -nocase $anames $get_name]
		if {$index == -1} {
			#error_out "The value name $get_name does not exist under the registry path $path\012Valid value names are: $anames" 2203
			set out_message "The value name $get_name does not exist under the registry path $path\012Valid value names are: $anames"
			insert_audit $::STEP_ID "" $out_message ""
			return ""
		} else {
			set anames $get_name
			set atypes [lindex $atypes $index]
		}

	}

	foreach value_name $anames type $atypes {

		#puts "value name -> $value_name, type -> $type"
		switch $type {
			1 { ;# REG_SZ
				set get_method GetStringValue
				set out_param_name sValue
			}
			2 { ;# REG_EXPAND_SZ
				set get_method GetExpandedStringValue
				set out_param_name sValue
			}
			3 { ;# REG_BINARY
				set get_method GetBinaryValue
				set out_param_name uValue
			}
			4 { ;# REG_DWORD
				set get_method GetDWORDValue
				set out_param_name uValue
			}
			7 { ;# REG_MULTI_SZ
				set get_method GetMultiStringValue
				set out_param_name sValue
			}
		}
		set oinparam [$omethods -with {{Item "$get_method"} {Inparameters}} SpawnInstance_]

		### Set up input parameter values.

		$oinparam hDefKey $hDefKey
		$oinparam sSubKeyName $sSubKeyName
		$oinparam sValueName $value_name

		### Get the value

		set ooutparam [$oreg ExecMethod_ $get_method [$oinparam -interface]]
		set value [$ooutparam $out_param_name]

		if {!$first} {
			if {"$get_name" > ""} {
				set output_buffer "$output_buffer\012{{$value}}"
			} else {
				set output_buffer "$output_buffer\012{{$value_name} {$value}}"
			}
		} else {
			if {"$get_name" > ""} {
				set output_buffer "$output_buffer{{$value}}"
			} else {
				set output_buffer "$output_buffer{{$value_name} {$value}}"
			}
			set first 0
		}

		$oinparam -destroy
		$ooutparam -destroy
	}
	#set output_buffer "$output_buffer\}"

	$omethods -destroy 
	return $output_buffer
}

proc close_logfile {} {
	set proc_name close_logfile
	catch {close $::LOG_FILE} 
}
proc initialize_logfile_ce {} {
	set proc_name initialize_logfile
	set ::LOG_FILE [open $::LOGFILES/$::TASK_INSTANCE.log w]
	fconfigure $::LOG_FILE -buffering none -translation {crlf}
	if {"$::tcl_platform(platform)" == "windows"} {
		dup $::LOG_FILE stderr
		dup $::LOG_FILE stdout
	}
}
proc get_var {var} {
	set proc_name get_var
	upvar x $var
	return $x
}
proc print_globals {} {
	set proc_name print_globals
	foreach name [info globals] {
		global $name
		if [array exists $name] {
			parray $name
		} else {
			puts "$name = [set $name]"
		}
	}
}
proc wait_for_tasks {} {
	set proc_name wait_for_tasks

	upvar command command
	regsub -all "&" $command "&amp;" command
	set xmldoc [dom parse $command]
	set root [$xmldoc documentElement]
	set handles [$root selectNodes {//handle}]
	set handle_set ""
	foreach the_node $handles {
		set handle [$the_node selectNodes string(name)]
		if {"[string index $handle 0]" != "#"} {
			set handle "#$handle"
		}
		lappend handle_set $handle
	}
	$root delete
	$xmldoc delete

	lappend finished_status Completed Error Cancelled
	refresh_handles
	while {[llength $handle_set] > 0} {
		foreach handle $handle_set {
			if [info exists ::HANDLE_ARR($handle.INSTANCE)] {
				set ti_status [get_task_status $::HANDLE_ARR($handle.INSTANCE)]
				if {[lsearch $finished_status $ti_status] == -1} {
					### at least one is still running, we'll sleep and continue
					sleep 5
					continue
				} else {
					### this handle's task is finished, remove from the list to check and get the next one 
					insert_audit $::STEP_ID  "" "Task handle $handle is finished with a status of $ti_status..." ""
					set pos [lsearch -exact $handle_set $handle]
					set handle_set [lreplace $handle_set $pos $pos]
				}
			} else {
				### this handle does not exist, check the next one
				insert_audit $::STEP_ID  "" "Task handle $handle does not exist, continuing..." ""
				set pos [lsearch -exact $handle_set $handle]
				set handle_set [lreplace $handle_set $pos $pos]
			}
		}
		if {[llength $handle_set] > 0} {
			sleep 5
		}
	}
	insert_audit $::STEP_ID  "" "All task handles have a finished status" ""
}
proc launch_run_task {} {
	set proc_name launch_run_task

	upvar command command
	regsub -all "&" $command "&amp;" command
	set xmldoc [dom parse $command]
	set root [$xmldoc documentElement]
	set original_task_id [replace_variables_all [$root selectNodes string(original_task_id)]]
	set version [replace_variables_all [$root selectNodes string(version)]]
	set handle [replace_variables_all [$root selectNodes string(handle)]]
	set asset_id [replace_variables_all [$root selectNodes string(asset_id)]]
	set wait_time [replace_variables_all [$root selectNodes string(time_to_wait)]]
	set pair [$root selectNodes {//pair}]

	#FIGURE OUT THE PARAMETERS
	#will take the values from the command and put them in the task_instance_parameter table
	set newdoc [dom createDocument parameters]
	set newroot [$newdoc documentElement]
	if {"$handle" == ""} {
		error_out "Handle name undefined. Run Task requires a handle name." 2000
	}
	if {[lsearch $::handle_names $handle] > -1} {
		insert_audit $::STEP_ID  "" "Handle name already being used in this task. Overwriting..." ""
		set pos [lsearch -exact $::handle_names $handle]
		set ::handle_names [lreplace $::handle_names $pos $pos]
		array unset ::HANDLE_ARR #$handle.*
	}

	if {"$pair" > ""} {
		output "Saving input parameters..." 2
		#regsub -all "&" $pair "&amp;" pair
		foreach node $pair {
			set key [replace_variables_all [$node selectNodes string(key)]]
			set val [string trim [$node selectNodes string(value)]]
			if {[regexp {^\[\[.*,all\]\]$} $val] == 1 || [regexp {^\[\[.*,ALL\]\]$} $val] == 1} {
				set var_name [string toupper [string range $val 2 [expr [string first ,all $val] - 1]]]
				#output "var name is $var_name"
				#output "runtime vars are [array get ::runtime_arr]" 
				foreach {arr_key arr_val} [array get ::runtime_arr $var_name,*] {
					set key_index [lindex [split $arr_key ,] 1]
					#output "match at index $key_index"
					$newroot appendXML "<parameter><key>$key,$key_index</key><value><!\[CDATA\[$arr_val\]\]></value></parameter>"
				}
			} else {
				set val [replace_variables_all $val]
				$newroot appendXML "<parameter><key>$key</key><value><!\[CDATA\[$val\]\]></value></parameter>"
			}
		}
	}
	set parameterXML [$newroot asXML]
	#output "XML = $parameterXML"
	#END PARAMETERS
	
	$newroot delete
	$newdoc delete
	$root delete
	$xmldoc delete
	
	if {"$version" > ""} {
		set sql "select task_id, task_name, version, default_version, now() from task where original_task_id = '$original_task_id' and version = '$version'"
	} else {
		set sql "select task_id, task_name, version, default_version, now() from task where original_task_id = '$original_task_id' and default_version = 1"
	}
	$::db_query $::CONN $sql
	set row [$::db_fetch $::CONN]
	set task_id [lindex $row 0]
	set task_name [lindex $row 1]
	set task_version [lindex $row 2]
	set default_version [lindex $row 3]
	set submitted_dt [lindex $row 4]
	output "run task $task_id"
					
	regsub -all "(')" $parameterXML "''" parameterXML
	#set sql "addTaskInstance '$task_id','$asset_id','$::SUBMITTED_BY', null, '', $::DEBUG_LEVEL, null, $::TASK_INSTANCE, '$parameterXML'"
	set sql "call addTaskInstance ('$task_id',NULL,'$::SUBMITTED_BY','$::DEBUG_LEVEL','','$parameterXML','$::ECOSYSTEM_ID','$::CLOUD_ACCOUNT')"
	#output $sql
	set task_instance_id [::mysql::sel $::CONN $sql -list]
	if {"$task_instance_id" == ""} {
		# must be queued, no task created
		insert_audit $::STEP_ID  "" "Task instance not created, maximum queue depth reached" ""
		return
	}

	#put this handle in the handle_names array
	lappend ::handle_names $handle
	
	# set some variables ...
	set_handle_var "#${handle}.INSTANCE" $task_instance_id
	set_handle_var "#${handle}.TASKID" $task_id
	set_handle_var "#${handle}.SUBMITTED_BY" $::SUBMITTED_BY
	set_handle_var "#${handle}.STATUS" Submitted
	#set_handle_var "#${handle}.STATUS" [lindex $row 1]
	set_handle_var "#${handle}.TASKNAME" $task_name
	set_handle_var "#${handle}.TASKVERSION" $task_version
	set_handle_var "#${handle}.ISDEFAULT" $default_version
	set_handle_var "#${handle}.SUBMITTED_DT" $submitted_dt


	##NOTE! we are putting a "command name" in this insert_audit.  This is a special case
	##	where the GUI picks up the command name and turns it into a hyperlink to jump to the other log.
	if {"$asset_id" > ""} {
		set_handle_var "#${handle}.ASSET" $asset_id
		set sql "select asset_name from asset where asset_id = '$asset_id'"
		$::db_query $::CONN $sql
		set asset_name [lindex [$::db_fetch $::CONN] 0]
		set_handle_var "#${handle}.ASSET_NAME" $asset_name

		insert_audit $::STEP_ID  "run_task $task_instance_id" "Running Task Instance $task_instance_id :: ID $task_id, Name $task_name, Version $task_version on Asset ID $asset_id, Asset Name $asset_name using handle $handle." ""
	} else {
		insert_audit $::STEP_ID  "run_task $task_instance_id" "Running Task Instance $task_instance_id :: ID $task_id, Name $task_name, Version $task_version on Asset ID $asset_id using handle $handle." ""
	}
	if {[string is integer $wait_time] && "$wait_time" > ""} {
		if {$wait_time == 0} {
			# do nothing
		} elseif {$wait_time == -1} {
			# we will sit in a loop forever until the task we kicked off completes
			lappend finished_status Completed Error Cancelled
			insert_audit $::STEP_ID  "" "Waiting until task instance $task_instance_id completes" ""
			while {1 == 1} {
				sleep 5
				set ti_status [get_task_status $task_instance_id]
				if {[lsearch $finished_status $ti_status] > -1} {
					refresh_handles
					break
				}
			}
		} else {
			# we will wait a number of seconds
			insert_audit $::STEP_ID  "" "Waiting $wait_time seconds before continuing" ""
			sleep $wait_time
			refresh_handles
		}
	}
}

proc get_task_status {task_instance} {
	set proc_name get_task_status
	set sql "select task_status from tv_task_instance where task_instance = $task_instance"
	$::db_query $::CONN $sql
	set status [$::db_fetch $::CONN]
	return $status	
}

proc for_loop {command task_name} {
	set proc_name for_loop
	get_xml_root $command
	set initial [replace_variables_all [$::ROOT selectNodes string(start)]]
	set counter [string toupper [$::ROOT selectNodes string(counter)]]
	set loop_test [$::ROOT selectNodes string(test)]
	set compare_to [replace_variables_all [$::ROOT selectNodes string(compare_to)]]
	set increment [replace_variables_all [$::ROOT selectNodes string(increment)]]
	set max_iterations [replace_variables_all [$::ROOT selectNodes string(max)]]
	set action [$::ROOT selectNodes string(action)]
	del_xml_root
	if {"$action" == ""} {
		error_out "Loop action is empty, action is required." 2020
	}

	set loop_var ::runtime_arr($counter,1)
	set loop_var2 "\$$loop_var"
	set ::runtime_arr($counter,1) [expr $initial + [expr 0 - $increment]]
	set max_iterations ""
	
	output "loop_var is $loop_var" 5
	output "loop_var2 is $loop_var2" 5
	output "loop_var2 value is [expr $loop_var2]" 5
	regsub -all "&gt;" $loop_test ">" loop_test
	regsub -all "&lt;" $loop_test "<" loop_test
	output "loop_test operator is $loop_test" 5
	output "initial value is $initial" 5
	output "increment by $increment" 5
	output "compare_to is $compare_to" 5
	output "loop test: $initial $loop_test $compare_to" 4
	#output "command is $command" 2
	for {set $loop_var $initial} {[expr $loop_var2 $loop_test $compare_to]} {incr $loop_var $increment} {
		if {"$max_iterations" > "" && $max_iterations < [expr $loop_var2]} {
			output "max iterations hit, $max_iterations. Breaking out of loop"
			break
		}
		set ::INLOOP 1
		process_step $action $task_name
		if {$::BREAK == 1} {
			set ::BREAK 0
			break
		}
	}
	set ::INLOOP 0

	return
}
proc while_loop {command task_name} {
	get_xml_root $command
	set test [$::ROOT selectNodes string(test)]
	set action [$::ROOT selectNodes string(action)]
	del_xml_root
	if {"$action" == ""} {
		error_out "While action is empty, action is required." 2020
	}
	set test_cond [replace_variables_all $test]
	regsub -all "&amp;" $test_cond {\&} test_cond
	regsub -all "&gt;" $test_cond ">" test_cond
	regsub -all "&lt;" $test_cond "<" test_cond
	while {[expr $test_cond]} {
		set ::INLOOP 1
		process_step $action $task_name
		if {$::BREAK == 1} {
			set ::BREAK 0
			break
		}
		set test_cond [replace_variables_all $test]
		regsub -all "&amp;" $test_cond {\&} test_cond
		regsub -all "&gt;" $test_cond ">" test_cond
		regsub -all "&lt;" $test_cond "<" test_cond
	} 	
	set ::INLOOP 0
	return
}
proc get_instance_handle {command} {
	set proc_name get_instance_handle

	get_xml_root $command
	set task_instance [replace_variables_all [$::ROOT selectNodes string(instance)]]
	set handle [replace_variables_all [$::ROOT selectNodes string(handle)]]
	del_xml_root
	if {"$handle" == ""} {
		error_out "Get Task Instance command requires a handle name" 9999
	} 
	if {"$task_instance" == ""} {
		error_out "Get Task Instance command requires a task instance number" 9999
	} 
	lappend ::handle_names $handle
	set ::HANDLE_ARR(#${handle}.INSTANCE) $task_instance
	refresh_handles
}
proc cancel_tasks {command} {
	set proc_name cancel_tasks

	get_xml_root $command
	set tis [replace_variables_all [$::ROOT selectNodes string(task_instance)]]
	del_xml_root

	foreach task_instance $tis {
		if {[string is integer $task_instance]} {
			insert_audit $::STEP_ID "" "Cancelling task instance $task_instance" ""
			set sql "update task_instance set task_status = 'Aborting' where task_instance = $task_instance and task_status in ('Submitted','Staged','Processing')"
			$::db_exec $::CONN $sql
			set log "Cancelling task instance $task_instance by other task instance number $::TASK_INSTANCE"
			set sql "insert into task_instance_log (task_instance, step_id, entered_dt, connection_name, log, command_text) values ($task_instance,'', $::getdate,'','$log','')"
			catch {$::db_exec $::CONN $sql}
		} else {
			error_out "Cancel Task error: invalid task instance: $task_instance. Value must be an integer." 9999
		}
	}
}
proc notify_error {out_message} {
	set proc_name notify_error
	set sql "select admin_email from messenger_settings where id = 1"
	$::db_query $::CONN $sql
	set row [lindex [$::db_fetch $::CONN] 0]
	set email_to $row
	if {[info exists ::runtime_arr(_NOTIFY_ON_ERROR,1)]} {
		set email_to "$email_to,$::runtime_arr(_NOTIFY_ON_ERROR,1)"
	}
	if {"$email_to" > ""} {
		regsub -all "(')" $out_message "''" out_message
		set subject "Task Error - $::TASK_NAME, error code $::errorCode at [clock format [clock seconds] -format "%Y-%m-%d %H:%M:%S"], node $::CE_NAME"
		insert_audit $::STEP_ID "" "Inserting into message queue : TO:{$email_to} SUBJECT:{$subject} BODY:{$out_message}" ""

		set sql "insert into message (date_time_entered,process_type,status,msg_to,msg_from,msg_subject,msg_body) values ($::getdate,1,0,'$email_to','$::CE_NAME','$subject','$out_message')"
		$::db_exec $::CONN $sql
	}
}
proc run_task_instance {} {
	set proc_name run_task_instance

	set sql "update task_instance set pid = [pid], task_status = 'Processing', started_dt = $::getdate where task_instance = $::TASK_INSTANCE"
	$::db_exec $::CONN $sql

	if {[catch {process_task} errMsg]} {

		### We have an internal script error. Let's update the database

		if {"$::errorCode" == "NONE"} {
			set ::errorCode 3003
		}
		set out_message "ERROR -> Error Code: $::errorCode, $errMsg"
		output $out_message 0
		output $::errorInfo 0
		insert_audit $::STEP_ID "" $out_message ""
		catch {notify_error $out_message} new_err
		output $new_err 0
		update_status Error
		set return_code $::errorCode	

	} else {
		output "Task Completed Normally" -99
		update_status Completed
		set return_code 0
	}
	#print_globals
	array unset ::runtime_arr 
	array unset ::codeblock_arr
	array unset ::step_arr
	array unset ::HANDLE_ARR 
	unset -nocomplain TASK_ID TASK_INSTANCE TASK_NAME TASK_VERSION TEST_RESULT SUBMITTED_BY SCHEDULE_INSTANCE DATASET 
	return $return_code
}
proc task_conn_log {address userid conn_type} {
        set proc_name task_conn_log

        set sql "insert into task_conn_log (task_instance, address, userid, conn_type, conn_dt) values ($::TASK_INSTANCE,'$address','$userid','
$conn_type', $::getdate)"
        if { [catch {$::db_exec $::CONN $sql} return_code ]} {
            output "Error: Unable to write conn log: --$return_code--"
            output "$sql"
        }
}

proc connect_db {} {
        set proc_name connect_db

        output "Connecting to $::CONNECT_SERVER $::CONNECT_DB $::CONNECT_PORT, user $::CONNECT_USER" 5
		if {"$::tcl_platform(platform)" == "windows"} {
	        if {[catch {set ::CONN [tdbc_connect "$::CONNECT_USER" "$::CONNECT_PASSWORD" "$::CONNECT_SERVER" "$::CONNECT_DB" "odbcsqlserver" "$::CONNECT_PORT" "command_engine.$::MY_PID"]} errMsg]} {
                output "Could not connect to the database. Error message -> $errMsg"
                output "Exiting..."
                exit
	        }
		} else {
    	    if {[catch {set ::CONN [::mysql::connect -user $::CONNECT_USER -password $::CONNECT_PASSWORD -host $::CONNECT_SERVER -db $::CONNECT_DB -port $::CONNECT_PORT -multiresult 1 -multistatement 1]} errMsg]} {
                output "Could not connect to the database. Error message -> $errMsg"
                output "Exiting..."
                exit
        	}
		}
    	output "Connected" 6
}
##################################################
#
#       MAIN LOGIC ROUTINE
#
##################################################
proc main_ce {} {
	set proc_name main

	#set ::CE_NAME [lindex $::argv 0]
	set ::TASK_INSTANCE [lindex [split [lindex $::argv 0]] 0]
	pre_initialize

	initialize_logfile_ce
	#output "argv(1) is ->[lindex $::argv 1]<-"


	initialize
	output "Task Instance: {$::TASK_INSTANCE} PID: {$::MY_PID}" -99


	###
	### Next let's reconnect to the db
	###

	connect_db

	###
	### Now let's process the task instance
	###
	set return_code [run_task_instance]

	### and any additional task instances ...
	if {$return_code == 0} {
		if {[info exists ::DATASET]} {
			$::DATASET delete
			unset ::DATASET
		}
		close_logfile
	} else {
		insert_audit $::STEP_ID "" "Erroring task instance {$::TASK_INSTANCE}... error code {$return_code}." ""
		update_status Error
	}
	release_all
	close_logfile
	catch {$::db_disconnect $::CONN}
}
main_ce
#if {[catch {main} errMsg]} {
#	set ::DEBUG_LEVEL 0
#	set proc_name main
#	output "ERROR: -> $errMsg"
#}
### 
### We are done at this point
###
flush stdout
exit 0
