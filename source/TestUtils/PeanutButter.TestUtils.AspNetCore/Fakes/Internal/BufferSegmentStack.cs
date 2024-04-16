﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if BUILD_PEANUTBUTTER_INTERNAL
namespace Imported.PeanutButter.TestUtils.AspNetCore.Fakes.Internal;
#else
namespace PeanutButter.TestUtils.AspNetCore.Fakes.Internal;
#endif

/// <summary>
/// Copied from dotnet sources, kept internal
/// </summary>
internal struct BufferSegmentStack
{
    private SegmentAsValueType[] _array;
    private int _size;

    public BufferSegmentStack(int size)
    {
        _array = new SegmentAsValueType[size];
        _size = 0;
    }

    public readonly int Count => _size;

    public bool TryPop([NotNullWhen(true)] out BufferSegment result)
    {
        var size = _size - 1;
        var array = _array;

        if ((uint)size >= (uint)array.Length)
        {
            result = default;
            return false;
        }

        _size = size;
        result = array[size];
        array[size] = default;
        return true;
    }

    // Pushes an item to the top of the stack.
    public void Push(BufferSegment item)
    {
        var size = _size;
        var array = _array;

        if ((uint)size < (uint)array.Length)
        {
            array[size] = item;
            _size = size + 1;
        }
        else
        {
            PushWithResize(item);
        }
    }

    // Non-inline from Stack.Push to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void PushWithResize(BufferSegment item)
    {
        Array.Resize(ref _array, 2 * _array.Length);
        _array[_size] = item;
        _size++;
    }

    /// <summary>
    /// A simple struct we wrap reference types inside when storing in arrays to
    /// bypass the CLR's covariant checks when writing to arrays.
    /// </summary>
    /// <remarks>
    /// We use <see cref="SegmentAsValueType"/> as a wrapper to avoid paying the cost of covariant checks whenever
    /// the underlying array that the <see cref="BufferSegmentStack"/> class uses is written to.
    /// We've recognized this as a perf win in ETL traces for these stack frames:
    /// clr!JIT_Stelem_Ref
    ///   clr!ArrayStoreCheck
    ///     clr!ObjIsInstanceOf
    /// </remarks>
    private readonly struct SegmentAsValueType
    {
        private readonly BufferSegment _value;
        private SegmentAsValueType(BufferSegment value) => _value = value;
        public static implicit operator SegmentAsValueType(BufferSegment s) => new SegmentAsValueType(s);
        public static implicit operator BufferSegment(SegmentAsValueType s) => s._value;
    }
}