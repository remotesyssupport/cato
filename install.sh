#!/bin/bash
DEPLOY_DIR=/opt/cato
COMPONENT=3
SILENT=0
if [ ! -n "$1" ]
then
	read -p "Enter a target directory. (default: $DEPLOY_DIR): " dir
	if [ "$dir" != "" ]; then
		DEPLOY_DIR=$dir
	fi	
	echo ""
	echo "Components, 1 = web, 2 = services, 3 = all"
	read -p "Enter a component to install. (1,2,3; default: 3): " dir
	if [ "$dir" = "" ]; then
		COMPONENT=3
	else
		COMPONENT=$dir
	fi
else
	while getopts ":c:t:s" opt
	do
	    case "$opt" in
	      c)  COMPONENT="$OPTARG";;
	      s)  SILENT=1;;
	      t)  DEPLOY_DIR="$OPTARG";;
	      \?)               # unknown flag
		  echo >&2 \
		  "usage: $0 [-s] [-t targetpath] [-c component (web|service|all)]"
		  echo "        -s      silent operation" 
		  echo "        -t      path to install to" 
		  echo "        -c      component to install, services only, web only or both" 
		  exit 1;;
	    esac
	done
	shift `expr $OPTIND - 1`
fi
if [ $SILENT = 0 ]
then 
	echo "target = $DEPLOY_DIR, installing component = $COMPONENT, silent = $SILENT"
	echo "Installing to $DEPLOY_DIR ..."
fi

mkdir -p $DEPLOY_DIR
mkdir -p $DEPLOY_DIR/web
mkdir -p $DEPLOY_DIR/services
mkdir -p $DEPLOY_DIR/conf
rsync -aq conf/default.cato.conf $DEPLOY_DIR/conf
rsync -aq conf/setup_conf $DEPLOY_DIR/conf
rsync -aq services/lib/catocrypt/* $DEPLOY_DIR/conf/catocrypt
rsync -aq database/* $DEPLOY_DIR/conf/data

if [ "$COMPONENT" = "1" -o "$COMPONENT" = "3" ]
then

	if [ $SILENT = 0 ]
	then 
		echo "Copying web files..."
	fi
	#copy pages WITHOUT the .cs files
	rsync -aq --exclude=*.cs web/pages/* $DEPLOY_DIR/web/pages

	#copy all images, script and style
	rsync -aq web/images/* $DEPLOY_DIR/web/images
	rsync -aq web/script/* $DEPLOY_DIR/web/script
	rsync -aq web/style/* $DEPLOY_DIR/web/style

	#just the dll's not the extras
	rsync -q web/bin/*.dll $DEPLOY_DIR/web/bin/

	#explicit local files
	rsync -q web/*.aspx $DEPLOY_DIR/web/
	rsync -q web/*.htm $DEPLOY_DIR/web/
	rsync -q web/Web.config $DEPLOY_DIR/web/
	rsync -q web/NOTICE $DEPLOY_DIR/web/
	rsync -q web/LICENSE $DEPLOY_DIR/web/

	if [ $SILENT = 0 ]
	then 
		echo "Creating web link to conf dir..."
	fi
	ln -s $DEPLOY_DIR/conf $DEPLOY_DIR/web/conf
fi
if [ "$COMPONENT" = "2" -o "$COMPONENT" = "3" ]
then

	if [ $SILENT = 0 ]
	then 
		echo "Copying services files..."
	fi

	#copy all images, script and style
	rsync -aq services/bin/* $DEPLOY_DIR/services/bin
	rsync -aq services/lib/* $DEPLOY_DIR/services/lib

	if [ $SILENT = 0 ]
	then 
		echo "Creating services link to conf dir..."
	fi
	ln -s $DEPLOY_DIR/conf $DEPLOY_DIR/services/conf
fi

if [ $SILENT = 0 ]
then 
	echo "... Done"
fi








