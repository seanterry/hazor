using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Http;

public static class RouteHelper
{
    /// <summary>
    /// Collection of already-tokenized route templates.
    /// This contains the route template as the index key and a collection of placeholders as the value.
    /// </summary>
    static readonly ConcurrentDictionary<string,ConcurrentDictionary<string,string>> RouteTemplates = new();

    /// <summary>
    /// Initializer for <see cref="RouteTemplates"/>.
    /// </summary>
    static ConcurrentDictionary<string, string> GetRoutePlaceholders( string route )
    {
        // split the route path into tokens
        string[] tokens = route.Split( '/', StringSplitOptions.RemoveEmptyEntries );
        ConcurrentDictionary<string, string> placeholders = new();

        foreach ( string token in tokens )
        {
            // skip placeholder checking if the token doesn't contain a route parameter
            if ( !token.StartsWith( '{' ) || !token.EndsWith( '}' ) ) continue;

            string placeholder = token[1..^1];

            // check if the placeholder has a constraint (e.g., {id:int})
            // if it does, we only want the placeholder name, not the constraint
            if ( placeholder.IndexOf( ':' ) is int constraint and >= 0 )
                placeholder = placeholder[..constraint];

            placeholders[placeholder] = token;
        }

        return placeholders;
    }

    /// <summary>
    /// Creates a relative URL for a route with the specified values.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="route">Route whose URL to generate.</param>
    /// <param name="values">Route and query parameters indexed by name.</param>
    /// <param name="includePathBase">
    /// Whether to include the <see cref="HttpRequest.PathBase"/> in the URL. Defaults to <c>true</c>.
    /// This is useful for applications hosted in a subdirectory.
    /// </param>
    /// <returns>The completed relative URL.</returns>
    static string UrlForRoute( this HttpContext context, string route, RouteValueDictionary? values = null, bool includePathBase = true )
    {
        values ??= new();
        var placeholders = RouteTemplates.GetOrAdd( route, GetRoutePlaceholders );
        StringBuilder pathBuilder = new StringBuilder( route );
        List<string> queryBuilder = [];

        foreach ( (string key, object? value) in values )
        {
            if ( value is null ) continue;
            string? output = WebUtility.UrlEncode( value.ToString() );

            // place values in the route path if they match a placeholder
            // otherwise, add them to the query string
            if ( placeholders.TryGetValue( key, out string? template ) )
                pathBuilder.Replace( template, output );
            else
                queryBuilder.Add( $"{key}={output}" );
        }

        if ( queryBuilder.Count != 0 )
        {
            pathBuilder.Append( '?' );
            pathBuilder.Append( string.Join( '&', queryBuilder ) );
        }

        if ( includePathBase ) pathBuilder.Insert( 0, context.Request.PathBase );

        return pathBuilder.ToString();
    }

    /// <summary>
    /// Cache of discovered routes for components.
    /// </summary>
    static readonly ConcurrentDictionary<Type, string> RoutesForComponents = new();

    /// <summary>
    /// Gets the route template from the <see cref="RouteAttribute"/> of a component type.
    /// </summary>
    static string GetComponentRoute( Type type )
    {
        if ( type.GetCustomAttribute<RouteAttribute>() is not {} attribute )
            throw new InvalidOperationException( $"The type '{type.FullName}' does not have a RouteAttribute attribute." );

        if ( string.IsNullOrWhiteSpace( attribute.Template ) )
            throw new InvalidOperationException( $"The type '{type.FullName}' has an invalid RouteAttribute template." );

        return attribute.Template;
    }

    /// <summary>
    /// Returns a relative URL for the route associated with the specified component type.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="values">Route and query values.</param>
    /// <param name="includePathBase">
    /// Whether to include the <see cref="HttpRequest.PathBase"/> in the URL. Defaults to <c>true</c>.
    /// </param>
    /// <typeparam name="TComponent">The type of the component whose URL to return.</typeparam>
    /// <returns>The relative URL for the route.</returns>
    public static string UrlForComponent<TComponent>( this HttpContext context, RouteValueDictionary? values = null, bool includePathBase = true )
        where TComponent : ComponentBase
    {
        string route = RoutesForComponents.GetOrAdd( typeof(TComponent), GetComponentRoute );
        return context.UrlForRoute( route, values, includePathBase );
    }
}
