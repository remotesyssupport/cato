#!/bin/bash

DEPLOY_DIR="../deploy"
echo "$DEPLOY_DIR"

rm -rf $DEPLOY_DIR

mkdir -p $DEPLOY_DIR

tar --exclude=".svn" --exclude="*.cs" -cvf foo.tar *

mv foo.tar $DEPLOY_DIR

cd $DEPLOY_DIR

tar -xvf foo.tar

rm *.csproj
rm *.sln
rm foo.tar
rm makedist.sh
rm *.userprefs
rm BuildAll.bat
rm GenerateAppGlobals.bat
rm MakeDistribution.bat

tar -cvf dist.tar *


