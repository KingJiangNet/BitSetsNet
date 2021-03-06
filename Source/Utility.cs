﻿using System;

namespace BitsetsNET
{
    class Utility
    {
        private const long Mask1 = 0x5555555555555555; //binary: 0101...
        private const long Mask2 = 0x3333333333333333; //binary: 00110011..
        private const long Mask4 = 0x0f0f0f0f0f0f0f0f; //binary:  4 zeros,  4 ones ...
        private const long H01 = 0x0101010101010101; //the sum of 256 to the power of 0,1,2,3...

        /// <summary>
        /// Gets the 16 highest-order bits from an integer.
        /// </summary>
        /// <param name="x">The integer to shift</param>
        /// <returns>The highest-order bits</returns>
        public static ushort GetHighBits(int x)
        {
            uint u = (uint)(x);
            return (ushort) (u >> 16);
        }

        /// <summary>
        /// Gets the 16 lowest-order bits from an integer.
        /// </summary>
        /// <param name="x">The integer to shift</param>
        /// <returns>The lowest-order bits</returns>
        public static ushort GetLowBits(int x)
        {
            return (ushort)(x & 0xFFFF);
        }

        public static int GetMaxLowBitAsInteger()
        {
            return (int) 0xFFFF;
        }

        public static int ToIntUnsigned(ushort x)
        {
            return (int) x;
        }

        public static void FlipBitsetRange(long[] bitset, int start, int end)
        {
            if (start == end)
            {
                return;
            }
            uint firstword = (uint) (start / 64);
            uint endword = (uint) (end - 1) / 64;
            bitset[firstword] ^= ~(long)(ulong.MaxValue << start);
            for (uint i = firstword; i < endword; i++)
            {
                bitset[i] = ~bitset[i];
            }

            bitset[endword] ^= (long)(ulong.MaxValue >> -end);
        }


        public static int FlipBitsetRangeAndCardinalityChange(long[] bitmap, int start, int end)
        {
            int cardbefore = CardinalityInBitmapWordRange(bitmap, start, end);
            FlipBitsetRange(bitmap, start, end);
            int cardafter = CardinalityInBitmapWordRange(bitmap, start, end);
            return cardafter - cardbefore;
        }

        /// <summary>
        /// Hamming weight of the 64-bit words involved in the range
        /// start, start+1,..., end-1.
        /// </summary>
        /// <param name="bitmap">Bitmap array of words to be modified</param>
        /// <param name="start">First index to be modified (inclusive)</param>
        /// <param name="end">Last index to be modified (exclusive)</param>
        /// <returns>Cardinality in specified range</returns>
        public static int CardinalityInBitmapWordRange(long[] bitmap, int start, int end)
        {
            if (start == end)
            {
                return 0;
            }
            int firstword = start / 64;
            int endword = (end - 1) / 64;
            int answer = 0;
            for (int i = firstword; i <= endword; i++)
            {
                answer += LongBitCount(bitmap[i]);
            }
            return answer;
        }

        public static int UnsignedBinarySearch(ushort[] array, int begin, int end, ushort key)
        {
            //optimizes for the case where the value is inserted at the end
            if ((end > 0) && (array[end - 1] < key))
            {
                return -end - 1;
            }

            int low = begin;
            int high = end - 1;
            while (low <= high)
            {
                // divide by 2 to find the middle index
                int middleIndex = (low + high) >> 1;
                ushort middleValue = array[middleIndex];

                if (middleValue < key)
                {
                    low = middleIndex + 1;
                }
                else if (middleValue > key)
                {
                    high = middleIndex - 1;
                }
                else
                {
                    return middleIndex;
                }
            }
            return -(low + 1);
        }
        
        ///// <summary>
        ///// Naive implementation to count the number of true bits in a word.
        ///// </summary>
        ///// <param name="w">word</param>
        ///// <returns>The number of true bits in the word</returns>
        //public static int LongBitCount(long w)
        //{
        //    int rtnVal = 0;
        //    ulong word = (ulong)w;

