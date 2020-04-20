# TODO

- pixel buffer
  
  Read pixel from buffer by pixel buffer and buffer mapping

- spir-v

- wpf view overlay UI

- fps

## Assamblies

GLWpfControlMSAA

## Framworks

[OpenTK](https://opentk.net/)  
[dwmkerr/sharpgl](https://github.com/dwmkerr/sharpgl)  
[luca-piccioni/OpenGL.Net](https://github.com/luca-piccioni/OpenGL.Net)  

## WPF

[Integrating WPF and Microsoft Kinect SDK with OpenTK](http://igordcard.blogspot.com/2011/12/integrating-wpf-and-kinect-with-opentk.html)
[A faster way to use OpenTk in WPF without Forms dependencies](https://github.com/jayhf/OpenTkControl) 
[Examples of how to using OpenGL via OpenTK in WPF applications](https://github.com/freakinpenguin/OpenTK-WPF)  

### event handling

[Event handling in an MVVM WPF application](https://social.technet.microsoft.com/wiki/contents/articles/18199.event-handling-in-an-mvvm-wpf-application.aspx)  
[Correct use of MVVM and MouseEvent Handling](https://codereview.stackexchange.com/questions/169047/correct-use-of-mvvm-and-mouseevent-handling)  
[Handling Mouse Events in MVVM in WPF](https://stackoverflow.com/questions/24260946/handling-mouse-events-in-mvvm-in-wpf)  
[Handling mouse events in WPF / MVVM using MvvmLight Event Triggers](https://www.technical-recipes.com/2017/handling-mouse-events-in-wpf-mvvm-using-mvvmlight-event-triggers/) 
[Mouse Event Commands for MVVM](https://www.codeproject.com/Tips/478643/Mouse-Event-Commands-for-MVVM)  

## Json

[How to Convert JSON object to Custom C# object?](https://stackoverflow.com/questions/2246694/how-to-convert-json-object-to-custom-c-sharp-object)  
[How to Parse JSON into a C# Object](https://www.codementor.io/andrewbuchan/how-to-parse-json-into-a-c-object-4ui1o0bx8)  

Model builder

## Projects

### 2D game

### Voxel based rendering

### Shadow

### Light model

### SSAO / HBAO

### Mincraft

- [Instancing](https://www.khronos.org/opengl/wiki/Vertex_Rendering#Instancing) 
- Texture 2d array N * 6*1 tile textures
- SSBO with cube translations and cube types `ivec`? encode coordinates and types in int or ivec2?
- 3 dimensional array which maps to index in SSBO

If a cube is removed, then remove the entry from the SSBO. Reorganize it and move the last entry to the removed entry.
Is a map from SSBO index to 3d array required?

### Control

Angular mobile application (controller) which controls WPF desktop application

- bluetooth
- web-server

## Render

Use compute shader to visualize rendering (Peter Shirley) 

# Ideas

## Overlay menue

- "spin" in
- json configuration

## Opengl stereo interleaved

Glasses? red green
 
https://hub.packtpub.com/rendering-stereoscopic-3d-models-using-opengl/  
http://paulbourke.net/stereographics/vpac/opengl.html  
https://stackoverflow.com/questions/8918945/how-to-do-stereoscopic-3d-with-opengl-on-gtx-560-and-later  
http://www.gali-3d.com/archive/articles/StereoOpenGL/StereoscopicOpenGLTutorial.php  

## Inirect rendering

[Indirect rendering](https://www.khronos.org/opengl/wiki/Vertex_Rendering#Indirect_rendering)  

