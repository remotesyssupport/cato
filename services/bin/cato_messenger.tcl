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

set PROCESS_NAME cato_messenger
set ::CATO_HOME [file dirname [file dirname [file dirname [file normalize $argv0]]]]
source $::CATO_HOME/services/bin/common.tcl
read_config

proc init_mimetypes {} {
	set proc_name init_mimetypes
	global mimetype
	### This list is generated from the following url: http://www.w3schools.com/media/media_mimeref.asp

	dict set mimetype "" application/octet-stream
	dict set mimetype 323 text/h323
	dict set mimetype acx application/internet-property-stream
	dict set mimetype ai application/postscript
	dict set mimetype aif audio/x-aiff
	dict set mimetype aifc audio/x-aiff
	dict set mimetype aiff audio/x-aiff
	dict set mimetype asf video/x-ms-asf
	dict set mimetype asr video/x-ms-asf
	dict set mimetype asx video/x-ms-asf
	dict set mimetype au audio/basic
	dict set mimetype avi video/x-msvideo
	dict set mimetype axs application/olescript
	dict set mimetype bas text/plain
	dict set mimetype bcpio application/x-bcpio
	dict set mimetype bin application/octet-stream
	dict set mimetype bmp image/bmp
	dict set mimetype c text/plain
	dict set mimetype cat application/vnd.ms-pkiseccat
	dict set mimetype cdf application/x-cdf
	dict set mimetype cer application/x-x509-ca-cert
	dict set mimetype class application/octet-stream
	dict set mimetype clp application/x-msclip
	dict set mimetype cmx image/x-cmx
	dict set mimetype cod image/cis-cod
	dict set mimetype cpio application/x-cpio
	dict set mimetype crd application/x-mscardfile
	dict set mimetype crl application/pkix-crl
	dict set mimetype crt application/x-x509-ca-cert
	dict set mimetype csh application/x-csh
	dict set mimetype css text/css
	dict set mimetype dcr application/x-director
	dict set mimetype der application/x-x509-ca-cert
	dict set mimetype dir application/x-director
	dict set mimetype dll application/x-msdownload
	dict set mimetype dms application/octet-stream
	dict set mimetype doc application/msword
	dict set mimetype dot application/msword
	dict set mimetype dvi application/x-dvi
	dict set mimetype dxr application/x-director
	dict set mimetype eps application/postscript
	dict set mimetype etx text/x-setext
	dict set mimetype evy application/envoy
	dict set mimetype exe application/octet-stream
	dict set mimetype fif application/fractals
	dict set mimetype flr x-world/x-vrml
	dict set mimetype gif image/gif
	dict set mimetype gtar application/x-gtar
	dict set mimetype gz application/x-gzip
	dict set mimetype h text/plain
	dict set mimetype hdf application/x-hdf
	dict set mimetype hlp application/winhlp
	dict set mimetype hqx application/mac-binhex40
	dict set mimetype hta application/hta
	dict set mimetype htc text/x-component
	dict set mimetype htm text/html
	dict set mimetype html text/html
	dict set mimetype htt text/webviewhtml
	dict set mimetype ico image/x-icon
	dict set mimetype ief image/ief
	dict set mimetype iii application/x-iphone
	dict set mimetype ins application/x-internet-signup
	dict set mimetype isp application/x-internet-signup
	dict set mimetype jfif image/pipeg
	dict set mimetype jpe image/jpeg
	dict set mimetype jpeg image/jpeg
	dict set mimetype jpg image/jpeg
	dict set mimetype js application/x-javascript
	dict set mimetype latex application/x-latex
	dict set mimetype lha application/octet-stream
	dict set mimetype lsf video/x-la-asf
	dict set mimetype lsx video/x-la-asf
	dict set mimetype lzh application/octet-stream
	dict set mimetype m13 application/x-msmediaview
	dict set mimetype m14 application/x-msmediaview
	dict set mimetype m3u audio/x-mpegurl
	dict set mimetype man application/x-troff-man
	dict set mimetype mdb application/x-msaccess
	dict set mimetype me application/x-troff-me
	dict set mimetype mht message/rfc822
	dict set mimetype mhtml message/rfc822
	dict set mimetype mid audio/mid
	dict set mimetype mny application/x-msmoney
	dict set mimetype mov video/quicktime
	dict set mimetype movie video/x-sgi-movie
	dict set mimetype mp2 video/mpeg
	dict set mimetype mp3 audio/mpeg
	dict set mimetype mpa video/mpeg
	dict set mimetype mpe video/mpeg
	dict set mimetype mpeg video/mpeg
	dict set mimetype mpg video/mpeg
	dict set mimetype mpp application/vnd.ms-project
	dict set mimetype mpv2 video/mpeg
	dict set mimetype ms application/x-troff-ms
	dict set mimetype mvb application/x-msmediaview
	dict set mimetype nws message/rfc822
	dict set mimetype oda application/oda
	dict set mimetype p10 application/pkcs10
	dict set mimetype p12 application/x-pkcs12
	dict set mimetype p7b application/x-pkcs7-certificates
	dict set mimetype p7c application/x-pkcs7-mime
	dict set mimetype p7m application/x-pkcs7-mime
	dict set mimetype p7r application/x-pkcs7-certreqresp
	dict set mimetype p7s application/x-pkcs7-signature
	dict set mimetype pbm image/x-portable-bitmap
	dict set mimetype pdf application/pdf
	dict set mimetype pfx application/x-pkcs12
	dict set mimetype pgm image/x-portable-graymap
	dict set mimetype pko application/ynd.ms-pkipko
	dict set mimetype pma application/x-perfmon
	dict set mimetype pmc application/x-perfmon
	dict set mimetype pml application/x-perfmon
	dict set mimetype pmr application/x-perfmon
	dict set mimetype pmw application/x-perfmon
	dict set mimetype pnm image/x-portable-anymap
	dict set mimetype pot application/vnd.ms-powerpoint
	dict set mimetype ppm image/x-portable-pixmap
	dict set mimetype pps application/vnd.ms-powerpoint
	dict set mimetype ppt application/vnd.ms-powerpoint
	dict set mimetype prf application/pics-rules
	dict set mimetype ps application/postscript
	dict set mimetype pub application/x-mspublisher
	dict set mimetype qt video/quicktime
	dict set mimetype ra audio/x-pn-realaudio
	dict set mimetype ram audio/x-pn-realaudio
	dict set mimetype ras image/x-cmu-raster
	dict set mimetype rgb image/x-rgb
	dict set mimetype rmi audio/mid
	dict set mimetype roff application/x-troff
	dict set mimetype rtf application/rtf
	dict set mimetype rtx text/richtext
	dict set mimetype scd application/x-msschedule
	dict set mimetype sct text/scriptlet
	dict set mimetype setpay application/set-payment-initiation
	dict set mimetype setreg application/set-registration-initiation
	dict set mimetype sh application/x-sh
	dict set mimetype shar application/x-shar
	dict set mimetype sit application/x-stuffit
	dict set mimetype snd audio/basic
	dict set mimetype spc application/x-pkcs7-certificates
	dict set mimetype spl application/futuresplash
	dict set mimetype src application/x-wais-source
	dict set mimetype sst application/vnd.ms-pkicertstore
	dict set mimetype stl application/vnd.ms-pkistl
	dict set mimetype stm text/html
	dict set mimetype svg image/svg+xml
	dict set mimetype sv4cpio application/x-sv4cpio
	dict set mimetype sv4crc application/x-sv4crc
	dict set mimetype swf application/x-shockwave-flash
	dict set mimetype t application/x-troff
	dict set mimetype tar application/x-tar
	dict set mimetype tcl application/x-tcl
	dict set mimetype tex application/x-tex
	dict set mimetype texi application/x-texinfo
	dict set mimetype texinfo application/x-texinfo
	dict set mimetype tgz application/x-compressed
	dict set mimetype tif image/tiff
	dict set mimetype tiff image/tiff
	dict set mimetype tr application/x-troff
	dict set mimetype trm application/x-msterminal
	dict set mimetype tsv text/tab-separated-values
	dict set mimetype txt text/plain
	dict set mimetype uls text/iuls
	dict set mimetype ustar application/x-ustar
	dict set mimetype vcf text/x-vcard
	dict set mimetype vrml x-world/x-vrml
	dict set mimetype wav audio/x-wav
	dict set mimetype wcm application/vnd.ms-works
	dict set mimetype wdb application/vnd.ms-works
	dict set mimetype wks application/vnd.ms-works
	dict set mimetype wmf application/x-msmetafile
	dict set mimetype wps application/vnd.ms-works
	dict set mimetype wri application/x-mswrite
	dict set mimetype wrl x-world/x-vrml
	dict set mimetype wrz x-world/x-vrml
	dict set mimetype xaf x-world/x-vrml
	dict set mimetype xbm image/x-xbitmap
	dict set mimetype xla application/vnd.ms-excel
	dict set mimetype xlc application/vnd.ms-excel
	dict set mimetype xlm application/vnd.ms-excel
	dict set mimetype xls application/vnd.ms-excel
	dict set mimetype xlt application/vnd.ms-excel
	dict set mimetype xlw application/vnd.ms-excel
	dict set mimetype xof x-world/x-vrml
	dict set mimetype xpm image/x-xpixmap
	dict set mimetype xwd image/x-xwindowdump
	dict set mimetype z application/x-compress
	dict set mimetype zip application/zip
}
proc lookup_mimetype {filename} {
	set proc_name lookup_mimetype
	
	set ext [file extension $filename]
	if {[string length $ext] > 1} {
		set ext [string range $ext 1 end]
	}
	if [catch {set mimename [dict get $::mimetype $ext]}] {
		set mimename unknown
	}
	return $mimename
}
proc send_ses {msg_to msg_subject msg_body attach_list} {
	set proc_name send_ses
	
	output "in send_email, $msg_to, $msg_subject, $msg_body, $attach_list"
	#set ::tcloud::debug 1

	$::SES call_aws ses {} SendEmail [list Destination.ToAddresses.member.1 $msg_to Message.Subject.Data test Message.Body.Text.Data $msg_body Source patrick.dunnigan@centivia.com]
}

