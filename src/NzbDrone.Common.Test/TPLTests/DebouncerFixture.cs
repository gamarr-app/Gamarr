using System;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.TPL;

namespace NzbDrone.Common.Test.TPLTests
{
    [TestFixture]
    public class DebouncerFixture
    {
        public class Counter
        {
            public int Count { get; private set; }

            public void Hit()
            {
                Count++;
            }
        }

        private void WaitForCount(Counter counter, int expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (counter.Count >= expected)
                {
                    return;
                }

                Thread.Sleep(10);
            }
        }

        [Test]
        public void should_hold_the_call_for_debounce_duration()
        {
            var counter = new Counter();
            var debounceFunction = new Debouncer(counter.Hit, TimeSpan.FromMilliseconds(200));

            debounceFunction.Execute();
            debounceFunction.Execute();
            debounceFunction.Execute();

            counter.Count.Should().Be(0);

            WaitForCount(counter, 1);

            counter.Count.Should().Be(1);
        }

        [Test]
        public void should_throttle_calls()
        {
            var counter = new Counter();
            var debounceFunction = new Debouncer(counter.Hit, TimeSpan.FromMilliseconds(200));

            debounceFunction.Execute();
            debounceFunction.Execute();
            debounceFunction.Execute();

            counter.Count.Should().Be(0);

            WaitForCount(counter, 1);

            debounceFunction.Execute();
            debounceFunction.Execute();
            debounceFunction.Execute();

            WaitForCount(counter, 2);

            counter.Count.Should().Be(2);
        }

        [Test]
        public void should_hold_the_call_while_paused()
        {
            var counter = new Counter();
            var debounceFunction = new Debouncer(counter.Hit, TimeSpan.FromMilliseconds(200));

            debounceFunction.Pause();

            debounceFunction.Execute();
            debounceFunction.Execute();

            Thread.Sleep(500);

            counter.Count.Should().Be(0);

            debounceFunction.Execute();
            debounceFunction.Execute();

            Thread.Sleep(500);

            counter.Count.Should().Be(0);

            debounceFunction.Resume();

            WaitForCount(counter, 1);

            counter.Count.Should().Be(1);
        }

        [Test]
        public void should_handle_pause_reentrancy()
        {
            var counter = new Counter();
            var debounceFunction = new Debouncer(counter.Hit, TimeSpan.FromMilliseconds(200));

            debounceFunction.Pause();
            debounceFunction.Pause();

            debounceFunction.Execute();
            debounceFunction.Execute();

            debounceFunction.Resume();

            Thread.Sleep(500);

            counter.Count.Should().Be(0);

            debounceFunction.Resume();

            WaitForCount(counter, 1);

            counter.Count.Should().Be(1);
        }
    }
}
