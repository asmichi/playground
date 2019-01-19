#define _GNU_SOURCE
#include <stdio.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/socket.h>
#include <fcntl.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <stdbool.h>
#include <memory.h>

// TODO: SIGPIPE handling

struct cmsg_int
{
    _Alignas(struct cmsghdr) char control[CMSG_SPACE(sizeof(int))];
};

#define MESSAGE_PIPEFD ((int)0x12345678)

int main(int argc, const char** argv)
{
    int ss[2];
    if (socketpair(AF_UNIX, SOCK_SEQPACKET, 0, ss) != 0)
    {
        perror("socketpair");
        return 1;
    }

    int childPid = fork();
    if (childPid == -1)
    {
        perror("fork");
        return 1;
    }
    else if (childPid == 0)
    {
        // child
        int sock = ss[1];
        close(ss[0]);

        int mymsg = 0;
        struct iovec iov;
        struct msghdr msg;
        struct cmsg_int cmsg;

        iov.iov_base = &mymsg;
        iov.iov_len = sizeof(mymsg);
        msg.msg_control = &cmsg.control;
        msg.msg_controllen = sizeof(cmsg.control);
        msg.msg_iov = &iov;
        msg.msg_iovlen = 1;

        ssize_t r = recvmsg(sock, &msg, MSG_CMSG_CLOEXEC);
        if (r == -1)
        {
            perror("recvmsg");
            return 1;
        }
        if (r == 0)
        {
            puts("child: premature end of communication");
            return 1;
        }
        if (mymsg != MESSAGE_PIPEFD)
        {
            printf("child: unknown message : %d\n", mymsg);
            return 1;
        }

        struct cmsghdr* pcmsghdr = CMSG_FIRSTHDR(&msg);
        if (pcmsghdr == NULL)
        {
            puts("child: cmsg expected");
            return 1;
        }
        if (pcmsghdr->cmsg_len != CMSG_LEN(sizeof(int))
            || pcmsghdr->cmsg_level != SOL_SOCKET
            || pcmsghdr->cmsg_type != SCM_RIGHTS)
        {
            puts("child: unexpected cmsg");
            return 1;
        }

        int childPipe = 0;
        memcpy(&childPipe, CMSG_DATA(pcmsghdr), sizeof(childPipe));

        int pipeData = 115648;
        ssize_t s = write(childPipe, &pipeData, sizeof(pipeData));
        if (s == -1)
        {
            perror("child: write");
            return 1;
        }

        return 0;
    }
    else
    {
        // parent
        int sock = ss[0];
        close(ss[1]);

        int pipes[2];
        if (pipe2(pipes, O_CLOEXEC) != 0)
        {
            perror("parent: pipe2");
            return 1;
        }

        int parentPipe = pipes[0];
        int childPipe = pipes[1];

        int mymsg = MESSAGE_PIPEFD;
        struct iovec iov;
        struct msghdr msg;
        struct cmsg_int cmsg;

        iov.iov_base = &mymsg;
        iov.iov_len = sizeof(mymsg);

        msg.msg_control = &cmsg.control;
        msg.msg_controllen = sizeof(cmsg.control);
        msg.msg_iov = &iov;
        msg.msg_iovlen = 1;
        msg.msg_name = NULL;
        msg.msg_namelen = 0;

        struct cmsghdr* pcmsghdr = CMSG_FIRSTHDR(&msg);
        pcmsghdr->cmsg_len = CMSG_LEN(sizeof(int));
        pcmsghdr->cmsg_level = SOL_SOCKET;
        pcmsghdr->cmsg_type = SCM_RIGHTS;
        memcpy(CMSG_DATA(pcmsghdr), &childPipe, sizeof(childPipe));

        ssize_t s = sendmsg(sock, &msg, 0);
        if (s == -1)
        {
            perror("parent: sendmsg");
            return 1;
        }

        close(childPipe);

        int pipeData = 0;
        ssize_t r = read(parentPipe, &pipeData, sizeof(pipeData));
        if (r == -1)
        {
            perror("parent: read");
            return 1;
        }
        if (r == 0)
        {
            puts("parent: premature end of communication");
            return 1;
        }

        printf("parent: received: %d\n", pipeData);
        return 0;
    }
}
