using System.IO.Compression;
using Centazio.Core.Misc;

namespace Centazio.Cli.Infra.Misc;

public static class Zip {

  public static async Task<byte[]> ZipDir(string dir, List<string> extensions, List<string> subdirs) {
    var zippath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
    await CreateZipImpl();

    var bytes = await File.ReadAllBytesAsync(zippath);
    File.Delete(zippath);
    return bytes;
    
    async Task CreateZipImpl() {
      await using var zip = new FileStream(zippath, FileMode.Create);
      using var archive = new ZipArchive(zip, ZipArchiveMode.Create);
      
      // root directory, add files that match extension
      Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
          .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
          .ForEach(file => archive.CreateEntryFromFile(file, Path.GetFileName(file)));
      
      // for each sub-directory add all files
      subdirs.ForEach(subdir => {
        var path = Path.Combine(dir, subdir);
        if (!Directory.Exists(path)) return;
        Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .ForEach(file => archive.CreateEntryFromFile(file, Path.GetRelativePath(dir, file)));
      });
    }
  }
}