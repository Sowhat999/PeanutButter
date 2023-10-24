#if BUILD_PEANUTBUTTER_INTERNAL
using Imported.PeanutButter.Utils;
#else
using PeanutButter.Utils;
#endif

namespace
#if BUILD_PEANUTBUTTER_INTERNAL
    Internal.PeanutButter.PeanutButter.TempRedis
#else
    PeanutButter.TempRedis
#endif
{
    /// <summary>
    /// Describes a TempRedis leasing factory
    /// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
    internal
#else
    public
#endif
        interface ITempRedisFactory
    {
        /// <summary>
        /// Borrow a TempRedis server; Dispose of the lease to return it.
        /// </summary>
        /// <returns></returns>
        public Lease<TempRedis> BorrowServer();
    }

    /// <inheritdoc cref="PeanutButter.TempRedis.ITempRedisFactory" />
    public class TempRedisFactory : LeasingFactory<TempRedis>, ITempRedisFactory
    {
        /// <summary>
        /// Instantiates a new TempRedis factory with default
        /// options
        /// </summary>
        public TempRedisFactory() : this(new TempRedisOptions())
        {
        }

        /// <summary>
        /// Instantiates a new TempRedis factory where new
        /// instances are created with the provided options
        /// </summary>
        /// <param name="redisOptions"></param>
        public TempRedisFactory(TempRedisOptions redisOptions)
            : base(() => new TempRedis(redisOptions))
        {
        }

        /// <inheritdoc />
        public Lease<TempRedis> BorrowServer()
        {
            return Borrow();
        }
    }
}