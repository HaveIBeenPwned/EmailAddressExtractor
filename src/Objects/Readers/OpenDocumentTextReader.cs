using System.IO.Compression;

namespace MyAddressExtractor.Objects.Readers {
    internal sealed class OpenDocumentTextReader : PlainTextReader
    {
        private static string DestinationPath = Path.GetTempFileName();

        public OpenDocumentTextReader(string zipPath)
            : base(ExtractContent(zipPath))
        {

        }

        private static string ExtractContent(string zipPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Equals("content.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(DestinationPath, true);
                        return DestinationPath;
                    }
                }
            }
            
            throw new Exception($"Unable to load content.xml from '{zipPath}'");
        }

        public async override ValueTask DisposeAsync()
        {
            if(Path.Exists(OpenDocumentTextReader.DestinationPath))
            {
                File.Delete(OpenDocumentTextReader.DestinationPath);
            }

            await base.DisposeAsync();
        }
    }
}