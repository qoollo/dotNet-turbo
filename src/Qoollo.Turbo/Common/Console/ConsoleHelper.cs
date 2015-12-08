using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Additional operations on Console Window 
    /// </summary>
    internal static class ConsoleHelper
    {
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private delegate bool HandlerRoutine(CtrlTypes CtrlType);


        private enum SysMenuElems: uint
        {
            SC_SIZE = 61440,
            SC_MOVE = 61456,
            SC_MINIMIZE = 61472,
            SC_MAXIMIZE = 61488,
            SC_NEXTWINDOW = 61504,
            SC_PREVWINDOW = 61520,
            SC_CLOSE = 61536,
            SC_VSCROLL = 61552,
            SC_HSCROLL = 61568,
            SC_MOUSEMENU = 61584,
            SC_KEYMENU = 61696,
            SC_ARRANGE = 61712,
            SC_RESTORE = 61728,
            SC_TASKLIST = 61744,
            SC_SCREENSAVE = 61760,
            SC_HOTKEY = 61776,
            SC_DEFAULT = 61792,
            SC_MONITORPOWER = 61808,
            SC_CONTEXTHELP = 61824,
            SC_SEPARATOR = 61455,
            SC_ICON = SC_MINIMIZE,
            SC_ZOOM = SC_MAXIMIZE
        }
        

        // ============

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        [DllImport("kernel32")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32")]
        private static extern IntPtr GetSystemMenu(IntPtr hwnd, int bRevert);
        [DllImport("user32")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint itemId, uint uEnable);



        // =================

        private static Action _innerDelegate;
        private static bool OnConsoleClose(CtrlTypes ctrlType)
        {
            var tmp = _innerDelegate;
            if (tmp != null)
            {
                if (ctrlType == CtrlTypes.CTRL_BREAK_EVENT ||
                    //ctrlType == CtrlTypes.CTRL_C_EVENT ||
                    ctrlType == CtrlTypes.CTRL_CLOSE_EVENT ||
                    ctrlType == CtrlTypes.CTRL_LOGOFF_EVENT ||
                    ctrlType == CtrlTypes.CTRL_SHUTDOWN_EVENT)
                {
                    tmp();
                }
            }
                

            return true;
        }


        /// <summary>
        /// Console closing event
        /// </summary>
        public static event Action ConsoleClose
        {
            add
            {
                if (_innerDelegate == null)
                    SetConsoleCtrlHandler(new HandlerRoutine(OnConsoleClose), true);

                _innerDelegate += value;
            }
            remove
            {
                _innerDelegate -= value;
            }
        }

        // =============

        /// <summary>
        /// Disables console window close button
        /// </summary>
        public static void DisableConsoleCloseButton()
        {
            var cnslWnd = GetConsoleWindow();
            if (cnslWnd == IntPtr.Zero)
                return;

            var sysMenu = GetSystemMenu(cnslWnd, 0);
            if (sysMenu == IntPtr.Zero)
                return;

            EnableMenuItem(sysMenu, (uint)SysMenuElems.SC_CLOSE, 1);
        }

        /// <summary>
        /// Enables console window close button
        /// </summary>
        public static void EnableConsoleCloseButton()
        {
            var cnslWnd = GetConsoleWindow();
            if (cnslWnd == IntPtr.Zero)
                return;

            var sysMenu = GetSystemMenu(cnslWnd, 0);
            if (sysMenu == IntPtr.Zero)
                return;

            EnableMenuItem(sysMenu, (uint)SysMenuElems.SC_CLOSE, 0);
        }
    }
}
