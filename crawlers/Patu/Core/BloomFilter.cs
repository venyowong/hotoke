/* Copyright (c) 2009 Joseph Robert. All rights reserved.
 *
 * This file is part of BloomFilter.NET.
 * 
 * BloomFilter.NET is free software; you can redistribute it and/or 
 * modify it under the terms of the GNU Lesser General Public 
 * License as published by the Free Software Foundation; either 
 * version 3.0 of the License, or (at your option) any later 
 * version.
 * 
 * BloomFilter.NET is distributed in the hope that it will be 
 * useful, but WITHOUT ANY WARRANTY; without even the implied 
 * warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
 * See the GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with BloomFilter.NET.  If not, see 
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Patu
{
    /// <summary>
    /// A Bloom filter is a space-efficient probabilistic data structure 
    /// that is used to test whether an element is a member of a set. False 
    /// positives are possible, but false negatives are not. Elements can 
    /// be added to the set, but not removed.
    /// </summary>
    /// <typeparam name="Type">Data type to be classified</typeparam>
    public class BloomFilter<T>
    {
        private static readonly Regex _fileNameRegex = new Regex("^[^-]+-(\\d+)-(\\d+)-(\\d+).blm$");

        int _bitSize, _numberOfHashes, _setSize;
        BitArray _bitArray;
        public BitArray BitArray{get => this._bitArray;}
        private int count = 0;
        public int Count{get => count;}

        #region Constructors
        /// <summary>
        /// Initializes the bloom filter and sets the optimal number of hashes. 
        /// </summary>
        /// <param name="bitSize">Size of the bloom filter in bits (m)</param>
        /// <param name="setSize">Size of the set (n)</param>
        public BloomFilter(int bitSize, int setSize)
        {
            _bitSize = bitSize;
            _bitArray = new BitArray(bitSize);
            _setSize = setSize;
            _numberOfHashes = OptimalNumberOfHashes(_bitSize, _setSize);
        }

        /// <summary>
        /// Initializes the bloom filter with a manual number of hashes.
        /// </summary>
        /// <param name="bitSize">Size of the bloom filter in bits (m)</param>
        /// <param name="setSize">Size of the set (n)</param>
        /// <param name="numberOfHashes">Number of hashing functions (k)</param>
        public BloomFilter(int bitSize, int setSize, int numberOfHashes)
        {
            _bitSize = bitSize;
            _bitArray = new BitArray(bitSize);
            _setSize = setSize;
            _numberOfHashes = numberOfHashes;
        }

        public BloomFilter(int bitSize, int setSize, int numberOfHashes, BitArray bitArray)
        {
            if(bitArray == null)
            {
                throw new ArgumentNullException(nameof(bitArray));
            }

            this._bitSize = bitSize;
            this._bitArray = bitArray;
            this._setSize = setSize;
            this._numberOfHashes = numberOfHashes;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Number of hashing functions (k)
        /// </summary>
        public int NumberOfHashes
        {
            set
            {
                _numberOfHashes = value;
            }
            get
            {
                return _numberOfHashes;
            }
        }

        /// <summary>
        /// Size of the set (n)
        /// </summary>
        public int SetSize
        {
            set
            {
                _setSize = value;
            }
            get
            {
                return _setSize;
            }
        }

        /// <summary>
        /// Size of the bloom filter in bits (m)
        /// </summary>
        public int BitSize
        {
            set
            {
                _bitSize = value;
            }
            get
            {
                return _bitSize;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds an item to the bloom filter.
        /// </summary>
        /// <param name="item">Item to be added</param>
        public void Add(T item)
        {
            Interlocked.Increment(ref this.count);

            var random = new Random(Hash(item));

            for (int i = 0; i < _numberOfHashes; i++)
                _bitArray[random.Next(_bitSize)] = true;
        }

        /// <summary>
        /// Checks whether an item is probably in the set. False positives 
        /// are possible, but false negatives are not.
        /// </summary>
        /// <param name="item">Item to be checked</param>
        /// <returns>True if the set probably contains the item</returns>
        public bool Contains(T item)
        {
            var random = new Random(Hash(item));

            for (int i = 0; i < _numberOfHashes; i++)
            {
                if (!_bitArray[random.Next(_bitSize)])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if any item in the list is probably in the set.
        /// </summary>
        /// <param name="items">List of items to be checked</param>
        /// <returns>True if the bloom filter contains any of the items in the list</returns>
        public bool ContainsAny(List<T> items)
        {
            foreach (T item in items)
            {
                if (Contains(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if all items in the list are probably in the set.
        /// </summary>
        /// <param name="items">List of items to be checked</param>
        /// <returns>True if the bloom filter contains all of the items in the list</returns>
        public bool ContainsAll(List<T> items)
        {
            foreach (T item in items)
            {
                if (!Contains(item))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Computes the probability of encountering a false positive.
        /// </summary>
        /// <returns>Probability of a false positive</returns>
        public double FalsePositiveProbability()
        {
            return Math.Pow((1 - Math.Exp(-_numberOfHashes * _setSize / (double)_bitSize)), _numberOfHashes);
        }

        public void Save(string directoryPath, string name)
        {
            if(!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if(name.Contains("-"))
            {
                throw new ArgumentException("the name cannot contain '-'");
            }

            var filePath = Path.Combine(directoryPath, $"{name}-{this._bitSize}-{this._numberOfHashes}-{this._setSize}.blm");
            using(var file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                var bytes = new byte[(int)Math.Ceiling((double)this._bitSize / 8)];
                this._bitArray.CopyTo(bytes, 0);
                file.Write(bytes, 0, bytes.Length);
            }
        }

        public static BloomFilter<T> LoadFromFile(string directoryPath, string name)
        {
            if(!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException(directoryPath);
            }

            if(name.Contains("-"))
            {
                throw new ArgumentException("the name cannot contain '-'");
            }

            var filePath = Directory.GetFiles(directoryPath).FirstOrDefault(path => path.Contains(name) && _fileNameRegex.IsMatch(path));
            if(string.IsNullOrWhiteSpace(filePath))
            {
                throw new FileNotFoundException($"not found Bloom file named {name} in {directoryPath}");
            }

            var fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
            var groups = _fileNameRegex.Match(fileName).Groups;
            int.TryParse(groups[1].Value, out int bitSize);
            int.TryParse(groups[2].Value, out int numberOfHashes);
            int.TryParse(groups[3].Value, out int setSize);

            using(var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var bytes = new byte[(int)Math.Ceiling((double)bitSize / 8)];
                file.Read(bytes, 0, bytes.Length);
                var bitArray = new BitArray(bitSize);
                for(int i = 0, j = 0; i < bytes.Length; i++)
                {
                    var bits = new BitArray(new byte[1]{bytes[i]});
                    for(int k = 0; k < bits.Length; k++)
                    {
                        if(j < bitSize)
                        {
                            bitArray.Set(j, bits.Get(k));
                            j++;
                        }
                        else
                        {
                            return new BloomFilter<T>(bitSize, setSize, numberOfHashes, bitArray);
                        }
                    }
                }

                return new BloomFilter<T>(bitSize, setSize, numberOfHashes, bitArray);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Hashing function for an object
        /// </summary>
        /// <param name="item">Any object</param>
        /// <returns>Hash of that object</returns>
        private int Hash(T item) {
            return item.GetHashCode();
        }

        /// <summary>
        /// Calculates the optimal number of hashes based on bloom filter
        /// bit size and set size.
        /// </summary>
        /// <param name="bitSize">Size of the bloom filter in bits (m)</param>
        /// <param name="setSize">Size of the set (n)</param>
        /// <returns>The optimal number of hashes</returns>
        private int OptimalNumberOfHashes(int bitSize, int setSize)
        {
            return (int)Math.Ceiling((bitSize / setSize) * Math.Log(2.0));
        }
        #endregion
    }
}