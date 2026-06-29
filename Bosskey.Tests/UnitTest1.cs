using System;
using Xunit;
using Bosskey;

namespace Bosskey.Tests
{
    public class BosskeyTests
    {
        [Fact]
        public void TestNextLongRange()
        {
            var random = new Random();
            long min = 100;
            long max = 1000;
            for (int i = 0; i < 100; i++)
            {
                long val = random.NextLong(min, max);
                Assert.True(val >= min, $"Generated value {val} must be >= min {min}");
                Assert.True(val < max, $"Generated value {val} must be < max {max}");
            }
        }

        [Fact]
        public void TestTfResourceInitialization()
        {
            var resource = new TfResource("aws_instance.web", "aws_instance", ResourceAction.Add);
            Assert.Equal("aws_instance.web", resource.Address);
            Assert.Equal("aws_instance", resource.Type);
            Assert.Equal(ResourceAction.Add, resource.Action);
            Assert.Equal(ResourceState.Pending, resource.State);
        }
    }
}
