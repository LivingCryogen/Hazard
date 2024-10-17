using System.IO.Abstractions;

namespace Model.Tests.Fixtures;

public static class FileProcessor
{
    private static readonly IFileSystem _fileSystem;

    static FileProcessor()
    {
        _fileSystem = new FileSystem();
    }

    public static string ReadFile(string path)
    {
        return _fileSystem.File.ReadAllText(path);
    }

    public static void WriteFile(string path, string content)
    {
        _fileSystem.File.WriteAllText(path, content);
    }

    public static void Move(string oldPath, string newPath)
    {
        _fileSystem.File.Move(oldPath, newPath);
    }

    public static string GetTempFile()
    {
        return _fileSystem.Path.GetTempFileName();
    }

    public static void Delete(string path)
    {
        _fileSystem.File.Delete(path);
    }

    public static byte[] ReadAllBytes(string path)
    {
        return _fileSystem.File.ReadAllBytes(path);
    }
}
