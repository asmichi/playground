cmake_minimum_required(VERSION 3.10)

find_package(Threads REQUIRED)

set(sources
    AncillaryDataSocket.cpp
    Base.cpp
    Client.cpp
    main.cpp
    MiscHelpers.cpp
    Request.cpp
    Service.cpp
    SignalHandler.cpp
    Subchannel.cpp
    SocketHelpers.cpp
    WriteBuffer.cpp
)

add_compile_options(
    -Wextra
    -Wswitch
    $<$<CONFIG:Debug>:-fno-omit-frame-pointer>
)

add_executable(ChildProcessExperiment ${sources})
target_link_libraries(ChildProcessExperiment Threads::Threads)
target_compile_features(ChildProcessExperiment PRIVATE cxx_std_17)
