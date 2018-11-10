#define WIN32_LEAN_AND_MEAN
#define NOMINMAX

#include <Windows.h>
#include <cstdio>
#include <cstdlib>
#include <ratio>
#include <chrono>

namespace
{

void AbortOnWin32Error()
{
    std::printf("LastError: %d\n", GetLastError());
    std::exit(1);
}

class stopwatch
{
public:
    stopwatch()
    {
        start_ = hrc::now();
    }

    double GetElapsedMicroseconds()
    {
        auto ts = std::chrono::duration_cast<std::chrono::microseconds>(hrc::now() - start_);
        return static_cast<double>(ts.count());
    }

private:
    using hrc = std::chrono::high_resolution_clock;
    hrc::time_point start_;
};

const size_t AllocationGranularity = 0x10000;

static void MeasureVirtualQuery(HANDLE hProcess)
{

    std::printf("VirtualQueryEx time\n");
    std::printf("     BaseAddress,      RegionSize,State,Type :          time / call\n");

    const int N = 1000;
    MEMORY_BASIC_INFORMATION mbi{};
    PBYTE pCurrent = nullptr;
    while (true)
    {
        stopwatch sw{};

        for (int i = 0; i < N; i++)
        {
            if (VirtualQueryEx(hProcess, pCurrent, &mbi, sizeof(mbi)) == 0)
            {
                return;
            }
        }

        std::printf(
            "%p,%16llx,%5x,%4x : %10.4f us / call\n",
            mbi.BaseAddress,
            mbi.RegionSize,
            mbi.State >> 12,
            mbi.Type >> 16,
            sw.GetElapsedMicroseconds() / N);

        pCurrent = (PBYTE)mbi.BaseAddress + mbi.RegionSize;
    }
}

} // namespace

int main()
{
    STARTUPINFOW si{};
    PROCESS_INFORMATION pi{};
    wchar_t cmdLine[] = L"cmd /c echo.";

    if (!CreateProcessW(nullptr, cmdLine, nullptr, nullptr, FALSE, CREATE_SUSPENDED, nullptr, nullptr, &si, &pi))
    {
        AbortOnWin32Error();
    }

    MeasureVirtualQuery(pi.hProcess);

    ResumeThread(pi.hThread);
    CloseHandle(pi.hThread);

    WaitForSingleObject(pi.hProcess, INFINITE);
    CloseHandle(pi.hProcess);

    return 0;
}

/* Win10 Pro 1803 (Build 17134.376)

VirtualQueryEx time
     BaseAddress,      RegionSize,State,Type :          time / call
0000000000000000,        7ffe0000,   10,   0 :     0.8830 us / call
000000007FFE0000,            1000,    1,   2 :     3.0270 us / call
000000007FFE1000,            1000,   10,   0 :     0.8790 us / call
000000007FFE2000,            1000,    1,   2 :     2.9730 us / call
000000007FFE3000,      e287c2d000,   10,   0 :     0.8840 us / call
000000E307C10000,            1000,    2,   2 :     1.3820 us / call
000000E307C11000,            3000,    1,   2 :     8.1130 us / call
000000E307C14000,           fc000,    1,   2 :     5.3770 us / call
000000E307D10000,           f0000,   10,   0 :     0.4430 us / call
000000E307E00000,           64000,    2,   2 :     1.4990 us / call
000000E307E64000,            3000,    1,   2 :     4.3650 us / call
000000E307E67000,          199000,    2,   2 :     4.2990 us / call
000000E308000000,     197ede10000,   10,   0 :     0.4410 us / call
0000027AF5E10000,           20000,    1,   2 :     0.7850 us / call
0000027AF5E30000,           19000,    1,   4 :     0.9080 us / call
0000027AF5E49000,            7000,   10,   0 :     0.4300 us / call
0000027AF5E50000,            4000,    1,   4 :     0.6950 us / call
0000027AF5E54000,            c000,   10,   0 :     0.4440 us / call
0000027AF5E60000,            1000,    1,   4 :     0.6000 us / call
0000027AF5E61000,            f000,   10,   0 :     0.4430 us / call
0000027AF5E70000,            1000,    1,   2 :     0.5970 us / call
0000027AF5E71000,    7b7a48f8f000,   10,   0 :     0.4420 us / call
00007DF53EE00000,           23000,    1,   4 :     1.9580 us / call
00007DF53EE23000,            d000,   10,   0 :     0.4470 us / call
00007DF53EE30000,         1dbc000,    2,   4 :  3372.2790 us / call <- taking 3.37 ms / call !!!
00007DF540BEC000,            4000,    1,   4 :    38.0790 us / call
00007DF540BF0000,     1ffd896d000,    2,   4 :   553.4680 us / call
00007FF51955D000,            2000,    1,   4 :     4.9520 us / call
00007FF51955F000,        12025000,    2,   4 :   109.4380 us / call
00007FF52B584000,          d02000,    1,   4 :   809.9590 us / call
00007FF52C286000,            8000,    1,   4 :     1.4670 us / call
00007FF52C28E000,        12ba2000,    2,   4 :   151.9190 us / call
00007FF53EE30000,       15dd20000,   10,   0 :     0.4380 us / call
00007FF69CB50000,            1000,    1, 100 :     1.4860 us / call
00007FF69CB51000,           2d000,    1, 100 :     1.6120 us / call
00007FF69CB7E000,            b000,    1, 100 :     1.3630 us / call
00007FF69CB89000,           1c000,    1, 100 :     1.2500 us / call
00007FF69CBA5000,            3000,    1, 100 :     0.7360 us / call
00007FF69CBA8000,            1000,    1, 100 :     0.8990 us / call
00007FF69CBA9000,            a000,    1, 100 :     0.8280 us / call
00007FF69CBB3000,       4b49dd000,   10,   0 :     0.4490 us / call
00007FFB51590000,            1000,    1, 100 :     8.6420 us / call
00007FFB51591000,          10f000,    1, 100 :     7.3040 us / call
00007FFB516A0000,           46000,    1, 100 :     4.1660 us / call
00007FFB516E6000,            b000,    1, 100 :     2.4400 us / call
00007FFB516F1000,            e000,    1, 100 :     0.9890 us / call
00007FFB516FF000,            1000,    1, 100 :     1.5250 us / call
00007FFB51700000,            3000,    1, 100 :     7.7890 us / call
00007FFB51703000,           6e000,    1, 100 :     7.6280 us / call
00007FFB51771000,       4ae87f000,   10,   0 :     0.8920 us / call

*/

