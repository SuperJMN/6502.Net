﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------


using Newtonsoft.Json.Linq;
using System;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents an error encountered during the building of a 
    /// schema object from parsed JSON.
    /// </summary>
    public sealed class SchemaBuilderException : Exception
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SchemaBuilderException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The <see cref="JToken"/> at the point the error occurred.</param>
        public SchemaBuilderException(string message, JToken token)
            : base($"Error in schema: {message}\nAt: {token.ToJsonPointer()}") => JsonPointer = token.ToJsonPointer();

        /// <summary>
        /// Creates a new instance of a <see cref="SchemaBuilderException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="jsonPointer">The JSON pointer path at the point the error occurrred.</param>
        public SchemaBuilderException(string message, string jsonPointer)
            : base(message) => JsonPointer = jsonPointer;

        /// <summary>
        /// Gets the JSON pointer of the path the error occurred.
        /// </summary>
        public string JsonPointer { get; }
    }
}
