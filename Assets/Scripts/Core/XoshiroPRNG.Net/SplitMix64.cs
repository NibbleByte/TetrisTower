/* This code is released by Pandu POLUAN into the Public Domain, OR using
 * one of the following licenses according to your preference:
 *   - The Unlicense
 *     https://spdx.org/licenses/Unlicense.html
 *   - Creative Commons Zero v1.0 Universal
 *     https://spdx.org/licenses/CC0-1.0.html
 *   - Do What The F*ck You Want Public License
 *     https://spdx.org/licenses/WTFPL.html
 *   - MIT-0 License
 *     https://spdx.org/licenses/MIT-0.html
 *   - BSD 3-Clause License
 *     https://spdx.org/licenses/BSD-3-Clause.html
 * 
 * This code is based on the source code for SplitMix64 as publicized in:
 * http://xoshiro.di.unimi.it/splitmix64.c
 * 
 */
using System;

namespace Xoshiro.Base {

    /* Comment from original source:
     * 
     * This is a fixed-increment version of Java 8's SplittableRandom generator
     * See http://dx.doi.org/10.1145/2714064.2660195 and 
     * http://docs.oracle.com/javase/8/docs/api/java/util/SplittableRandom.html
     *
     * It is a very fast generator passing BigCrush, and it can be useful if
     * for some reason you absolutely want 64 bits of state; otherwise, we
     * rather suggest to use a xoroshiro128+ (for moderately parallel
     * computations) or xorshift1024* (for massively parallel computations)
     * generator.
     */

    /// <summary>
    /// A generator to quickly generate 64- or 32-bit states for the PRNGs.
    /// DO NOT USE THIS CLASS AS A PRNG.
    /// </summary>
    public sealed class SplitMix64 {

        /* Primitive Properties */
        /// <summary>
        /// Specifies the method used to fold 64-bit integers into 32-bit integers.
        /// </summary>
        public Fold64To32Method FoldMethod;

        /* State */
        private UInt64 x;

        /* Constructor - NO Null Constructor */
        /// <summary>
        /// Instantiates a SplitMix64 object with a given initial state.
        /// </summary>
        /// <param name="seed">Initial State</param>
        /// <param name="foldMethod"><see cref="Fold64To32Method"/></param>
        public SplitMix64(long seed,
            Fold64To32Method foldMethod = Fold64To32Method.ChunkMethod) {
            x = (UInt64)seed;
            this.FoldMethod = foldMethod;
        }

        /* Public Methods */

        /// <summary>
        /// Get one UInt64 number from the SplitMix64 generator
        /// </summary>
        public UInt64 Next() {
            UInt64 z = (x += 0x9e3779b97f4a7c15);
            z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
            z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
            return z ^ (z >> 31);
        }

        /// <summary>
        /// Fill an array of UInt64 with the next numbers from the SplitMix64 generator
        /// </summary>
        /// <param name="arr">Must not be null</param>
        public void FillArray64(UInt64[] arr) {
            if(arr == null) throw new ArgumentNullException(
               nameof(arr), "arr cannot be null!");
            for(int i = 0; i < arr.Length; i++) {
                arr[i] = Next();
            }
        }

        /// <summary>
        /// Fill an array of UInt32 by folding the next numbers from the SplitMix64
        /// generator
        /// </summary>
        /// <param name="arr">Must not be null</param>
        public void FillArray32(UInt32[] arr) {
            if(arr == null) throw new ArgumentNullException(
               nameof(arr), "arr cannot be null!");
            UInt32? nextChunk = null;
            for(int i = 0; i < arr.Length; i++) {
                if(FoldMethod == Fold64To32Method.XorMethod) {
                    UInt64 n = Next();
                    arr[i] = (UInt32)(n & 0xFFFFFFFF) ^ (UInt32)(n >> 32);
                }
                else if(FoldMethod == Fold64To32Method.ChunkMethod) {
                    if(nextChunk == null) {
                        UInt64 n = Next();
                        nextChunk = (UInt32)(n & 0xFFFFFFFF);
                        arr[i] = (UInt32)(n >> 32);
                    }
                    else {
                        arr[i] = (UInt32)nextChunk;
                        nextChunk = null;
                    }
                }
                else throw new NotSupportedException("Unrecognized Fold64To32 method!");
            }
        }

    }
}