proc send_email {msg_to msg_subject msg_body attach_list} {
	set proc_name send_email
	set attach_list2 ""


	output "in send_email, $msg_body, $attach_list"
	if {[string match -nocase "*<html>*" $msg_body]} {
		set body [mime::initialize -canonical text/html -string $msg_body]
	} else {
		set body [mime::initialize -canonical "text/plain" -string $msg_body]
	}
	if {[llength $attach_list] > 0} {
		foreach {filename file_id} $attach_list {
		output "attaching $filename with file id >$file_id<, [lookup_mimetype $filename]"
			lappend body [mime::initialize -canonical "[lookup_mimetype $filename]; name=\"$filename\"" -file $file_id]
			#if [file exists $file_id] {
			#	lappend body [mime::initialize -canonical "[lookup_mimetype $filename]; name=\"$filename\"" -file $file_id]
			#	#lappend body [mime::initialize -canonical "[lookup_mimetype $filename]; name=\"[file tail $filename]\"" -file $filename]
			#} else {
			#	set msg_body "NOTE:THE FILE $filename, $file_id WAS NOT AVAILABLE FOR ATTACHMENT\n$msg_body"
			#}
		}
		#set body_mime [mime::initialize -canonical text/html -string $msg_body]
		#lappend body $body_mime
	} else {
		#set body_mime [mime::initialize -canonical text/html -string $msg_body]
		#set body $body_mime
	}
	output $body
	set full_msg [mime::initialize -canonical multipart/mixed -parts $body]
		
	set what [smtp::sendmessage $full_msg \
		-servers $::SMTP_SERVER \
		-ports $::SMTP_PORT \
		-username $::SMTP_USER \
		-password $::SMTP_PASS \
		-header [list From $::SMTP_FROM] \
		-header [list To $msg_to] \
		-header [list Subject $msg_subject] \
		-debug $::DEBUG \
		-queue 1]

output "***********$what******"

	mime::finalize $full_msg
	foreach attach $body {
		mime::finalize $attach
	}
	foreach {filename file_id} $attach_list {
		file delete $file_id
	}
}



