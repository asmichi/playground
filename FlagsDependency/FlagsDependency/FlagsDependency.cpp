#include "pch.h"
#include <cstdio>
#include <memory>
#include <chrono>

// 64 bytes
struct Fuga
{
    int xs[16];
};

extern "C" int sum(int, Fuga*, Fuga*);

const int N = 16 * 1024 * 1024;

int main()
{
    auto* A1 = new Fuga[N];
    auto* A2 = new Fuga[N];

    // Commit allocated regions.
    std::memset(A1, 0, sizeof(Fuga[N]));
    std::memset(A2, 0, sizeof(Fuga[N]));

    typedef std::chrono::high_resolution_clock hrc;
    auto start = hrc::now();

    for (int i = 0; i < 32; i++)
    {
        sum(N, A1, A2);
    }

    long long mills = std::chrono::duration_cast<std::chrono::milliseconds>(hrc::now() - start).count();
    std::printf("%lld ms\n", mills);
}