        //    for (; word > 0; rtnVal++)
        //    {
        //        word &= word - 1;
        //    }

        //    return rtnVal;
        //}

        public static int LongBitCount(long n)
        {
            if (n == 0)
                return 0;

            n -= (n >> 1) & Mask1;             // Put count of each 2 bits into those 2 bits
            n = (n & Mask2) + ((n >> 2) & Mask2); // Put count of each 4 bits into those 4 bits 
            n = (n + (n >> 4)) & Mask4;        // Put count of each 8 bits into those 8 bits 

            return (int)((n * H01) >> 56);  // Returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
        }

        /// <summary>
        /// Compute the bitwise AND between two long arrays and write the set
        /// bits in the container.
        /// </summary>
        /// <param name="container">Container to write to</param>
        /// <param name="bitmap1">First bitmap</param>
        /// <param name="bitmap2">Second bitmap</param>
        public static void FillArrayAND(ref ushort[] container, long[] bitmap1, long[] bitmap2)
        {
            int pos = 0;

            if (bitmap1.Length != bitmap2.Length)
            {
                throw new ArgumentOutOfRangeException("not supported");
            }

            for (int k = 0; k < bitmap1.Length; ++k)
            {
                long bitset = bitmap1[k] & bitmap2[k];

                while (bitset != 0) {
                    long t = bitset & -bitset;
                    container[pos++] = (ushort) (k * 64 + LongBitCount(t - 1));
                    bitset ^= t;
                }
            }
        }
        
        /// <summary>
        /// Computes the bitwise ANDNOT between two long arrays and writes
        /// the set bits in the container.
        /// </summary>
        /// <param name="container"> Writing here</param>
        /// <param name="bitmap1">First bitmap</param>
        /// <param name="bitmap2">Second bitmap</param>
        public static void FillArrayANDNOT(ushort[] container, long[] bitmap1, long[] bitmap2)
        {
            int pos = 0;
            if (bitmap1.Length != bitmap2.Length)
            {
                throw new ArgumentOutOfRangeException("Bitmaps need to be the same length");
            }
            for (int k = 0; k < bitmap1.Length; ++k)
            {
                long bitset = bitmap1[k] & (~bitmap2[k]);
                while (bitset != 0)
                {
                    long t = bitset & -bitset;
                    container[pos++] = (ushort)(k * 64 + Utility.LongBitCount(t - 1));
                    bitset ^= t;
                }
            }
        }

        /// <summary>
        /// Unite two sorted lists and write the result to the provided
        /// output array
        /// </summary>
        /// <param name="set1">first array</param>
        /// <param name="length1">length of first array</param>
        /// <param name="set2">second array</param>
        /// <param name="length2">length of second array</param>
        /// <param name="buffer">output array</param>
        /// <returns>cardinality of the union</returns>
        public static int UnsignedUnion2by2(ushort[] set1, int length1, 
                                            ushort[] set2, int length2,
                                            ushort[] buffer)
        {
            int pos = 0;
            int k1 = 0, k2 = 0;
            if (0 == length2)
            {
                Array.Copy(set1, 0, buffer, 0, length1);
                return length1;
            }
            if (0 == length1)
            {
                Array.Copy(set2, 0, buffer, 0, length2);
                return length2;
            }
            ushort s1 = set1[k1];
            ushort s2 = set2[k2];
            while (true)
            {
                int v1 = s1;
                int v2 = s2;
                if (v1 < v2)
                {
                    buffer[pos++] = s1;
                    ++k1;
                    if (k1 >= length1)
                    {
                        Array.Copy(set2, k2, buffer, pos, length2 - k2);
                        return pos + length2 - k2;
                    }
                    s1 = set1[k1];
                }
                else if (v1 == v2)
                {
                    buffer[pos++] = s1;
                    ++k1;
                    ++k2;
                    if (k1 >= length1)
                    {
                        Array.Copy(set2, k2, buffer, pos, length2 - k2);
                        return pos + length2 - k2;
                    }
                    if (k2 >= length2)
                    {
                        Array.Copy(set1, k1, buffer, pos, length1 - k1);
                        return pos + length1 - k1;
                    }
                    s1 = set1[k1];
                    s2 = set2[k2];
                }
                else
                {// if (set1[k1]>set2[k2])
                    buffer[pos++] = s2;
                    ++k2;
                    if (k2 >= length2)
                    {
                        Array.Copy(set1, k1, buffer, pos, length1 - k1);
                        return pos + length1 - k1;
                    }
                    s2 = set2[k2];
                }
            }
            //return pos;
        }


