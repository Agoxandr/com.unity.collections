using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

internal class UnsafeHashMapTests
{
    // Burst error BC1071: Unsupported assert type
    // [BurstCompile(CompileSynchronously = true)]
    public struct UnsafeHashMapAddJob : IJob
    {
        public UnsafeHashMap<int, int>.ParallelWriter Writer;

        public void Execute()
        {
            Assert.True(Writer.TryAdd(123, 1));
        }
    }

    [Test]
    public void UnsafeHashMap_AddJob()
    {
        var container = new UnsafeHashMap<int, int>(32, Allocator.TempJob);

        var job = new UnsafeHashMapAddJob()
        {
            Writer = container.AsParallelWriter(),
        };

        job.Schedule().Complete();

        Assert.True(container.ContainsKey(123));

        container.Dispose();
    }

    [Test]
    public void UnsafeHashMap_ForEach([Values(10, 1000)]int n)
    {
        var seen = new NativeArray<int>(n, Allocator.Temp);
        using (var container = new UnsafeHashMap<int, int>(32, Allocator.TempJob))
        {
            for (int i = 0; i < n; i++)
            {
                container.Add(i, i * 37);
            }

            var count = 0;
            foreach (var kv in container)
            {
                int value;
                Assert.True(container.TryGetValue(kv.Key, out value));
                Assert.AreEqual(value, kv.Value);
                Assert.AreEqual(kv.Key * 37, kv.Value);

                seen[kv.Key] = seen[kv.Key] + 1;
                ++count;
            }

            Assert.AreEqual(container.Count(), count);
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual(1, seen[i], $"Incorrect key count {i}");
            }
        }
    }
}
