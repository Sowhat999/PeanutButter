using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace
#if BUILD_PEANUTBUTTER_INTERNAL
    Imported.PeanutButter.Utils
#else
    PeanutButter.Utils
#endif
{
    /// <summary>
    /// Basic interface for a factory leasing objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ILeasingFactory<T> : IDisposable where T : IDisposable
    {
        /// <summary>
        /// Provides a new or previously-released http server
        /// </summary>
        /// <returns></returns>
        ILease<T> Borrow();
    }

    /// <summary>
    /// provides an abstract base for a leasing factory:
    /// callers can ask to borrow an item and dispose
    /// the lease when done to release the item for re-use
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LeasingFactory<T>
        : ILeasingFactory<T> where T : IDisposable
    {
        private readonly Func<T> _factory;
        private readonly List<T> _instances = new();

        /// <summary>
        /// Sets the factory to use when there are no items available to lease
        /// </summary>
        /// <param name="factory"></param>
        protected LeasingFactory(
            Func<T> factory
        )
        {
            _factory = factory;
        }

        /// <summary>
        /// Provides a new or previously-released http server
        /// </summary>
        /// <returns></returns>
        public virtual ILease<T> Borrow()
        {
            lock (_instances)
            {
                if (!_instances.TryShift(out var instance))
                {
                    instance = _factory();
                }

                return new Lease<T>(
                    instance,
                    () =>
                    {
                        lock (_instances)
                        {
                            _instances.Add(instance);
                        }
                    }
                );
            }
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            T[] toDispose;
            lock (_instances)
            {
                toDispose = _instances.ToArray();
                _instances.Clear();
            }

            if (toDispose.Length == 0)
            {
                return;
            }

            var errors = new ConcurrentBag<Exception>();
            Run.InParallel(
                100,
                toDispose.Select(
                    s => new Action(
                        () =>
                        {
                            try
                            {
                                s.Dispose();
                            }
                            catch (Exception ex)
                            {
                                errors.Add(ex);
                            }
                        }
                    )
                )
            );
            if (!errors.Any())
            {
                return;
            }

            Console.Error.WriteLine(
                $"Warning: one or more leased instances of {typeof(T)} could not be disposed, errors follow"
            );
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"  {error}");
            }
        }
    }

    /// <summary>
    /// Container for a leased resource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ILease<T>
        : IDisposable
    {
        /// <summary>
        /// The leased item
        /// </summary>
        T Item { get; }
    }

    /// <summary>
    /// Provides an IDisposable interface over
    /// a borrowed item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Lease<T> : ILease<T>
    {
        /// <summary>
        /// The leased item
        /// </summary>
        public T Item { get; }

        private Action _whenReleased;

        /// <summary>
        /// Lease the item, and run the action when released
        /// via .Dispose()
        /// </summary>
        /// <param name="item"></param>
        /// <param name="whenReleased"></param>
        public Lease(
            T item,
            Action whenReleased
        )
        {
            Item = item;
            _whenReleased = whenReleased;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var callback = _whenReleased;
            _whenReleased = null;
            callback?.Invoke();
        }
    }
}