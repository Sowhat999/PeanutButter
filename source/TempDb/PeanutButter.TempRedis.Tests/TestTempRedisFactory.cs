using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace PeanutButter.TempRedis.Tests
{
    [TestFixture]
    public class TestTempRedisFactory
    {
        [Test]
        public void ShouldProvideAndReUseServers()
        {
            // Arrange
            // Act
            TempRedis server1;
            TempRedis server2;
            TempRedis server3;

            using (var sut = Create())
            {
                using (var lease1 = sut.BorrowServer())
                {
                    server1 = lease1.Item;
                    using var lease2 = sut.BorrowServer();
                    server2 = lease2.Item;
                }

                using (var lease3 = sut.BorrowServer())
                {
                    server3 = lease3.Item;
                }

                // Assert
                Expect(server1)
                    .To.Be.An.Instance.Of<TempRedis>()
                    .And
                    .Not.To.Be(server2);
                Expect(server2)
                    .To.Be.An.Instance.Of<TempRedis>()
                    .And
                    .To.Be(server3);
            }

            Expect(server1.IsDisposed)
                .To.Be.True();
            Expect(server2.IsDisposed)
                .To.Be.True();
            Expect(server3.IsDisposed)
                .To.Be.True();
        }

        private static TempRedisFactory Create()
        {
            return new TempRedisFactory();
        }
    }
}