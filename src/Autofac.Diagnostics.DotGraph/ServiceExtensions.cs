// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// Extension methods for building DOT graph components from services.
    /// </summary>
    internal static class ServiceExtensions
    {
        /// <summary>
        /// Gets the display name for a service that should be used in a graph.
        /// </summary>
        /// <param name="service">The service for which a display name should be retrieved.</param>
        /// <returns>A <see cref="string"/> with a human-readable, pretty-printed display name for use in a graph node or label.</returns>
        public static string GraphDisplayName(this Service service)
        {
            return service switch
            {
                IServiceWithType swt => swt.ServiceType.CSharpName(),
                _ => service.Description
            };
        }
    }
}
