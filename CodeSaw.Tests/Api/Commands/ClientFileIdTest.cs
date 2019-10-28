using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.RepositoryApi;

namespace CodeSaw.Tests.Api.Commands
{
    [TestFixture]
    public class ClientFileIdTest
    {
        [Test]
        public void ParseProvisionalIdOldAndNew()
        {
            var clientFileId = ClientFileId.Parse("PROV_cGF0aDEAcGF0aDI=");

            Assert.That(clientFileId.IsProvisional, Is.True);
            Assert.That(clientFileId.ProvisionalPathPair.OldPath, Is.EqualTo("path1"));
            Assert.That(clientFileId.ProvisionalPathPair.NewPath, Is.EqualTo("path2"));
        }

        [Test]
        public void ParseProvisionalIdOnlyNew()
        {
            var clientFileId = ClientFileId.Parse("PROV_cGF0aABwYXRo");

            Assert.That(clientFileId.IsProvisional, Is.True);
            Assert.That(clientFileId.ProvisionalPathPair.OldPath, Is.EqualTo("path"));
            Assert.That(clientFileId.ProvisionalPathPair.NewPath, Is.EqualTo("path"));
        }

        [Test]
        public void WriteProvisionalIdOldAndNew()
        {
            var clientFileId = ClientFileId.Provisional(PathPair.Make("path1", "path2"));

            var text = ClientFileId.Write(clientFileId);

            Assert.That(text, Is.EqualTo("PROV_cGF0aDEAcGF0aDI="));
        }

        [Test]
        public void WriteProvisionalIdOnlyNew()
        {
            var clientFileId = ClientFileId.Provisional(PathPair.Make("path"));

            var text = ClientFileId.Write(clientFileId);

            Assert.That(text, Is.EqualTo("PROV_cGF0aABwYXRo"));
        }

        [Test]
        public void ParsePersistentId()
        {
            var guid = "9acf4124-e1eb-44cd-bdbb-f8c675bce872";

            var clientFileId = ClientFileId.Parse(guid);

            Assert.That(clientFileId.IsProvisional, Is.False);
            Assert.That(clientFileId.PersistentId, Is.EqualTo(Guid.Parse(guid)));
        }

        [Test]
        public void WritePersistentId()
        {
            var guid = "69e1cf41-71cb-4384-86c9-c66706dcf3ea";

            var clientFileId = ClientFileId.Persistent(Guid.Parse(guid));

            var text = ClientFileId.Write(clientFileId);

            Assert.That(text, Is.EqualTo(guid));
        }
    }
}
