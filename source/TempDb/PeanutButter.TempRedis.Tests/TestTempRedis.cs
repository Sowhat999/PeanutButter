using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using StackExchange.Redis;
using NUnit.Framework;
using NExpect;
using PeanutButter.Utils;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace PeanutButter.TempRedis.Tests;

[TestFixture]
public class TestTempRedis
{
    [TestCase("127.0.0.1")]
    [TestCase("localhost")]
    public void ShouldStartUp(string ipOrHostName)
    {
        // Arrange
        using var sut = Create();

        // Act
        var connection1 = ConnectionMultiplexer.Connect(
            $"{ipOrHostName}:{sut.Port}",
            o => o.ConnectTimeout = 1000
        );
        connection1.GetDatabase();
        var server = connection1.GetEndPoints()
            .Select(e => connection1.GetServer(e))
            .FirstOrDefault();

        // Assert
        Expect(server)
            .Not.To.Be.Null(() => "Can't determine the redis server");
    }

    [Test]
    [Explicit(
        @"Discovery: 
    we should only use valid values mapped back to localhost 
    - a system without ipv6 may fail if we configure binding 
    to include ::1"
    )]
    public void WhatLoopbackDevicesDoIHave()
    {
        // Arrange
        // Act
        var entry = Dns.GetHostEntry("localhost");
        var result = string.Join(
            " ",
            entry.AddressList
                .OrderBy(a => a.AddressFamily != AddressFamily.InterNetworkV6)
                .Select(a => $"{a}")
        );
        // Assert
        Expect(result)
            .To.Equal("::1 127.0.0.1");
    }

    [Test]
    public void ShouldProvideConvenienceConnectMethod()
    {
        // Arrange
        using var sut = Create();

        // Act
        var connection = sut.Connect();
        connection.GetDatabase();
        var server = connection.GetEndPoints()
            .Select(e => connection.GetServer(e))
            .FirstOrDefault();

        // Assert
        Expect(server)
            .Not.To.Be.Null(() => "Can't determine the redis server");
    }

    [Test]
    public void ShouldProvideConvenienceConnectMethodWithOptions()
    {
        // Arrange
        using var sut = Create();

        // Act
        var connection = sut.Connect(
            new ConfigurationOptions()
            {
                EndPoints =
                {
                    {
                        "3.3.3.3", 1234 // these should be ignored
                    }
                },
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
                AsyncTimeout = 5000
            }
        );
        // Assert
        Expect(connection.TimeoutMilliseconds)
            .To.Equal(5000);
        connection.GetDatabase();
        var server = connection.GetEndPoints()
            .Select(e => connection.GetServer(e))
            .FirstOrDefault();

        Expect(server)
            .Not.To.Be.Null(() => "Can't determine the redis server");
    }

    [Test]
    [Timeout(10000)]
    public void ShouldRestartOnAccidentalDeath()
    {
        // Arrange
        using var sut = Create();
        var key = GetRandomString();
        var value = GetRandomString();
        // Act
        Console.Error.WriteLine("Attempt to connect");
        var connection = ConnectionMultiplexer.Connect(
            $"127.0.0.1:{sut.Port}",
            o =>
            {
                o.ReconnectRetryPolicy = new LinearRetry(250);
                o.AbortOnConnectFail = false;
                o.ConnectRetry = 10;
                o.ConnectTimeout = 500;
                o.AsyncTimeout = 1000;
                o.SyncTimeout = 1000;
            }
        );
        var db = connection.GetDatabase();
        db.StringSet(key, value);
        sut.ServerProcess.Kill();
        do
        {
            Thread.Sleep(1);
        } while (!sut.ServerProcessIsRunning);

        string result = null;
        for (var i = 0; i < 3; i++)
        {
            try
            {
                result = (string)db.StringGet(key);
            }
            catch
            {
                if (i == 2)
                {
                    throw;
                }

                // because of tighter timings in this test,
                // a single read may, occasionally, fail
                Thread.Sleep(10);
            }
        }

        // Assert
        Expect(result)
            .To.Equal(value);
    }

    /// <summary>
    /// Uses random allocation for ports - give it three tries, and
    /// hopefully we get a passing result with no port conflict
    /// </summary>
    [Retry(3)]
    [Test]
    public void ShouldObservePortHint()
    {
        Assert.That(
            () =>
            {
                // Arrange
                var hint = GetRandomInt(20000, 25000);
                using var _ = new AutoTempEnvironmentVariable(
                    TempRedisOptions.TEMPREDIS_PORT_HINT,
                    $"{hint}"
                );
                // Act
                using var sut1 = Create();
                using var sut2 = Create();
                // Assert
                Expect(sut1.Port)
                    .To.Equal(hint);
                Expect(sut2.Port)
                    .To.Equal(hint + 1);
            },
            Throws.Nothing
        );
    }

    [Test]
    [Explicit("Requires downloading redis from GitHub")]
    public void ShouldBeAbleToStartFromAutoDownload()
    {
        // Arrange
        using var sut = new TempRedis(
            new TempRedisOptions()
            {
                LocatorStrategies = RedisLocatorStrategies.DownloadForWindowsIfNecessary
            }
        );
        // Act
        var connection = ConnectionMultiplexer.Connect(
            $"127.0.0.1:{sut.Port}"
        );
        connection.GetDatabase();
        var server = connection.GetEndPoints()
            .Select(e => connection.GetServer(e))
            .FirstOrDefault();
        // Assert
        Expect(server)
            .Not.To.Be.Null(() => "Can't determine the redis server");
    }

    private static TempRedis Create()
    {
        return new();
    }
}