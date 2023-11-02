using NUnit.Framework;
using NExpect;
using PeanutButter.TempDb.MySql.Base;
using PeanutButter.Utils;
using PeanutButter.WindowsServiceManagement;
using static NExpect.Expectations;

namespace PeanutButter.TempDb.Tests
{
    [TestFixture]
    public class TestMySqlServiceFinder
    {
        [Test]
        public void ShouldBeAbleToFindServiceWhenNotRunning()
        {
            // Arrange
            if (!Platform.IsWindows)
            {
                Assert.Ignore(
                    "Requires the service infrastructure of windows"
                );
                return;
            }

            var svc = new WindowsServiceUtil("mysql80");
            svc.Stop();
            // Act
            var path = MySqlWindowsServiceFinder.FindPathToMySqlD();
            // Assert
            Expect(path)
                .Not.To.Be.Null.Or.Empty();
        }
    }
}