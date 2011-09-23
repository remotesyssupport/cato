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

set PROCESS_NAME cato_scheduler
source $::env(CATO_HOME)/services/bin/common.tcl
read_config

proc check_schedules {} {
	set proc_name check_schedules
	set sql "select ap.schedule_id, min(ap.plan_id) as plan_id, ap.task_id, 
		ap.action_id, ap.ecosystem_id, ap.parameter_xml, ap.debug_level, min(ap.run_on_dt), e.account_id		
		from action_plan  ap
		join ecosystem e on e.ecosystem_id = ap.ecosystem_id
		where run_on_dt < now() and schedule_id is not null group by schedule_id
		union
		select '', ap.plan_id, ap.task_id, ap.action_id, ap.ecosystem_id, ap.parameter_xml, 
			ap.debug_level, ap.run_on_dt, e.account_id
		from action_plan ap 
		join ecosystem e on e.ecosystem_id = ap.ecosystem_id
		where run_on_dt < now() and schedule_id is null"
	set rows [::mysql::sel $::CONN $sql -list]
	foreach row $rows {
		run_schedule_instance $row
	}
}
proc split_clean {it} {
	set proc_name split_clean
	set the_list ""
	foreach x [split $it ,] {
		if {"$x" > ""} {
			lappend the_list $x
		}
	}
	return $the_list
}
proc expand_this_schedule {sched_row} {
	set proc_name expand_this_schedule
	set id [lindex $sched_row 0]
	set now [lindex $sched_row 1]
	#set start_dt [lindex $sched_row 2]
	#set stop_dt [lindex $sched_row 3]
	set months [split_clean [lindex $sched_row 2]]
	set days_or_weeks [lindex $sched_row 3]
	set days [split_clean [lindex $sched_row 4]]
	set hours [split_clean [lindex $sched_row 5]]
	set minutes [split_clean [lindex $sched_row 6]]
	set start_dt [lindex $sched_row 7]
	set task_id [lindex $sched_row 8]
	set action_id [lindex $sched_row 9]
	set ecosystem_id [lindex $sched_row 10]
	set parameter_xml [lindex $sched_row 11]
	set debug_level [lindex $sched_row 12]
	set start_instances [lindex $sched_row 13]
	#output "sched row is $sched_row"

        #set top_run_dt [::mysql::fetch $::CONN]
	#if {($top_run_dt >= $stop_dt && $stop_dt != 0)} {
	#	output "Lifecycle for schedule $id is complete. Retiring."
	#	set sql "update schedule set status = 'Retired' where schedule_id = '$id'"
	#	::mysql::exec $::CONN $sql
	#	return
	#}
	#if {$recurring == 0} {
	#	output "Lifecycle for schedule $id is complete. Retiring."
	#	set sql "update schedule set status = 'Retired' where schedule_id = '$id'"
	#	::mysql::exec $::CONN $sql
	#}
	#if {$start_dt < $top_run_dt} {
	#	set start_dt $top_run_dt
	#}
	#output "start date is $start_dt and real now is [clock seconds]"
	#output "start_dt = $start_dt, now is $now"
	if {$start_dt == 0 || $start_dt == ""} {
		set start_dt $now
	} else {
		set start_dt [expr $start_dt + 1]
	}
	#output "now year is [clock format $now -format "%Y"]"	
	set start_year [clock format $start_dt -format "%Y"]
	set start_mon [clock format $start_dt -format "%m"]
	set start_day [clock format $start_dt -format "%d"]
	set start_hr [clock format $start_dt -format "%H"]
	set start_min [clock format $start_dt -format "%M"]
	#output "start_dt = $start_dt, now is $now"
	#output "$start_year, $start_mon, $start_day, $start_hr, $start_min"

	#if {$stop_dt == 0} {
	#	set stop_dt [expr $now + ($::MAX_DAYS * 24 * 60 * 60)]
	#}
	#output "max days is $::MAX_DAYS, stop dt is [clock format $stop_dt -format "%Y-%m-%d %H:%M:%S"], instances to start is $start_instances"
	set the_dates ""
	set enough 0
	for {set y $start_year} {$y <= [expr $start_year + 1]} {incr y} {
		foreach m [lsort -integer $months] {
			incr m
			set test [clock scan "$y-$m-01 00:00" -format {%Y-%m-%d %H:%M}]
			set test_start [clock scan "$start_year-$start_mon-01 00:00" -format {%Y-%m-%d %H:%M}]
			#output "comparing $test < $test_start"
			if {$test < $test_start} {
				continue
			}
			foreach d [lsort -integer $days] {
				incr d
				set test [clock scan "$y-$m-$d 00:00" -format {%Y-%m-%d %H:%M}]
				set test_start [clock scan "$start_year-$start_mon-$start_day 00:00" -format {%Y-%m-%d %H:%M}]
				if {$test < $test_start} {
					continue
				}
				foreach h [lsort -integer $hours] {
			
					set test [clock scan "$y-$m-$d $h:00" -format {%Y-%m-%d %H:%M}]
					set test_start [clock scan "$start_year-$start_mon-$start_day $start_hr:00" -format {%Y-%m-%d %H:%M}]
					if {$test < $test_start} {
						continue
					}
					foreach min [lsort -integer $minutes] {
						set test [clock scan "$y-$m-$d $h:$min" -format {%Y-%m-%d %H:%M}]
						set test_start [clock scan "$start_year-$start_mon-$start_day $start_hr:$start_min" -format {%Y-%m-%d %H:%M}]
						#output "comparing $test < $test_start"
						if {$test < $test_start} {
							continue
						}
						set the_time [clock scan "$y-$m-$d $h:$min" -format {%Y-%m-%d %H:%M}]
						if {$the_time >= $start_dt} {
							#output $the_time
							lappend the_dates $the_time
						}
						if {[llength $the_dates] >= $start_instances} {
							set enough 1
							break
						}
					}
					if {$enough == 1} {
						break
					}
				}
				if {$enough == 1} {
					break
				}
			}
			if {$enough == 1} {
				break
			}
		}
		if {$enough == 1} {
			break
		}
	}
	set the_dates [lsort -unique $the_dates]
	#output "There are [llength $the_dates] dates"
	foreach date $the_dates {
		set date [clock format $date -format "%Y-%m-%d %H:%M:%S"]
		set sql "insert into action_plan (task_id,run_on_dt,action_id,ecosystem_id,parameter_xml,debug_level,source,schedule_id)
			values ('$task_id', '$date', '$action_id', '$ecosystem_id', '$parameter_xml', '$debug_level', 'schedule', '$id')"
		::mysql::exec $::CONN $sql
	}	
}
proc run_schedule_instance {instance_row} {
	set proc_name run_schedule_instance

	#set sql "update schedule_instance set status = 'Processing' where schedule_instance = '$instance'"
	#::mysql::exec $::CONN $sql
	set schedule_id [lindex $instance_row 0]
	set plan_id [lindex $instance_row 1]
	set task_id [lindex $instance_row 2]
	set action_id [lindex $instance_row 3]
	set ecosystem_id [lindex $instance_row 4]
	set parameter_xml [lindex $instance_row 5]
	set debug_level [lindex $instance_row 6]
	set run_on_dt [lindex $instance_row 7]
	set account_id [lindex $instance_row 8]

	set sql "call addTaskInstance ('$task_id',NULL,'$schedule_id','$debug_level','$plan_id','$parameter_xml','$ecosystem_id,','$account_id')"
#output $sql
	set ti [::mysql::sel $::CONN $sql -list]
	output "Started task instance $ti for schedule id $schedule_id and plan id $plan_id"
	set sql "insert into action_plan_history (plan_id, task_id, run_on_dt, action_id, ecosystem_id, parameter_xml, debug_level, source, schedule_id, task_instance)
		values ('$plan_id', '$task_id', '$run_on_dt', '$action_id', '$ecosystem_id', '$parameter_xml', '$debug_level', 'schedule', '$schedule_id', '$ti')"
	::mysql::exec $::CONN $sql
	set sql "delete from action_plan where plan_id = '$plan_id'"
	::mysql::exec $::CONN $sql

}
proc clear_scheduled_action_plans {} {
	### delete action_plan rows upon scheduler startup to remove any backlog
	set sql "delete from action_plan where source = 'schedule'"
	::mysql::exec $::CONN $sql
}
proc expand_schedules {} {
	set proc_name expand_schedules
	set sql "select distinct(a.schedule_id), unix_timestamp() as now, a.months, a.days_or_weeks, a.days, 
		a.hours, a.minutes, max(unix_timestamp(ap.run_on_dt)), a.task_id, a.action_id, a.ecosystem_id,
		a.parameter_xml, a.debug_level, $::MIN_DEPTH - count(ap.schedule_id) as num_to_start
		from action_schedule a
		left outer join action_plan ap on ap.schedule_id = a.schedule_id
		group by a.schedule_id
		having count(*) < $::MIN_DEPTH"

        set rows [::mysql::sel $::CONN $sql -list]
	foreach row $rows {
		#output "remaining $remaining_instances, min depth $::MIN_DEPTH"
		expand_this_schedule $row
	}

}
proc get_settings {} {
	
	set ::PREVIOUS_MODE ""
	
	if {[info exists ::MODE]} {
		set ::PREVIOUS_MODE $::MODE
	}

	set sql "select mode_off_on, loop_delay_sec, schedule_min_depth, schedule_max_days from scheduler_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [::mysql::fetch $::CONN]
	set ::MODE [lindex $row 0]
	set ::LOOP [lindex $row 1]
	set ::MIN_DEPTH [lindex $row 2]
	set ::MAX_DAYS [lindex $row 3]
	if {$::MIN_DEPTH <= 0} {
		set ::MIN_DEPTH 1
	}
	if {$::MAX_DAYS > 360} {
		set ::MAX_DAYS 360
	}
        set sql "select admin_email from messenger_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [lindex [::mysql::fetch $::CONN] 0]
	set ::ADMIN_EMAIL [lindex $row 0]
	
	#did the run mode change? not the first time of course previous_mode will be ""
	if {"$::PREVIOUS_MODE" != "" && "$::PREVIOUS_MODE" != "$::MODE"} {
		output "*** Control Change: Mode is now $::MODE"
	}

}
proc initialize_process {} {
	clear_scheduled_action_plans
}
proc main_process {} {

	expand_schedules
	check_schedules
}
main
exit 0
