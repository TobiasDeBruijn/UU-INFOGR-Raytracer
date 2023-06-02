# INFOGR Raytracer
Assignment 1

# Students
- Tobias de Bruijn (4714652)

# A note on the debug view
The debug view can be enabled by uncommenting the `#define` on line 2 of Raytracer.cs.
Note that enabling the debug view cripples performance. Enabling the debug view also balloons RAM usage (Up to 5GB in some cases!)

Furthermore, there is a slight issue with translating the cast rays from their 3D World coordinates to the scaled 2D debug view, making it so that some rays
appear to be behind the camera.

Out of all rays cast, a random selection is made every frame to show in the debug view.

## Ray Colors
- Primary rays: red
- Secondary rays: green
- Shadow rays: blue

# Features
- All minimum features were implemented

## Extra features
- Multithreading
    - Every column is rendered on a different thread

# Sources
- https://gamedev.stackexchange.com/a/190058
- https://gamedev.stackexchange.com/a/172357
- https://raytracing.github.io/books/RayTracingInOneWeekend.html
- https://www.youtube.com/watch?v=Qz0KTGYJtUk
- INFOGR 2023 course slides

# License
MIT or Apache-2.0, at your option.