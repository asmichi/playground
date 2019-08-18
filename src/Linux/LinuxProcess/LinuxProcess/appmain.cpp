#include "const.hpp"
#include <cerrno>
#include <cstdio>
#include <cstdlib>
#include <fcntl.h>
#include <sys/epoll.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>

struct ChildState
{
    int pid;
    int readPipe;
};

static struct ChildState g_States[ChildCount];

bool SpawnChild(struct ChildState* state, int index)
{
    int pipefd[2];
    if (pipe2(pipefd, O_CLOEXEC) == -1)
    {
        perror("pipe2");
        return false;
    }

    char arg1[3];
    std::sprintf(arg1, "%d", index);
    char appName[] = "app1";
    char* args[] = {appName, arg1, NULL};
    int childPid = fork();
    if (childPid == -1)
    {
        perror("fork");
        return false;
    }
    else if (childPid == 0)
    {
        // child
        if (pipefd[1] == MyPipeFd)
        {
            if (fcntl(MyPipeFd, F_SETFD, 0) == -1)
            {
                perror("fcntl");
                _exit(1);
            }
        }
        else
        {
            if (dup2(pipefd[1], MyPipeFd) == -1)
            {
                perror("dup2");
                _exit(1);
            }
        }
        execvpe(MyProgramName, args, environ);
        perror("execve");
        _exit(1);
    }
    else
    {
        // parent
        close(pipefd[1]);
        state->readPipe = pipefd[0];
        state->pid = childPid;
        std::printf("spawned pid %d\n", state->pid);

        return true;
    }
}

int appmain(int argc, const char** argv)
{
    // fork children, then epoll pipes
    for (int i = 0; i < ChildCount; i++)
    {
        if (!SpawnChild(&g_States[i], i))
        {
            std::printf("failed\n");
            return 1;
        }
    }

    int epfd = epoll_create1(EPOLL_CLOEXEC);
    if (epfd == -1)
    {
        perror("epoll_create");
        return 1;
    }

    for (int i = 0; i < ChildCount; i++)
    {
        struct ChildState* pState = &g_States[i];
        struct epoll_event event;
        event.events = EPOLLIN;
        event.data.ptr = pState;
        epoll_ctl(epfd, EPOLL_CTL_ADD, pState->readPipe, &event);
        std::printf("registered State %p (readPipe) %d\n", pState, pState->readPipe);
    }

    int remainingProcs = ChildCount;
    while (remainingProcs > 0)
    {
        struct epoll_event event;
        int ret;
        while ((ret = epoll_wait(epfd, &event, 1, -1)) == -1 && errno == EINTR)
            ;
        if (ret == -1)
        {
            perror("epoll_wait");
            return 1;
        }

        std::printf("event %x, data %p\n", event.events, event.data.ptr);

        if (event.events == EPOLLIN || event.events == EPOLLHUP)
        {
            struct ChildState* pState = static_cast<ChildState*>(event.data.ptr);

            int value;
            ssize_t readSize = read(pState->readPipe, &value, sizeof(int));
            if (readSize == -1)
            {
                perror("read");
            }
            else if (readSize != 4)
            {
                std::printf("%p: premature end of data.\n", pState);
            }
            else
            {
                int wstatus = 0;
                waitpid(pState->pid, &wstatus, 0);
                std::printf("%d finished with %d\n", value, wstatus);
            }
            remainingProcs--;
            epoll_ctl(epfd, EPOLL_CTL_DEL, pState->readPipe, &event);
        }
    }

    close(epfd);

    return 0;
}
