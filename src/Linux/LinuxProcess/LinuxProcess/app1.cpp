#include <cerrno>
#include <cstdio>
#include <cstdlib>
#include <fcntl.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>
#include "const.hpp"

int app1(int argc, const char** argv)
{
    int index = std::atoi(argv[1]);
    fcntl(MyPipeFd, F_SETFD, O_CLOEXEC);

    for (int i = 0; i < argc; i++)
    {
        std::printf("[%d]:%s\n", i, argv[i]);
    }

    sleep(ChildCount - index);

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
