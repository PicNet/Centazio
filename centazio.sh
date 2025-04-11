#!/bin/bash
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
$SCRIPT_DIR/src/Centazio.Cli/bin/Debug/net9.0/Centazio.Cli $@
