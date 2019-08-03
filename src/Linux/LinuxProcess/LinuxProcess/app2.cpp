#include <cstdio>
#include <cstdlib>

int app2(int argc, const char** argv)
{
    for (int i = 0; i < argc; i++)
    {
        std::printf("[%d]:%s\n", i, argv[i]);
    }

    return std::atoi(argv[1]);
}
