# BRDF Zoo

This program showcases 3 different diffuse and 3 different specular shading models.

### Diffuse
- Lambertian
- Oren-Nayar
- Disney

### Specular
- Phong
- Blinn-Phong
- Cook-Torrance

## Build Instructions

(The libraries for this program were compiled using MinGW on Windows, so a MinGW compiler is necessary.)

Use the provided CMakeLists.txt file to generate the necessary files:

`cmake CMakeLists.txt -D CMAKE_C_COMPILER="c:\msys64\ucrt64\bin\gcc.exe" -D CMAKE_CXX_COMPILER="c:\msys64\ucrt64\bin\g++.exe" -G "MinGW Makefiles" -B build/`

Then build the program:

`cmake --build build/`

The output executable will be stored in the `build/` directory.

## Executable Parameters

This program has only one optional parameter, being scene selection. Enter the scene number desired to be viewed (1 or 2) after the executable file name in the command line.

### Run Scene 1
`./build/brdf_zoo.exe` or `./build/brdf_zoo.exe 1`

### Run Scene 2
`./build/brdf_zoo.exe 2`

## Camera Controls
| Key | Function |
| --- | --- |
| Esc | Stop capturing inputs |
| Left Mouse Click (in program window) | Capture inputs |
| W/A/S/D | Move camera forward/left/back/right along XZ-plane |
| Space/Left Shift | Move camera up/down along Y-axis |
| Mouse Move | Rotate camera |
| Scroll Wheel | Zoom in/out |

## Model Controls

### Diffuse Models
| Key | Model |
| --- | --- |
| L | Lambertian |
| O | Oren-Nayar |
| K | Disney |

### Specular Models
| Key | Model |
| --- | --- |
| 0 | Phong |
| 1 | Blinn-Phong |
| 2 | Cook-Torrance |