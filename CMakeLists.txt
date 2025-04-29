cmake_minimum_required(VERSION 3.13.0)
project(brdf_zoo LANGUAGES C CXX)
set(CMAKE_CXX_STANDARD 17)

add_executable(brdf_zoo)

target_sources(brdf_zoo PUBLIC
    src/main.cpp
    src/gl.c
    src/stb_image.cpp
)
target_include_directories(brdf_zoo PUBLIC include)
target_link_directories(brdf_zoo PUBLIC lib)
target_link_libraries(brdf_zoo libglfw3.a)