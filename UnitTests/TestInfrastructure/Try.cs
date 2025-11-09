public static class Try
{
    public static void DeleteDirectory(string path)
    {
        try { Directory.Delete(path, true); } catch { /* ignore */ }
    }
}