/* Win10 Pro 1809 (Build 17763.55)

VirtualQueryEx time
     BaseAddress,      RegionSize,State,Type :          time / call
0000000000000000,        7ffe0000,   10,   0 :     0.9020 us / call
000000007FFE0000,            1000,    1,   2 :     1.3900 us / call
000000007FFE1000,            1000,   10,   0 :     0.8930 us / call
000000007FFE2000,            1000,    1,   2 :     1.3600 us / call
000000007FFE3000,      258de1d000,   10,   0 :     0.8990 us / call
000000260DE00000,           c4000,    2,   2 :     5.3470 us / call
000000260DEC4000,            3000,    1,   2 :     1.4470 us / call
000000260DEC7000,          139000,    2,   2 :     7.5410 us / call
000000260E000000,            1000,    2,   2 :     1.3710 us / call
000000260E001000,            3000,    1,   2 :     1.3530 us / call
000000260E004000,           fc000,    1,   2 :     8.2190 us / call
000000260E100000,     146148a0000,   10,   0 :     0.4520 us / call
0000016C229A0000,           20000,    1,   2 :     0.8230 us / call
0000016C229C0000,           1a000,    1,   4 :     0.6320 us / call
0000016C229DA000,            6000,   10,   0 :     0.4530 us / call
0000016C229E0000,            4000,    1,   4 :     0.6170 us / call
0000016C229E4000,            c000,   10,   0 :     0.4520 us / call
0000016C229F0000,            1000,    1,   4 :     0.6070 us / call
0000016C229F1000,            f000,   10,   0 :     0.4530 us / call
0000016C22A00000,            2000,    1,   2 :     0.6190 us / call
0000016C22A02000,    7c898a24e000,   10,   0 :     0.4560 us / call
00007DF5ACC50000,            1000,    1,   4 :     1.6350 us / call
00007DF5ACC51000,            f000,   10,   0 :     0.4400 us / call
00007DF5ACC60000,           23000,    1,   4 :     1.6740 us / call
00007DF5ACC83000,            d000,   10,   0 :     0.4430 us / call
00007DF5ACC90000,         1dc0000,    2,   4 :    38.3170 us / call
00007DF5AEA50000,            1000,    1,   4 :    18.7190 us / call
00007DF5AEA51000,     1ffd8805000,    2,   4 :    27.3670 us / call
00007FF587256000,            3000,    1,   4 :     1.4570 us / call
00007FF587259000,         b51e000,    2,   4 :    29.3520 us / call
00007FF592777000,          f72000,    1,   4 :     7.6980 us / call
00007FF5936E9000,            8000,    1,   4 :     3.2420 us / call
00007FF5936F1000,        1959f000,    2,   4 :    12.3250 us / call
00007FF5ACC90000,        ea520000,   10,   0 :     0.9230 us / call
00007FF6971B0000,            1000,    1, 100 :     1.4630 us / call
00007FF6971B1000,           2f000,    1, 100 :     1.6710 us / call
00007FF6971E0000,            b000,    1, 100 :     1.8310 us / call
00007FF6971EB000,           1c000,    1, 100 :     1.6040 us / call
00007FF697207000,            3000,    1, 100 :     1.5080 us / call
00007FF69720A000,            1000,    1, 100 :     1.4030 us / call
00007FF69720B000,            a000,    1, 100 :     1.7110 us / call
00007FF697215000,       31243b000,   10,   0 :     1.0510 us / call
00007FF9A9650000,            1000,    1, 100 :     1.5190 us / call
00007FF9A9651000,          117000,    1, 100 :     2.9510 us / call
00007FF9A9768000,           47000,    1, 100 :     1.8320 us / call
00007FF9A97AF000,            b000,    1, 100 :     1.3870 us / call
00007FF9A97BA000,            e000,    1, 100 :     1.3740 us / call
00007FF9A97C8000,            1000,    1, 100 :     1.5220 us / call
00007FF9A97C9000,            3000,    1, 100 :     1.4760 us / call
00007FF9A97CC000,           71000,    1, 100 :     5.1430 us / call
00007FF9A983D000,       6567b3000,   10,   0 :     0.4440 us / call
*/
