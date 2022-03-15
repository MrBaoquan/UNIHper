using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DNHper {
    public static class UtilityExtensions {
        [DllImport ("Kernel32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        private static extern bool QueryFullProcessImageName ([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        /// <summary>
        /// 注意：某些系统进程可能不支持QueryFullProcessImageName，请自行检查
        /// </summary>
        /// <param name="process"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string GetMainModuleFileName (this Process process, int buffer = 1024) {
            var fileNameBuilder = new StringBuilder (buffer);
            uint bufferLength = (uint) fileNameBuilder.Capacity + 1;
            if (QueryFullProcessImageName (process.Handle, 0, fileNameBuilder, ref bufferLength)) {
                return fileNameBuilder.ToString ();
            }
            return string.Empty;
        }
    }
}