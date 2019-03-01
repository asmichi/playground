#define _GNU_SOURCE
#include <stdio.h>
#include <string.h>

int appagent(int argc, const char** argv);
int app1(int argc, const char** argv);
int app2(int argc, const char** argv);
int appmain(int argc, const char** argv);

struct SubCommandDefinition
{
    const char* Name;
    int(*Func)(int, const char**);
};

static struct SubCommandDefinition SubCommandDefinitions[] =
{
    {"app1", app1},
    {"app2", app1},
};


int main(int argc, const char** argv)
{
    for (int i = 0; i < sizeof(SubCommandDefinitions) / (sizeof(SubCommandDefinitions[0])); i++)
    {
        const struct SubCommandDefinition* d = &SubCommandDefinitions[i];

        if (strcmp(argv[0], d->Name) == 0)
        {
            return (d->Func)(argc, argv);
        }
    }

    return appmain(argc, argv);
}