        /// <summary>
        /// Intersect two sorted lists and write the result to the provided
        /// output array
        /// </summary>
        /// <param name="set1">first array</param>
        /// <param name="length1">length of first array</param>
        /// <param name="set2">second array</param>
        /// <param name="length2">length of second array</param>
        /// <param name="buffer">output array</param>
        /// <returns>cardinality of the intersection</returns>
        public static int UnsignedIntersect2by2(ushort[] set1, int length1, 
                                                ushort[] set2, int length2,
                                                ushort[] buffer)
        {
            if (set1.Length * 64 < set2.Length)
            {
                return UnsignedOneSidedGallopingIntersect2by2(set1, length1, set2, length2, buffer);
            }
            else if (set2.Length * 64 < set1.Length)
            {
                return UnsignedOneSidedGallopingIntersect2by2(set2, length2, set1, length1, buffer);
            }
            else
            {
                return UnsignedLocalIntersect2by2(set1, length1, set2, length2, buffer);
            }
        }

        protected static int UnsignedLocalIntersect2by2(ushort[] set1,
                                                        int length1, ushort[] set2, int length2,
                                                        ushort[] buffer) {
            if ((0 == length1) || (0 == length2))
                return 0;
            int k1 = 0;
            int k2 = 0;
            int pos = 0;
            ushort s1 = set1[k1];
            ushort s2 = set2[k2];

            bool breakflag = false;
            while (!breakflag)
            {
                int v1 = s1;
                int v2 = s2;
                if (v2 < v1)
                {
                    do
                    {
                        ++k2;
                        if (k2 == length2)
                        {
                            breakflag = true;
                            break;
                        }

                        s2 = set2[k2];
                        v2 = s2;
                    } while (v2 < v1);
                }
                else if (v1 < v2)
                {
                    do
                    {
                        ++k1;
                        if (k1 == length1)
                        {
                            breakflag = true;
                            break;
                        }

                        s1 = set1[k1];
                        v1 = s1;
                    } while (v1 < v2);
                }
                else
                {
                    // (set2[k2] == set1[k1])
                    buffer[pos++] = s1;
                    ++k1;
                    if (k1 == length1)
                    {
                        break;
                    }
                    ++k2;
                    if (k2 == length2)
                    {
                        break;
                    }
                    s1 = set1[k1];
                    s2 = set2[k2];
                }
            }
            return pos;
        }

        protected static int UnsignedOneSidedGallopingIntersect2by2(ushort[] smallSet, int smallLength,
                                                                    ushort[] largeSet, int largeLength,
                                                                    ushort[] buffer)
        {
            if (0 == smallLength)
            {
                return 0;
            }
            
            int k1 = 0;
            int k2 = 0;
            int pos = 0;
            ushort s1 = largeSet[k1];
            ushort s2 = smallSet[k2];

            while (true)
            {
                if (s1 < s2)
                {
                    k1 = AdvanceUntil(largeSet, k1, largeLength, s2);
                    if (k1 == largeLength)
                    {
                        break;
                    }
                    s1 = largeSet[k1];
                }
                if (s2 < s1)
                {
                    ++k2;
                    if (k2 == smallLength)
                    {
                        break;
                    }
                    s2 = smallSet[k2];
                }
                else
                {
                    // (set2[k2] == set1[k1])
                    buffer[pos++] = s2;
                    ++k2;
                    if (k2 == smallLength)
                    {
                        break;
                    }
                    s2 = smallSet[k2];
                    k1 = AdvanceUntil(largeSet, k1, largeLength, s2);
                    if (k1 == largeLength)
                    {
                        break;
                    }
                    s1 = largeSet[k1];
                }

            }
            return pos;

        }

