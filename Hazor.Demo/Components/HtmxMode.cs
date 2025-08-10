namespace Hazor.Demo.Components;

public enum HtmxMode
{
    /// <summary>
    /// Not an HTMX request.
    /// </summary>
    None,

    /// <summary>
    /// The request was made by HTMX.
    /// </summary>
    HxRequest,

    /// <summary>
    /// HTMX boosted link or form.
    /// </summary>
    HxBoosted,

    /// <summary>
    /// HTMX history restore request.
    /// This is expected to return a full page.
    /// </summary>
    HxHistoryRestoreRequest
}
