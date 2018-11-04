#define _GNU_SOURCE
#include <stdio.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <fcntl.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include "const.h"

int app1(int argc, const char** argv)
{
    int index = atoi(argv[1]);
    fcntl(MyPipeFd, F_SETFD, O_CLOEXEC);

    for (int i = 0; i < argc; i++)
    {
        printf("[%d]:%s\n", i, argv[i]);
    }

    sleep(CHILD_COUNT - index);

    if (index == 1)
    {
        close(MyPipeFd);
    }
    else
    {
        write(MyPipeFd, &index, sizeof(int));
    }

    return index;
}
