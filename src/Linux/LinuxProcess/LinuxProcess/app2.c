#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>

int app2(int argc, const char** argv)
{
    for (int i = 0; i < argc; i++)
    {
        printf("[%d]:%s\n", i, argv[i]);
    }

    return atoi(argv[1]);
}
