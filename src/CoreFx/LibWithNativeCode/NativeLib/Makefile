# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#
# Variables
#

ifeq ($(OUTROOT),)
$(error OUTROOT must be specified)
endif
ifeq ($(RID),)
$(error RID must be specified)
endif
ifeq ($(CONFIGURATION),)
CONFIGURATION = Release
endif

LIBNAME = NativeLib
SRCDIR = ./
OBJDIR = $(OUTROOT)/obj/NativeLib/$(RID)/$(CONFIGURATION)/
BINDIR = $(OUTROOT)/bin/NativeLib/$(RID)/$(CONFIGURATION)/

SRCS = $(addprefix $(SRCDIR),lib.c)
OBJS = $(addprefix $(OBJDIR),$(SRCS:.c=.o))

CC = gcc

CFLAGS = -g -gdwarf -MMD -std=c11 -fPIC -Wextra -Wswitch -Wswitch -Werror
LDFLAGS_SHARED = -Wl,--no-undefined -Wl,-z,relro -Wl,-z,now -Wl,-z,noexecstack -shared -Wl,-soname,lib$(LIBNAME).so -Wl,--version-script=$(SRCDIR)$(LIBNAME).version

ifeq ($(CONFIGURATION),Release)
CFLAGS += -O3
else ifeq ($(CONFIGURATION),Debug)
CFLAGS += -O0 -fno-omit-frame-pointer
else
$(error Unknown CONFIGURATION: $(CONFIGURATION))
endif

#
# Targets
#
.PHONY : all
all: $(BINDIR)lib$(LIBNAME).so

-include $(OBJS:.o=.d)

$(OBJDIR)%.o : $(SRCDIR)%.c
	mkdir -p $(dir $@)
	$(CC) $(CFLAGS) -o $@ -c $<

$(BINDIR)lib$(LIBNAME).so: $(OBJS) $(SRCDIR)$(LIBNAME).version
	mkdir -p $(dir $@)
	$(CC) $(LDFLAGS_SHARED) -o $@ $(OBJS)
