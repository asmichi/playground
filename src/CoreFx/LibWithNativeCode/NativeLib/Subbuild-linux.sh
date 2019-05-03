#!/bin/bash
# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

SCRIPT_DIR=$(dirname $0)

make -C $SCRIPT_DIR RID=linux-x64 CONFIGURATION=Debug OUTROOT=/home/proj
make -C $SCRIPT_DIR RID=linux-x64 CONFIGURATION=Release OUTROOT=/home/proj