        /// <summary>
        /// Compares the two specified unsigned short values, treating them as
        /// unsigned values between 0 and 2^16 - 1 inclusive.
        /// </summary>
        /// <param name="a">the first unsigned short to compare</param>
        /// <param name="b">the second unsigned short to compare</param>
        /// <returns>A negative value if a is less than b, a positive value if a is 
        /// greater than b, or zero if they are equal</returns>
        public static uint CompareUnsigned(ushort a, ushort b)
        {
            return (uint) (ToIntUnsigned(a) - ToIntUnsigned(b));
        }

        /// <summary>
        /// Compute the difference between two sorted lists and write the result to the provided
        /// output array
        /// </summary>
        /// <returns>Cardinality of the difference</returns>
        public static int UnsignedDifference(ushort[] set1, int length1,
                                             ushort[] set2, int length2,
                                             ushort[] buffer)
        {
            int pos = 0;
            int k1 = 0, k2 = 0;

            // If nothing to diff with, use original cardinality
            if (length2 == 0)
            {
                Array.Copy(set1, 0, buffer, 0, length1);
                return length1;
            }

            // Cardinality must be zero
            if (length1 == 0)
            {
                return 0;
            }

            ushort s1 = set1[k1];
            ushort s2 = set2[k2];
            while (true)
            {
                if (ToIntUnsigned(s1) < ToIntUnsigned(s2))
                {
                    buffer[pos++] = s1;
                    ++k1;
                    if (k1 >= length1)
                    {
                        break;
                    }
                    s1 = set1[k1];
                }
                else if (ToIntUnsigned(s1) == ToIntUnsigned(s2))
                {
                    ++k1;
                    ++k2;
                    if (k1 >= length1)
                    {
                        break;
                    }
                    if (k2 >= length2)
                    {
                        Array.Copy(set1, k1, buffer, pos, length1 - k1);
                        return pos + length1 - k1;
                    }
                    s1 = set1[k1];
                    s2 = set2[k2];
                }
                else
                {
                    ++k2;
                    if (k2 >= length2)
                    {
                        Array.Copy(set1, k1, buffer, pos, length1 - k1);
                        return pos + length1 - k1;
                    }
                    s2 = set2[k2];
                }
            }
            return pos;
        }

