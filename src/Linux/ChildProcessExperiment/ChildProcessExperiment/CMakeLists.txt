cmake_minimum_required(VERSION 3.10)

find_package(Threads REQUIRED)

set(sources
    AncillaryDataSocket.cpp
    AncillaryDataSocket.hpp
    Base.cpp
    Base.hpp
    BinaryReader.hpp
    BinaryWriter.hpp
    Client.cpp
    Client.hpp
    ErrnoExceptions.hpp
    ExactBytesIO.hpp
    main.cpp
    Request.cpp
    Request.hpp
    Service.cpp
    Service.hpp
    SignalHandler.cpp
    SignalHandler.hpp
    Subchannel.cpp
    Subchannel.hpp
    UniqueResource.hpp
    Wrappers.cpp
    Wrappers.hpp
    WriteBuffer.cpp
    WriteBuffer.hpp
)

add_compile_options(
    -Wextra
    -Wswitch
    $<$<CONFIG:Debug>:-fno-omit-frame-pointer>
)

add_executable(ChildProcessExperiment ${sources})
target_link_libraries(ChildProcessExperiment Threads::Threads)
target_compile_features(ChildProcessExperiment PRIVATE cxx_std_17)
