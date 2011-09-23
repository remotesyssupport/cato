#!/bin/bash
date
OWNER=`ls -l $0 | awk '{ print $3 }'`

if [ $OWNER != $LOGNAME ]; then
    echo "$0 must be run by owner."
    exit
fi

. /$HOME/set_ce_env.sh

# All other processes go here.  No process should be in both sections though.
FULL_PROCS[0]="bin/tclsh poller.tcl"
FULL_PROCS[1]="bin/tclsh bin/logserver.tcl"
FULL_PROCS[2]="bin/tclsh scheduler.tcl"
FULL_PROCS[3]="bin/tclsh d_checker.tcl"
FULL_PROCS[4]="bin/tclsh messenger.tcl"


count=0
while [[ $count -lt ${#FULL_PROCS[*]} ]]; do
    PIDS=`ps -eafl -u $LOGNAME | grep "${FULL_PROCS[$count]}" | grep -v "grep" | awk '{ print \$4 }'`
    if [ -z "$PIDS" ]; then
        echo "${FULL_PROCS[$count]} is not running"
    else
        for PID in $PIDS; do
            echo "Shutting down $i ($PID)"
            kill -9 $PID
        done
    fi
        (( count += 1 ))
done

echo "Removing startup.sh from crontab"
        crontab -l | grep -v startup.sh > etc/crontab.backup 2>/dev/null
        crontab -r 2>/dev/null
        crontab etc/crontab.backup
        rm etc/crontab.backup
touch .shutdown

echo "end"
exit