        /// <summary>
        /// This is an Array extension method analogous to Java's Array.fill().
        /// Fills a certain range of array indices with a specific value.
        /// </summary>
        /// <param name="array">array to modify</param>
        /// <param name="start">the starting index</param>
        /// <param name="end">the ending index</param>
        /// <param name="value">value to set</param>
        public static void Fill<T>(T[] array, int start, int end, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (start < 0 || start > end)
            {
                throw new ArgumentOutOfRangeException("fromIndex");
            }
            if (end > array.Length)
            {
                throw new ArgumentOutOfRangeException("toIndex");
            }
            for (int i = start; i < end; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Find the smallest integer larger than pos such that array[pos] >= min.
        /// If none can be found, return length. Based on code by O. Kaser.
        /// </summary>
        /// <param name="array">array to search within</param>
        /// <param name="pos">starting position of the search</param>
        /// <param name="length">length of the array to search</param>
        /// <param name="min">minimum value</param>
        /// <returns>x greater than pos such that array[pos] is at least as large
        /// as min, pos is is equal to length if it is not possible.</returns>
        public static int AdvanceUntil(ushort[] array, int pos, int length, ushort min)
        {
            int lower = pos + 1;

            // special handling for a possibly common sequential case
            if (lower >= length || array[lower] >= min)
            {
                return lower;
            }

            int spansize = 1; // could set larger
            // bootstrap an upper limit

            while (lower + spansize < length && ToIntUnsigned(array[lower + spansize]) < ToIntUnsigned(min))
            {
                spansize *= 2; // hoping for compiler will reduce to shift
            }
            int upper = (lower + spansize < length) ? lower + spansize : length - 1;

            // maybe we are lucky (could be common case when the seek ahead
            // expected
            // to be small and sequential will otherwise make us look bad)
            if (array[upper] == min)
            {
                return upper;
            }

            if (ToIntUnsigned(array[upper]) < ToIntUnsigned(min))
            {// means
                // array
                // has no
                // item
                // >= min
                // pos = array.length;
                return length;
            }

            // we know that the next-smallest span was too small
            lower += (spansize / 2);

            // else begin binary search
            // invariant: array[lower]<min && array[upper]>min
            while (lower + 1 != upper)
            {
                int mid = (lower + upper) / 2;
                ushort arraymid = array[mid];
                if (arraymid == min)
                {
                    return mid;
                }
                else if (arraymid < min)
                    lower = mid;
                else
                    upper = mid;
            }
            return upper;

        }

        public static int Select(long w, int j)
        {
            ulong word = (ulong) w;
            int seen = 0;
            // Divide 64bit
            uint part = (uint) word & 0xFFFFFFFF;
            int n = LongBitCount(part);
            if (n <= j)
            {
                part = (uint) (word >> 32);
                seen += 32;
                j -= n;
            }
            uint ww = part;

            // Divide 32bit
            part = ww & 0xFFFF;

            n = LongBitCount(part);
            if (n <= j)
            {

                part = ww >> 16;
                seen += 16;
                j -= n;
            }
            ww = part;

            // Divide 16bit
            part = ww & 0xFF;
            n = LongBitCount(part);
            if (n <= j)
            {
                part = ww >> 8;
                seen += 8;
                j -= n;
            }
            ww = part;

            // Lookup in final byte
            int counter;
            for (counter = 0; counter < 8; counter++)
            {
                j -= (int)(ww >> counter) & 1;
                if (j < 0)
                {
                    break;
                }
            }
            return seen + counter;
        }
        
        /// <summary>
        /// clear bits at start, start+1,..., end-1
        /// </summary>
        /// <param name="bitmap">bitmap array of words to be modified</param>
        /// <param name="start">start first index to be modified (inclusive)</param>
        /// <param name="end">end last index to be modified (exclusive)</param>
        public static void ResetBitmapRange(long[] bitmap, int start, int end)
        {
            if (start == end)
            {
                return;
            }

            int firstword = start / 64;
            int endword = (end - 1) / 64;

            if (firstword == endword)
            {
                bitmap[firstword] &= ~((~0L << start) & (long)(~0UL >> -end));
                return;
            }

            bitmap[firstword] &= ~(~0L << start);

            for (int i = firstword + 1; i < endword; i++)
            {
                bitmap[i] = 0;
            }

            bitmap[endword] &= (long)~(~0UL >> -end);
        }

        /// <summary>
        /// set bits at start, start+1,..., end-1
        /// </summary>
        /// <param name="bitmap">array of words to be modified</param>
        /// <param name="start">first index to be modified (inclusive)</param>
        /// <param name="end">last index to be modified (exclusive)</param>
        public static void SetBitmapRange(long[] bitmap, ushort start, ushort end)
        {
            if (start == end) return;

            int firstWord = start / 64;
            int endWord = (end - 1) / 64;

            if (firstWord == endWord)
            {
                bitmap[firstWord] |= (~0L << start) & (long)(~0UL >> -end);
                return;
            }

            bitmap[firstWord] |= ~0L << start;

            for (int i = firstWord + 1; i < endWord; i++)
            {
                bitmap[i] = ~0L;
            }

            bitmap[endWord] |= (long)(~0UL >> -end);
        }
    }
}
