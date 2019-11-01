using System;
using OpenTK.Graphics.OpenGL4; // GL

using System.Runtime.InteropServices;

namespace OpenTK_library
{
    public class GL_DebugCallback
    {
        public GL_DebugCallback()
        {}

        // Callback for OpenGL debug message
        public static void DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string message_str = Marshal.PtrToStringAnsi(message);
            Console.WriteLine(message_str);
        }

        // create end enable debug message callback
        public void Init()
        {
            GL.DebugMessageCallback(DebugProc, IntPtr.Zero);

            // filter: all debug messages on
            GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);

            // filter: only error messages
            //GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], false);
            //GL.DebugMessageControl(DebugSourceControl.DebugSourceApi, DebugTypeControl.DebugTypeError, DebugSeverityControl.DontCare, 0, new int[0], false);

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug output enabled");
        }
    }
}
