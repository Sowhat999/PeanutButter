﻿using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using PeanutButter.Utils;

#if BUILD_PEANUTBUTTER_INTERNAL
namespace Imported.PeanutButter.TestUtils.AspNetCore.Utils;
#else
namespace PeanutButter.TestUtils.AspNetCore.Utils;
#endif

/// <summary>
/// Encodes a form with url-encoding
/// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
internal
#else
public
#endif
    class UrlEncodedBodyEncoder : IFormEncoder
{
    /// <inheritdoc />
    public Stream Encode(IFormCollection form)
    {
        var result = new MemoryStream();
        if (form is null)
        {
            return result;
        }

        var isFirst = true;
        foreach (var key in form.Keys)
        {
            if (!isFirst)
            {
                result.AppendString("&");
            }

            isFirst = false;

            result.AppendString($"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(form[key])}");
        }

        result.Position = 0;
        return result;
    }
}