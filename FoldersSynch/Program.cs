using System.Security.Cryptography;

class Program
{
    private static string logFilePath;

    static void Main(string[] args)
    {
        try
        {
            string sourcePath = args[0];
            string replicaPath = args[1];
            string interval = args[2];
            logFilePath = args[3];

            bool isDir = (File.GetAttributes(sourcePath) & FileAttributes.Directory) == FileAttributes.Directory;
            if (!isDir)
                throw new Exception("The provided source path does not correspond to a directory");

            isDir = (File.GetAttributes(replicaPath) & FileAttributes.Directory) == FileAttributes.Directory;
            if (!isDir)
                throw new Exception("The provided replica path does not correspond to a directory");

            int synchInterval = 0;

            if (!int.TryParse(interval, out synchInterval))
                throw new Exception("Wrong value in interval argument");

            synchInterval *= 1000;

            while (true)
            {
                CopyFolderContent(sourcePath, replicaPath);
                DeleteNotMatchingContent(sourcePath, replicaPath);

                Thread.Sleep(synchInterval);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private static void CopyFolderContent(string sourcePath, string replicaPath)
    {
        try
        {
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var sourceFileInfo = new FileInfo(file);
                var replicaFileInfo = new FileInfo(replicaPath + "\\" + sourceFileInfo.Name);

                if (replicaFileInfo.Exists)
                {
                    if (!CompareFiles(sourceFileInfo, replicaFileInfo))
                    {
                        File.Copy(sourceFileInfo.FullName, replicaFileInfo.FullName, true);
                        GenerateLog("FILE UPDATED", replicaFileInfo.FullName);
                    }
                }
                else
                {
                    File.Copy(sourceFileInfo.FullName, replicaFileInfo.FullName);
                    GenerateLog("FILE COPIED", replicaFileInfo.FullName);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourcePath))
            {
                var sourceDirectoryInfo = new DirectoryInfo(directory);
                var replicaDirectoryInfo = new DirectoryInfo(replicaPath + "\\" + sourceDirectoryInfo.Name);

                if (!replicaDirectoryInfo.Exists)
                {
                    Directory.CreateDirectory(replicaDirectoryInfo.FullName);
                    GenerateLog("DIRECTORY CREATED", replicaDirectoryInfo.FullName);
                }

                CopyFolderContent(sourceDirectoryInfo.FullName, replicaDirectoryInfo.FullName);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private static bool CompareFiles(FileInfo sourceFile, FileInfo replicaFile)
    {
        try
        {
            if (sourceFile.Length == replicaFile.Length)
            {
                byte[] sourceHash = MD5.Create().ComputeHash(sourceFile.OpenRead());
                byte[] replicaHash = MD5.Create().ComputeHash(replicaFile.OpenRead());

                for (int i = 0; i < sourceHash.Length; i++)
                {
                    if (sourceHash[i] != replicaHash[i])
                        return false;
                }

                return true;
            }
            else
                return false;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private static void DeleteNotMatchingContent(string sourcePath, string replicaPath)
    {
        try
        {
            foreach (var file in Directory.GetFiles(replicaPath))
            {
                var replicaFileInfo = new FileInfo(file);
                var sourceFileInfo = new FileInfo(sourcePath + "\\" + replicaFileInfo.Name);

                if (!sourceFileInfo.Exists)
                {
                    replicaFileInfo.Delete();
                    GenerateLog("FILE DELETED", replicaFileInfo.FullName);
                }
            }

            foreach (var directory in Directory.GetDirectories(replicaPath))
            {
                var replicaDirectoryInfo = new DirectoryInfo(directory);
                var sourceDirectoryInfo = new DirectoryInfo(sourcePath + "\\" + replicaDirectoryInfo.Name);

                if (!sourceDirectoryInfo.Exists)
                {
                    replicaDirectoryInfo.Delete(true);
                    GenerateLog("DIRECTORY DELETED", replicaDirectoryInfo.FullName);
                }
                else
                    DeleteNotMatchingContent(sourceDirectoryInfo.FullName, replicaDirectoryInfo.FullName);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private static void GenerateLog(string action, string filePath)
    {
        try
        {
            var log = DateTime.Now.ToString() + '\t' + action + "\t" + filePath;

            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine(log);
            }

            Console.WriteLine(log);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}