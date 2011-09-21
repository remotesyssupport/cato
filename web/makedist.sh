#!/bin/bash

CONFDIR=$(cd ../conf && pwd)

read -p "Enter a target directory. (default: /opt/cato/web): " dir
if [ "$dir" = "" ]; then
  DEPLOY_DIR="/opt/cato/web"
else
  DEPLOY_DIR=$dir
fi

echo "Installing to $DEPLOY_DIR ..."

echo "Cleaning up..."
rm -rf $DEPLOY_DIR
mkdir -p $DEPLOY_DIR

echo "Copying files..."
#copy pages WITHOUT the .cs files
rsync -aq --exclude=*.cs pages/* $DEPLOY_DIR/pages

#copy all images, script and style
rsync -aq images/* $DEPLOY_DIR/images
rsync -aq script/* $DEPLOY_DIR/script
rsync -aq style/* $DEPLOY_DIR/style

#just the dll's not the extras
rsync -q bin/*.dll $DEPLOY_DIR/bin/

#explicit local files
rsync -q *.aspx $DEPLOY_DIR/
rsync -q *.htm $DEPLOY_DIR/
rsync -q Web.config $DEPLOY_DIR/
rsync -q NOTICE $DEPLOY_DIR/
rsync -q LICENSE $DEPLOY_DIR/

echo "Creating configuration link to $CONFDIR ..."
ln -s $CONFDIR $DEPLOY_DIR/conf

echo "... Done"








