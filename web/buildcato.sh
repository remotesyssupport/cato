#!/bin/bash

echo "Cleaning the Solution..."

xbuild /target:Clean CloudSidekick.sln

echo "Building..."

xbuild CloudSidekick.sln

echo "Done."
