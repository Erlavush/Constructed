using System;
using System.Collections.Generic;
using System.IO;

namespace Constructed.Unity
{
    public static class CreatePrivateAssetProjectPaths
    {
        public const string ReferenceRepositoryRelativePath = "References/Create-mc1.21.1-dev";
        public const string PrivateCreateAssetRelativePath = "Assets/PrivateTemp/Create";

        public static string GetReferenceRepositoryRoot(string projectRoot)
        {
            return CreatePrivateAssetPathResolver.ResolvePath(projectRoot, ReferenceRepositoryRelativePath);
        }

        public static string GetPrivateCreateAssetRoot(string projectRoot)
        {
            return CreatePrivateAssetPathResolver.ResolvePath(projectRoot, PrivateCreateAssetRelativePath);
        }
    }

    public sealed class CreatePrivateAssetSyncResult
    {
        private readonly List<string> copiedPaths;
        private readonly List<string> unchangedPaths;
        private readonly List<string> missingPaths;

        public CreatePrivateAssetSyncResult(
            int requestedFileCount,
            IEnumerable<string> copiedPaths,
            IEnumerable<string> unchangedPaths,
            IEnumerable<string> missingPaths)
        {
            if (requestedFileCount < 0)
                throw new ArgumentOutOfRangeException(nameof(requestedFileCount), "Requested file count cannot be negative.");
            if (copiedPaths == null)
                throw new ArgumentNullException(nameof(copiedPaths));
            if (unchangedPaths == null)
                throw new ArgumentNullException(nameof(unchangedPaths));
            if (missingPaths == null)
                throw new ArgumentNullException(nameof(missingPaths));

            RequestedFileCount = requestedFileCount;
            this.copiedPaths = new List<string>(copiedPaths);
            this.unchangedPaths = new List<string>(unchangedPaths);
            this.missingPaths = new List<string>(missingPaths);
        }

        public int RequestedFileCount { get; }

        public IReadOnlyList<string> CopiedPaths
        {
            get { return copiedPaths; }
        }

        public IReadOnlyList<string> UnchangedPaths
        {
            get { return unchangedPaths; }
        }

        public IReadOnlyList<string> MissingPaths
        {
            get { return missingPaths; }
        }

        public int AvailableFileCount
        {
            get { return copiedPaths.Count + unchangedPaths.Count; }
        }
    }

    public static class CreatePrivateAssetPathResolver
    {
        public static string ResolveReferenceSourcePath(string referenceRepositoryRoot, CreatePrivateAssetFileReference file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return ResolvePath(referenceRepositoryRoot, file.RepositoryRelativePath);
        }

        public static string ResolvePrivateAssetPath(string privateAssetRoot, CreatePrivateAssetFileReference file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            return ResolvePath(privateAssetRoot, file.PrivateRelativePath);
        }

        public static string ResolvePath(string rootPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));
            if (Path.IsPathRooted(relativePath))
                throw new ArgumentException("Relative path cannot be absolute.", nameof(relativePath));

            string normalizedRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(rootPath));
            string normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string combinedPath = Path.GetFullPath(Path.Combine(normalizedRoot, normalizedRelative));
            if (!IsPathUnderRoot(normalizedRoot, combinedPath))
                throw new InvalidOperationException($"Resolved path escaped root {normalizedRoot}: {relativePath}");

            return combinedPath;
        }

        public static bool IsPathUnderRoot(string rootPath, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(candidatePath))
                return false;

            string normalizedRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(rootPath));
            string normalizedCandidate = Path.GetFullPath(candidatePath);
            return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;

            return path + Path.DirectorySeparatorChar;
        }
    }

    public static class CreatePrivateAssetSyncService
    {
        public static CreatePrivateAssetSyncResult Sync(
            CreatePrivateAssetManifest manifest,
            string referenceRepositoryRoot,
            string privateAssetRoot)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            List<string> copiedPaths = new List<string>();
            List<string> unchangedPaths = new List<string>();
            List<string> missingPaths = new List<string>();

            foreach (CreatePrivateAssetFileReference file in manifest.UniqueFiles)
            {
                string sourcePath = CreatePrivateAssetPathResolver.ResolveReferenceSourcePath(referenceRepositoryRoot, file);
                string privatePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(privateAssetRoot, file);
                if (!File.Exists(sourcePath))
                {
                    missingPaths.Add(file.RepositoryRelativePath);
                    continue;
                }

                string privateDirectory = Path.GetDirectoryName(privatePath);
                if (!string.IsNullOrEmpty(privateDirectory))
                    Directory.CreateDirectory(privateDirectory);

                if (File.Exists(privatePath) && FilesMatch(sourcePath, privatePath))
                {
                    unchangedPaths.Add(file.RepositoryRelativePath);
                    continue;
                }

                File.Copy(sourcePath, privatePath, true);
                copiedPaths.Add(file.RepositoryRelativePath);
            }

            return new CreatePrivateAssetSyncResult(manifest.UniqueFiles.Count, copiedPaths, unchangedPaths, missingPaths);
        }

        private static bool FilesMatch(string sourcePath, string privatePath)
        {
            FileInfo sourceInfo = new FileInfo(sourcePath);
            FileInfo privateInfo = new FileInfo(privatePath);
            if (sourceInfo.Length != privateInfo.Length)
                return false;

            using (FileStream sourceStream = File.OpenRead(sourcePath))
            using (FileStream privateStream = File.OpenRead(privatePath))
            {
                int sourceByte;
                while ((sourceByte = sourceStream.ReadByte()) >= 0)
                {
                    if (sourceByte != privateStream.ReadByte())
                        return false;
                }
            }

            return true;
        }
    }
}
