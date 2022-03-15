using System;
using System.Runtime.InteropServices;
namespace DNHper
{


public static class UConverter
{

    public static T ByteArray2Struct<T>(this byte[] InData) where T : struct
    {
        T _instance;
        GCHandle handle = GCHandle.Alloc(InData, GCHandleType.Pinned);
        try
        {
            _instance = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        }
        finally
        {
            handle.Free();
        }
        return _instance;
    }

    public static string ToHexString(this byte[] InData, bool bWithDash=true){
        var _hexString = BitConverter.ToString(InData);
        if(!bWithDash){
            _hexString = _hexString.Replace("-",string.Empty);
        }
        return _hexString;
    }

    public static void BGR2RGB(ref byte[] buffer) {
        byte swap;
        for (int i = 0; i < buffer.Length; i = i + 3) {
            swap = buffer[i];
            buffer[i] = buffer[i + 2];
            buffer[i + 2] = swap;
        }
    }

}

}