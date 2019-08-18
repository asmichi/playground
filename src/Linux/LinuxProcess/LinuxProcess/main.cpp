#include <cstdio>
#include <cstring>

int app1(int argc, const char** argv);
int app2(int argc, const char** argv);
int appmain(int argc, const char** argv);

namespace
{
    struct SubCommandDefinition
    {
        const char* Name;
        int (*Func)(int, const char**);
    };

    static struct SubCommandDefinition SubCommandDefinitions[] = {
        {"app1", app1},
        {"app2", app1},
    };
} // namespace

int main(int argc, const char** argv)
{
    for (const auto& d : SubCommandDefinitions)
    {
        if (std::strcmp(argv[0], d.Name) == 0)
        {
            return (d.Func)(argc, argv);
        }
    }

    return appmain(argc, argv);
}
