cmake_minimum_required(VERSION 3.14)
project(gloo_native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# ─── Find Gloo ──────────────────────────────────────────────────────
find_package(gloo REQUIRED)

# ─── Optional InfiniBand support ────────────────────────────────────
option(GLOO_USE_IBVERBS "Enable InfiniBand (ibverbs) transport" OFF)

# ─── Build shared library ──────────────────────────────────────────
add_library(gloo_native SHARED gloo_native.cpp)

target_include_directories(gloo_native PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

target_compile_definitions(gloo_native PRIVATE GLOO_NATIVE_EXPORTS)

target_link_libraries(gloo_native PRIVATE gloo::gloo)

if(GLOO_USE_IBVERBS)
    target_compile_definitions(gloo_native PRIVATE GLOO_USE_IBVERBS)
    find_library(IBVERBS_LIBRARY ibverbs)
    if(IBVERBS_LIBRARY)
        target_link_libraries(gloo_native PRIVATE ${IBVERBS_LIBRARY})
    else()
        message(WARNING "ibverbs library not found. InfiniBand support will not be available.")
    endif()
endif()

# ─── Install rules ──────────────────────────────────────────────────
install(TARGETS gloo_native
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin
    ARCHIVE DESTINATION lib
)

install(FILES gloo_native.h DESTINATION include)
