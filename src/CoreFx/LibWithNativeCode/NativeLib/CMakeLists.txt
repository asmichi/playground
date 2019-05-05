cmake_minimum_required(VERSION 3.10)

project("NativeLib" C)

set(MYLIBNAME "NativeLib")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

add_library(${MYLIBNAME} SHARED lib.c)
target_compile_features(${MYLIBNAME} PRIVATE c_std_99)
if(CMAKE_C_COMPILER_ID STREQUAL "MSVC")
    target_compile_options(${MYLIBNAME} PRIVATE
        /EHsc
        /fp:precise
        /Gm-
        /GS
        /permissive-
        /W3
        /Zc:forScope
        /Zc:inline
        /Zc:wchar_t
        /Zi
    )
    set_target_properties(${MYLIBNAME} PROPERTIES PREFIX "lib")
else()
    target_compile_options(${MYLIBNAME} PRIVATE
        -Wextra
        -Wswitch
        $<$<CONFIG:Debug>:-fno-omit-frame-pointer>
    )
    set_target_properties(${MYLIBNAME} PROPERTIES LINK_FLAGS
        "-Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/NativeLib.version -Wl,--no-undefined"
    )
endif()
