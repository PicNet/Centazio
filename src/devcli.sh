#!/bin/bash
CURR_DIR=$(pwd)
SLN_DIR="../centazio3"

dotnet tool uninstall --local Centazio.Cli
rm -rf ./config/
cd $SLN_DIR
rm packages/*; src/bump.sh; dotnet pack -v detailed -c Release -o packages
cd $CURR_DIR
cp $SLN_DIR/settings.* $CURR_DIR
dotnet new tool-manifest --force
dotnet tool install --prerelease --local --add-source ../centazio3/packages/ Centazio.Cli
dotnet centazio "$@"