proc  get_attachments {msg_id} {
	set proc_name get_attachments

	set sql "select file_name, file_type, file_data from message_data_file where msg_id = $msg_id"
	#output $sql
	::mysql::sel $::CONN $sql
	set ii 0
	set attaches ""
	while {[string length [set row [::mysql::fetch $::CONN]]] > 0} {
		#output $row
		set file_name [lindex $row 0]
		set file_type [lindex $row 1]
		#set file_data [lindex $row 2]
		output "found attachment row file name $file_name, file type $file_type"
		set file_id [file join $::TEMP msg_[clock clicks].tmp]
		set fp [open $file_id w]
		if {"$file_type" == "base64"} {
			puts $fp [::base64::decode [lindex $row 2]]
		} else {
			puts $fp [lindex $row 2]
		}
		close $fp
		lappend attaches $file_name $file_id
	}
	return $attaches

}

proc  check_for_emails {} {
	set proc_name check_for_emails
	set sql "select msg_id, status, msg_to, msg_subject, msg_body, msg_cc, msg_bcc from message where status in (0,1) and ifnull(num_retries,0) <= $::RETRY_ATTEMPTS order by msg_id asc"
	::mysql::sel $::CONN $sql
	set ii 0
	while {[string length [set row [::mysql::fetch $::CONN]]] > 0} {
		incr ii
		set msg_array($ii) $row
		#output $row
	}
	
	if {"$ii" > "0"} {
		output "Processing $ii messages..."
	}
	
	for {set jj 1} {$jj <= $ii} {incr jj} {
		set msg_id [lindex $msg_array($jj) 0]
		set status [lindex $msg_array($jj) 1]
		set msg_to [lindex $msg_array($jj) 2]
		set msg_subject [lindex $msg_array($jj) 3]
		set msg_body [lindex $msg_array($jj) 4]
		set msg_cc [lindex $msg_array($jj) 5]
		set msg_bcc [lindex $msg_array($jj) 6]
		#set attaches [get_attachments $msg_id]
		set attaches ""
		output "Sending msg_id: $msg_id"
		if [catch {send_ses $msg_to $msg_subject $msg_body $attaches} error_msg] {
			output "send email error -> $error_msg" 
			update_status $msg_id 1 $error_msg
		} else {
			update_status $msg_id 2 ""
		}
	}
}

