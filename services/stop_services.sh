#!/bin/bash
date
if [ -z "$CATO_HOME" ]; then

    EX_FILE=`readlink -f $0`
    EX_HOME=${EX_FILE%/*}
    CATO_HOME=${EX_HOME%/*}
    echo "CATO_HOME not set, assuming $CATO_HOME"
    export CATO_HOME
fi

# All other processes go here.  No process should be in both sections though.
FULL_PROCS[0]="$CATO_HOME/services/bin/cato_poller.tcl"
FULL_PROCS[1]="$CATO_HOME/services/bin/cato_scheduler.tcl"
FULL_PROCS[2]="$CATO_HOME/services/bin/cato_messenger.tcl"
FULL_PROCS[3]="$CATO_HOME/services/bin/cato_ecosync.tcl"

count=0
while [[ $count -lt ${#FULL_PROCS[*]} ]]; do
    PIDS=`ps -eafl | grep "${FULL_PROCS[$count]}" | grep -v "grep" | awk '{ print \$4 }'`
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
        crontab -l | grep -v start_services.sh > $CATO_HOME/conf/crontab.backup 2>/dev/null
        crontab -r 2>/dev/null
        crontab $CATO_HOME/conf/crontab.backup
        rm $CATO_HOME/conf/crontab.backup
touch .shutdown

echo "end"
exit
