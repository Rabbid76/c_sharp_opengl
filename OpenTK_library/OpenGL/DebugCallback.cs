using System;
using System.Reflection;
using OpenTK.Graphics.OpenGL4; // GL

using System.Runtime.InteropServices;

namespace OpenTK_library.OpenGL
{
    public class DebugCallback
    {
        public DebugCallback()
        {}

        // Callback for OpenGL debug message
        public static void DebugProcCallBack(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string message_str = Marshal.PtrToStringAnsi(message);
            Console.WriteLine(message_str);
        }

        // create end enable debug message callback
        public void Init()
        {
            _debugMessageCallbackInstance = new DebugProc(DebugProcCallBack);
            _hijackCallback(); // see [DebugMessageCallback segfaults upon logging (?) #880](https://github.com/opentk/opentk/issues/880)

            GL.DebugMessageCallback(_debugMessageCallbackInstance, IntPtr.Zero);

            // filter: all debug messages on
            GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);

            // filter: only error messages
            //GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], false);
            //GL.DebugMessageControl(DebugSourceControl.DebugSourceApi, DebugTypeControl.DebugTypeError, DebugSeverityControl.DontCare, 0, new int[0], false);

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug output enabled");
        }

        /// <summary>
        /// [DebugMessageCallback segfaults upon logging (?) #880](https://github.com/opentk/opentk/issues/880)
        /// </summary>
        DebugProc _debugMessageCallbackInstance;
        private delegate void DebugMessageCallbackDelegate([MarshalAs(UnmanagedType.FunctionPtr)] DebugProc proc, IntPtr userParam);
        private void _hijackCallback()
        {
            var type = typeof(GL);
            var entryPoints = (IntPtr[])type.GetField("EntryPoints", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var ep = entryPoints[184]; // I did this for the OpenGL4 namespace, this value might be incorrect for others.
            var d = Marshal.GetDelegateForFunctionPointer<DebugMessageCallbackDelegate>(ep);
            d(_debugMessageCallbackInstance, new IntPtr(0x3005));
        }
    }
}
