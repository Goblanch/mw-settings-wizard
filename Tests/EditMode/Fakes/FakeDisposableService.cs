using System;

namespace MiradorWorks.Settings.Tests.EditMode
{
    public sealed class FakeDisposableService : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }
}