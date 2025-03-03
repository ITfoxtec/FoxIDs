using MysticMind.PostgresEmbed;
using System;

namespace FoxIDs.IntegrationTests.Helpers
{
    public class DatabaseFixture : IDisposable
    {
        public DatabaseFixture()
        {
            PgServer = new PgServer("17.4.0", clearWorkingDirOnStart: true, clearInstanceDirOnStop: true);
            PgServer.Start();
        }

        public void Dispose()
        {
            PgServer.Stop();
        }

        public PgServer PgServer { get; private set; }
    }
}