# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#
# CMakeLists.txt for building ChildProcess.Native (not including .NET projects.)
#

cmake_minimum_required(VERSION 3.10)

project("AsmichiChildProcessNative" CXX)

set(libName "AsmichiChildProcess")
set(helperName "AsmichiChildProcessHelper")
set(mainName "ChildProcessExperiment")
set(versionScript "${CMAKE_CURRENT_SOURCE_DIR}/AsmichiChildProcess.version")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_BUILD_WITH_INSTALL_RPATH true)
set(CMAKE_INSTALL_RPATH "\$ORIGIN")

find_package(Threads REQUIRED)

set(libSources
    AncillaryDataSocket.cpp
    Base.cpp
    ChildProcessState.cpp
    Globals.cpp
    Exports.cpp
    HelperMain.cpp
    MiscHelpers.cpp
    Request.cpp
    Service.cpp
    SignalHandler.cpp
    Subchannel.cpp
    SocketHelpers.cpp
    WriteBuffer.cpp
)

set(mainSources
    ${libSources}
    Client.cpp
    main.cpp
)

add_compile_options(
    -Wextra
    -Wswitch
    $<$<OR:$<CONFIG:Release>,$<CONFIG:RelWithDebInfo>>:-O3>
    $<$<CONFIG:Debug>:-O1>
    $<$<CONFIG:Debug>:-fno-omit-frame-pointer>
    $<$<CONFIG:Debug>:-DENABLE_TRACE_DEBUG>
    $<$<CONFIG:Debug>:-DENABLE_TRACE_INFO>
    $<$<CONFIG:Debug>:-DENABLE_TRACE_ERROR>
)

add_executable(${mainName} ${mainSources})
target_compile_features(${mainName} PRIVATE cxx_std_17)
target_link_libraries(${mainName}
    Threads::Threads
    ${CMAKE_DL_LIBS}
)
