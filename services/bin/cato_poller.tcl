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

set PROCESS_NAME cato_poller
source $::env(CATO_HOME)/services/bin/common.tcl
read_config

proc start_submitted_tasks {get_num} {
	set proc_name start_submitted_tasks 
	set V3_CE 1
	set sql "select ti.task_instance, ti.asset_id, ti.schedule_instance 
		from tv_task_instance ti
		join task t on t.task_id = ti.task_id
		where ti.task_status = 'Submitted'
		order by task_instance asc limit $get_num"
#			and ti.ce_node = $::INSTANCE_ID
	::mysql::sel $::CONN $sql
	set ii 0
	while {[string length [set row [::mysql::fetch $::CONN]]] > 0} {
		output "Considering Task Instance: $row"
		incr ii
		set task_array($ii) $row
	}
	for {set jj 1} {$jj <= $ii} {incr jj} {
		set task_instance [lindex $task_array($jj) 0]
		set asset_id [lindex $task_array($jj) 1]
		set schedule_instance [lindex $task_array($jj) 2]
		
		if {"$task_instance" > "0"} {
			set pid ""
			set error_flag 0
			lappend task_list $task_instance

			output "Starting process..."

			set pid [exec nohup $::HOME/services/bin/cato_task_engine.tcl $task_instance >& $::LOGFILES/ce/$task_instance.log &]
			output "Started Task Instance(s) $task_list, process id, $pid."
			if {"$pid" > ""} {
				set sql "update task_instance set task_status = 'Staged', pid = $pid where task_instance in ([string map {{ } ,} $task_list])"
				::mysql::exec $::CONN $sql
			} elseif {$error_flag == 0} {
				set sql "update task_instance set task_status = 'Staged' where task_instance in ([string map {{ } ,} $task_list])"
				::mysql::exec $::CONN $sql
			}
			after 50
			unset task_list
		}
	}
}
proc update_to_error {the_pid} {
	set proc_name update_to_error
	output "Settings Tasks with PID $the_pid and Processing status to Error..." 

	set sql "update task_instance set task_status = 'Error', completed_dt = now() where pid = $the_pid and task_status = 'Processing'"
	::mysql::exec $::CONN $sql
}
proc update_cancelled {task_instance} {
	set proc_name update_cancelled
	set sql "update task_instance set task_status = 'Cancelled', completed_dt = now() where task_instance = $task_instance"
	::mysql::exec $::CONN $sql
}

proc kill_ce_pid {pid} {
	set proc_name kill_ce_pid
	output "Killing process $pid"
	catch {[exec -ignorestderr kill -9 $pid]}
}

proc check_processing {} {
	set proc_name check_processing
	set sql "select distinct pid from tv_task_instance where ce_node = $::INSTANCE_ID and task_status = 'Processing' and pid is not null"
	::mysql::sel $::CONN $sql
	set pids_db [::mysql::fetch $::CONN]
	#get a ps list
	#set pids_os [exec -ignorestderr ps U$::tcl_platform(user) -opid,command | grep command_engine.tcl 2>@ stderr]
	set pids_os ""
	#output "ps U$::tcl_platform(user) -opid,command | grep command_engine.tcl | grep -v grep | grep -v PID"

	foreach the_pid $pids_db {
		if {[lsearch "$pids_os" [lindex $the_pid 0]] == -1} {
			update_to_error [lindex $the_pid 0]
		}
	}
}

proc update_load {} {
	set proc_name update_load
	
	#set load_value [exec bin/load.sh 2>@ stderr]
	set load_value .5
	if {"$load_value" == ""} {
		set load_value 0
	}
	
	if {[info exists load_value]} {
		set sql "update application_registry set load_value = $load_value where id = $::INSTANCE_ID"
		::mysql::exec $::CONN $sql
	}
}

proc get_settings {} {
	set proc_name get_settings
	
	set ::PREVIOUS_MODE ""
	
	if {[info exists ::MODE]} {
		set ::PREVIOUS_MODE $::MODE
	}

	set sql "select mode_off_on, loop_delay_sec, max_processes from poller_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [::mysql::fetch $::CONN]
	set ::MODE [lindex $row 0]
	set ::LOOP [lindex $row 1]
	set ::MAX_PROCESSES [lindex $row 2]
        set sql "select admin_email from messenger_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [lindex [::mysql::fetch $::CONN] 0]
	set ::ADMIN_EMAIL [lindex $row 0]
	
	#did the run mode change? not the first time of course previous_mode will be ""
	if {"$::PREVIOUS_MODE" != "" && "$::PREVIOUS_MODE" != "$::MODE"} {
		output "*** Control Change: Mode is now $::MODE"
	}

}
proc get_aborting {} {
	set proc_name get_aborting
	set sql "select task_instance, pid from tv_task_instance where ce_node = $::INSTANCE_ID and task_status = 'Aborting' order by task_instance asc"
	::mysql::sel $::CONN $sql
	set rows [::mysql::fetch $::CONN]
	foreach row $rows {
		output "cancelling -> $row"
		if {"[lindex $row 1]" > ""} {
			kill_ce_pid [lindex $row 1]
		}
		update_cancelled [lindex $row 0]
	}
}
proc initialize_process {} {
}
proc main_process {} {
	update_load
	get_aborting
	check_processing
	### TO DO - need to get process count from linux
	set process_count 0
	set get_processes [expr $::MAX_PROCESSES - $process_count]
	if {[catch {start_submitted_tasks $get_processes} errorMsg]} {
		output "Poller error -> $errorMsg"
		error $errorMsg $errorMsg 9999
	}
}
main
exit 0
