// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include "UniqueResource.hpp"
#include <sys/types.h>

extern int g_SignalDataPipeReadEnd;

void SetupSignalHandlers();
