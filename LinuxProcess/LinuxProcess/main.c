#define _GNU_SOURCE
#include <stdio.h>
#include <string.h>

int app1(int argc, const char** argv);
int app2(int argc, const char** argv);
int appmain(int argc, const char** argv);

int main(int argc, const char** argv)
{
    if (strcmp(argv[0], "app1") == 0)
    {
        return app1(argc, argv);
    }
    else if (strcmp(argv[0], "app2") == 0)
    {
        return app2(argc, argv);
    }
    else
    {
        return appmain(argc, argv);
    }
}

