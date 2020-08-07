// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Web;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// Metadata about the operation being graphed. Used to
    /// generate the graph header.
    /// </summary>
    internal class OperationNode
    {
        /// <summary>
        /// Gets or sets the name of the service being resolved in this operation.
        /// </summary>
        public string? Service { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the overall operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Serializes the top-level operation data to the graph.
        /// </summary>
        /// <param name="stringBuilder">
        /// The <see cref="StringBuilder"/> to which graph content should be written.
        /// </param>
        public void ToString(StringBuilder stringBuilder)
        {
            // Graph header
            stringBuilder.Append("label=<");
            if (!Success)
            {
                stringBuilder.Append("<b>");
            }

            stringBuilder.Append(HttpUtility.HtmlEncode(Service));
            if (!Success)
            {
                stringBuilder.Append("</b>");
            }

            stringBuilder.Append(">;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("labelloc=t");
        }
    }
}
