using Hazor.Demo.Components;
using Microsoft.Extensions.Primitives;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extensions for <see cref="HttpContext"/> related to HTMX.
/// </summary>
public static class HttpContextHtmxExtensions
{
    static string? GetHeaderValue( this HttpRequest request, string key )
    {
        return request.Headers.TryGetValue( key, out StringValues values )
            ? values.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Returns the HTMX mode of the current request.
    /// </summary>
    /// <param name="request">The current <see cref="HttpRequest"/>.</param>
    public static HtmxMode GetHtmxMode( this HttpRequest request )
    {
        // check whether this is an HTMX request
        if ( request.GetHeaderValue( "HX-Request" ) is not "true" )
            return HtmxMode.None;

        // check whether this is a history cache miss
        // htmx expects a full page in this case
        if ( request.GetHeaderValue( "HX-History-Restore-Request" ) is "true" )
            return HtmxMode.HxHistoryRestoreRequest;

        // check whether this is a boosted link or form
        return request.GetHeaderValue( "HX-Boosted" ) is "true"
            ? HtmxMode.HxBoosted
            : HtmxMode.HxRequest;
    }
}
