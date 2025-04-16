cmake_minimum_required(VERSION 3.13.0)
project(brdf_zoo_multipoint LANGUAGES C CXX)
set(CMAKE_CXX_STANDARD 17)

add_executable(brdf_zoo_multipoint)

target_sources(brdf_zoo_multipoint PUBLIC
    src/main.cpp
    src/gl.c
    src/stb_image.cpp
)
target_include_directories(brdf_zoo_multipoint PUBLIC include)
target_link_directories(brdf_zoo_multipoint PUBLIC lib)
target_link_libraries(brdf_zoo_multipoint libglfw3.a libassimpd.a)