using System;
using System.IO;

namespace DNHper
{
    
public static class Utility
{
    public static void CallFunction(Delegate InDelegate, params object[] Params)
    {
        if(InDelegate!=null)
            InDelegate.DynamicInvoke(Params);
    }

    public static void Dispose(ref IDisposable Target){
        if(Target!=null){
            Target.Dispose();
            Target = null;
        }
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        
        // If the destination directory doesn't exist, create it.       
        Directory.CreateDirectory(destDirName);        

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, true);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

}



}

