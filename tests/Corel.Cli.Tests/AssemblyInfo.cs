using System.Runtime.Versioning;
using Xunit;

[assembly: SupportedOSPlatform("windows")]

// CommandDispatcherTests/VerbGateTests redirect the process-wide Console.Out/Error to
// capture output - parallel test classes writing to the real Console at the same time
// would race on that swap, so this assembly's tests run sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
