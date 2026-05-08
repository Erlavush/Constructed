using System;
using System.Collections.Generic;
using System.IO;
using Constructed.Core;
using Constructed.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Constructed.Tests
{
    public sealed class CreatePrivateAssetManifestTests
    {
        [Test]
        public void FirstSliceManifestContainsExpectedItemAndBlockTargets()
        {
            CreatePrivateAssetManifest manifest = CreateFirstSlicePrivateAssetManifest.Manifest;

            Assert.AreEqual(13, manifest.Targets.Count);

            CollectionAssert.AreEquivalent(
                CreateFirstSlicePrivateAssetManifest.ItemCatalogIds,
                GetTargetIds(manifest, CreateVisualAssetKind.Item));

            CollectionAssert.AreEquivalent(
                CreateFirstSlicePrivateAssetManifest.BlockCatalogIds,
                GetTargetIds(manifest, CreateVisualAssetKind.Block));

            CreateVisualAssetTarget brassFunnelBlock = manifest.GetTarget(ResourceLocation.Parse("create:brass_funnel"), CreateVisualAssetKind.Block);
            Assert.IsTrue(ContainsPath(brassFunnelBlock, "src/generated/resources/assets/create/blockstates/brass_funnel.json"));
            Assert.IsTrue(ContainsPath(brassFunnelBlock, "src/main/resources/assets/create/models/block/funnel/block_vertical.json"));

            CreateVisualAssetTarget creativeCrateItem = manifest.GetTarget(ResourceLocation.Parse("create:creative_crate"), CreateVisualAssetKind.Item);
            Assert.IsTrue(ContainsPath(creativeCrateItem, "src/generated/resources/assets/create/models/block/crate/creative/single.json"));
            Assert.IsTrue(ContainsPath(creativeCrateItem, "src/main/resources/assets/create/textures/block/crate_creative.png"));

            CreateVisualAssetTarget creativeMotorBlock = manifest.GetTarget(ResourceLocation.Parse("create:creative_motor"), CreateVisualAssetKind.Block);
            Assert.IsTrue(ContainsPath(creativeMotorBlock, "src/main/resources/assets/create/models/block/shaft_half.json"));
        }

        [Test]
        public void FirstSliceManifestPathsExistInReferenceCheckout()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string referenceRoot = Path.Combine(projectRoot, "References", "Create-mc1.21.1-dev");

            foreach (CreatePrivateAssetFileReference file in CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles)
            {
                string sourcePath = CreatePrivateAssetPathResolver.ResolveReferenceSourcePath(referenceRoot, file);
                Assert.IsTrue(File.Exists(sourcePath), $"Missing manifest source file: {file.RepositoryRelativePath}");
            }
        }

        [Test]
        public void ResolvePrivateAssetPathKeepsManifestFilesUnderPrivateTempRoot()
        {
            string tempProjectRoot = Path.Combine(Path.GetTempPath(), "ConstructedTempProject");
            string expectedPrivateRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(tempProjectRoot);

            foreach (CreatePrivateAssetFileReference file in CreateFirstSlicePrivateAssetManifest.Manifest.UniqueFiles)
            {
                string privatePath = CreatePrivateAssetPathResolver.ResolvePrivateAssetPath(tempProjectRoot, file);
                Assert.IsTrue(
                    CreatePrivateAssetPathResolver.IsPathUnderRoot(expectedPrivateRoot, privatePath),
                    $"Resolved private path escaped root for {file.RepositoryRelativePath}");
            }
        }

        [Test]
        public void ProjectPathHelperResolvesReferenceAndPrivateRootsFromProjectRoot()
        {
            string projectRoot = Path.Combine(Path.GetTempPath(), "ConstructedProjectRoot");

            string referenceRoot = CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(projectRoot);
            string privateRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(projectRoot);

            StringAssert.EndsWith(Path.Combine("References", "Create-mc1.21.1-dev"), referenceRoot);
            StringAssert.EndsWith(Path.Combine("Assets", "PrivateTemp", "Create"), privateRoot);
            Assert.IsTrue(CreatePrivateAssetPathResolver.IsPathUnderRoot(projectRoot, referenceRoot));
            Assert.IsTrue(CreatePrivateAssetPathResolver.IsPathUnderRoot(projectRoot, privateRoot));
        }

        [Test]
        public void ResolvePathRejectsEscapingRelativePaths()
        {
            string root = Path.Combine(Path.GetTempPath(), "Constructed", "PathSafety");

            Assert.Throws<InvalidOperationException>(() => CreatePrivateAssetPathResolver.ResolvePath(root, "..\\outside.txt"));
            Assert.Throws<InvalidOperationException>(() => CreatePrivateAssetPathResolver.ResolvePath(root, "nested\\..\\..\\outside.txt"));
        }

        [Test]
        public void SyncCopiesExistingFilesAndReportsMissingFiles()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "ConstructedSync", Guid.NewGuid().ToString("N"));
            string referenceRoot = CreatePrivateAssetProjectPaths.GetReferenceRepositoryRoot(tempRoot);
            string privateRoot = CreatePrivateAssetProjectPaths.GetPrivateCreateAssetRoot(tempRoot);
            string existingPath = "src/main/resources/assets/create/textures/item/example.png";
            string missingPath = "src/generated/resources/assets/create/models/item/missing.json";

            try
            {
                string sourceAbsolutePath = Path.Combine(referenceRoot, existingPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(sourceAbsolutePath));
                File.WriteAllText(sourceAbsolutePath, "example-content");

                CreatePrivateAssetManifest manifest = new CreatePrivateAssetManifest(new[]
                {
                    new CreateVisualAssetTarget(
                        ResourceLocation.Parse("create:example"),
                        CreateVisualAssetKind.Item,
                        "Example",
                        new[]
                        {
                            new CreatePrivateAssetFileReference(existingPath),
                            new CreatePrivateAssetFileReference(missingPath)
                        })
                });

                CreatePrivateAssetSyncResult firstSync = CreatePrivateAssetSyncService.Sync(manifest, tempRoot);
                string privateAbsolutePath = Path.Combine(privateRoot, existingPath.Replace('/', Path.DirectorySeparatorChar));

                Assert.AreEqual(2, firstSync.RequestedFileCount);
                Assert.AreEqual(1, firstSync.CopiedPaths.Count);
                Assert.AreEqual(0, firstSync.UnchangedPaths.Count);
                Assert.AreEqual(1, firstSync.MissingPaths.Count);
                Assert.IsTrue(File.Exists(privateAbsolutePath));
                Assert.AreEqual("example-content", File.ReadAllText(privateAbsolutePath));
                Assert.AreEqual(existingPath, firstSync.CopiedPaths[0]);
                Assert.AreEqual(missingPath, firstSync.MissingPaths[0]);

                CreatePrivateAssetSyncResult secondSync = CreatePrivateAssetSyncService.Sync(manifest, tempRoot);

                Assert.AreEqual(0, secondSync.CopiedPaths.Count);
                Assert.AreEqual(1, secondSync.UnchangedPaths.Count);
                Assert.AreEqual(existingPath, secondSync.UnchangedPaths[0]);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        private static List<ResourceLocation> GetTargetIds(CreatePrivateAssetManifest manifest, CreateVisualAssetKind visualKind)
        {
            List<ResourceLocation> ids = new List<ResourceLocation>();
            foreach (CreateVisualAssetTarget target in manifest.Targets)
            {
                if (target.VisualKind == visualKind)
                    ids.Add(target.ResourceId);
            }

            return ids;
        }

        private static bool ContainsPath(CreateVisualAssetTarget target, string repositoryRelativePath)
        {
            foreach (CreatePrivateAssetFileReference file in target.Files)
            {
                if (file.RepositoryRelativePath == repositoryRelativePath)
                    return true;
            }

            return false;
        }
    }
}
