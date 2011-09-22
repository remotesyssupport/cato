#!/bin/bash

echo "Cleaning the Solution..."

xbuild /target:Clean CloudSidekick.sln

echo "Building..."

xbuild /p:configuration=Release CloudSidekick.sln

echo "Done."
