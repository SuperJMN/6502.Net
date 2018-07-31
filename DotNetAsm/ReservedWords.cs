﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetAsm
{
    /// <summary>
    /// A dictionary of reserved words.
    /// </summary>
    public class ReservedWords
    {
        #region Members

        HashSet<string> _values;

        Dictionary<string, HashSet<string>> _types;

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="T:DotNetAsm.ReservedWords"/> class object.
        /// </summary>
        /// <param name="comparer">A <see cref="T:System.StringComparison"/> object to indicate whether
        /// to enforce case-sensitivity.</param>
        public ReservedWords(StringComparison comparer)
        {
            _types = new Dictionary<string, HashSet<string>>();
            Comparer = comparer;
            _values = new HashSet<string>();
        }

        /// <summary>
        /// Instantiates a new <see cref="T:DotNetAsm.ReservedWords"/> class object.
        /// </summary>
        public ReservedWords() :
            this(StringComparison.Ordinal)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Add a reserved word to a defined type.
        /// </summary>
        /// <param name="type">The defined type</param>
        /// <param name="word">The reserved word to include</param>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">System.Collections.Generic.KeyNotFoundException
        /// </exception>
        public void AddWord(string type, string word)
        {
            var t = _types[type];
            t.Add(word);
            _values.Add(word);
        }

        /// <summary>
        /// Defie a type of reserved words.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public void DefineType(string type) => _types.Add(type, new HashSet<string>());

        /// <summary>
        /// Define a type of reserved words.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <param name="values">The collection of values that comprise the type. </param>
        /// <exception cref="T:System.ArgumentNullException">System.ArgumentNullException</exception>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public void DefineType(string type, params string[] values)
        {
            _types.Add(type, new HashSet<string>(values));
            foreach (var v in values)
                _values.Add(v); // grr!!!
        }

        /// <summary>
        /// Determines if the token is one of the type specified.
        /// </summary>
        /// <param name="type">The type (dictionary key).</param>
        /// <param name="token">The token or keyword.</param>
        /// <returns><c>True</c> if the specified token is one of the specified type, otherwise <c>false</c>.</returns>
        /// <exception cref="T:System.ArgumentNullException">System.ArgumentNullException</exception>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public bool IsOneOf(string type, string token) => _types[type].Any(d => d.Equals(token, Comparer));

        /// <summary>
        /// Determines if the token is in the list of reserved words for all types.
        /// </summary>
        /// <param name="token">The token or keyword.</param>
        /// <returns><c>True</c> if the specified token is in the collection of reserved words,
        /// regardless of type, otherwise <c>false</c>.</returns>
        public bool IsReserved(string token) => _values.Any(s => s.Equals(token, Comparer));

        /// <summary>
        /// Gets the type of the token, if any.
        /// </summary>
        /// <param name="token">The token or keyword.</param>
        /// <returns>The type of the token.</returns>
        public string GetType(string token)
        {
            return _types.FirstOrDefault(kv => kv.Value.Equals(token)).Key;
        }

        /// <summary>
        /// Determines if the ReservedWord object contains a type (key) of reserved words.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>True</c> if the <see cref="T:DotNetAsm.ReservedWord"/> object has the type, 
        /// otherwise <c>false</c>.</returns>
        public bool HasType(string type) => _types.ContainsKey(type);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="T:System.StringComparison"/> for the 
        /// <see cref="T:DotNetAsm.ReservedWords"/> collection.
        /// </summary>
        public StringComparison Comparer { get; set; }

        #endregion
    }
}
