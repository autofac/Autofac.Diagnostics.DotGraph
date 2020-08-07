// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// Convenience collection for accessing a request by ID
    /// out of the list of all requests.
    /// </summary>
    internal class RequestDictionary : KeyedCollection<Guid, ResolveRequestNode>
    {
        /// <inheritdoc/>
        protected override Guid GetKeyForItem(ResolveRequestNode item)
        {
            return item.Id;
        }
    }
}