proc update_status {msg_id status err_msg} {
	set proc_name update_status

	regsub -all "(')" $err_msg "''" err_msg
	if {$status == 1} {
		set sql "update message set status = $status, date_time_completed = now(), num_retries = ifnull(num_retries,0) + 1, error_message = '$err_msg' where msg_id = $msg_id"
	} else {
		set sql "update message set status = $status, date_time_completed = now(), num_retries = 0, error_message = '' where msg_id = $msg_id"
	}
	::mysql::exec $::CONN $sql
}

proc get_settings {} {
	
	set ::PREVIOUS_MODE ""
	
	if {[info exists ::MODE]} {
		set ::PREVIOUS_MODE $::MODE
	}

	set sql "select mode_off_on, loop_delay_sec, retry_max_attempts, smtp_server_addr, smtp_server_user, smtp_server_password, smtp_server_port, from_email, from_name from messenger_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [::mysql::fetch $::CONN]
	set ::MODE [lindex $row 0]
	set ::LOOP [lindex $row 1]
	set ::RETRY_ATTEMPTS [lindex $row 2]
	set ::SMTP_SERVER [lindex $row 3]
	set ::SMTP_USER [lindex $row 4]
	set pass [lindex $row 5]
	
	if {"$pass" != ""} {
		set ::SMTP_PASS [decrypt_password $pass $::SITE_KEY]
	} else {
		set ::SMTP_PASS ""
	}
	
	set ::SMTP_PORT [lindex $row 6]
	set smtp_from_email [lindex $row 7]
	set smtp_from_name [lindex $row 8]
	if {"smtp_from_name" == ""} {
		set ::SMTP_FROM "\"$smtp_from_name\"<$smtp_from_name>"
	} else {
		set ::SMTP_FROM $smtp_from_email
	}
	#output "Smtp from address is $::SMTP_FROM"
	
	#did the run mode change? not the first time of course previous_mode will be ""
	if {"$::PREVIOUS_MODE" != "" && "$::PREVIOUS_MODE" != "$::MODE"} {
		output "*** Control Change: Mode is now $::MODE"
	}
        set sql "select admin_email from messenger_settings where id = 1"
	::mysql::sel $::CONN $sql
	set row [::mysql::fetch $::CONN]
	set ::ADMIN_EMAIL [lindex $row 0]

}

proc initialize_process {} {
	### when smtp is supported again, we'll comment out the following lines
	#package require mime
	#package require smtp
	#init_mimetypes
	package require base64
	package require TclOO
	package require tclcloud
	set ::SES [::tclcloud::connection new $::SES_ACCESS_KEY $::SES_SECRET_KEY]
}

proc main_process {} {

	check_for_emails
}
main
