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
 * This code is based on the source code for xoshiro256** as publicized in:
 * http://xoshiro.di.unimi.it/xoshiro256starstar.c
 * 
 */
using System;
using System.Runtime.CompilerServices;
using Xoshiro.Base;

namespace Xoshiro.PRNG64 {
    /* Comment from original source:
     * 
     * This is xoshiro256** 1.0, our all-purpose, rock-solid generator. It has
     * excellent (sub-ns) speed, a state (256 bits) that is large enough for
     * any parallel application, and it passes all tests we are aware of.
     * For generating just floating-point numbers, xoshiro256+ is even faster.
     * The state must be seeded so that it is not everywhere zero. If you have
     * a 64-bit seed, we suggest to seed a splitmix64 generator and use its
     * output to fill s.
     *
     */
    /// <summary>
    /// A fast PRNG with 256-bit state, suitable for generating all-purpose random numbers
    /// (64-bit integers and double precision floating-point).
    /// </summary>
    /// <remarks>
    /// If, however, one has to generate only 64-bit floating-point numbers (by extracting
    /// the upper 53 bits) xoshiro256+ is a slightly (≈15%) faster generator with analogous
    /// statistical properties. For general usage, one has to consider that [the plus version's]
    /// lowest bits have low linear complexity and will fail linearity tests.
    /// </remarks>
    public sealed class XoShiRo256starstar : Xoshiro64Base {
        #region Internal States

        private UInt64 s0;
        private UInt64 s1;
        private UInt64 s2;
        private UInt64 s3;

        private const int NUM_STATES = 4;

        #endregion Internal States

        #region Constructors

        /// <summary>
        /// Default (Null) Constructor with seed taken from DateTime.UtcNow.Ticks
        /// </summary>
        public XoShiRo256starstar() : this(DateTime.UtcNow.Ticks) { }

        /// <summary>
        /// Constructor with custom seed, which will be expanded into states
        /// by the SplitMix64 algorithm.
        /// </summary>
        /// <param name="seed">seed to initialize the state of SplitMix64</param>
        public XoShiRo256starstar(long seed) {
            UInt64[] s = new UInt64[NUM_STATES];
            SplitMix64 sm64 = new SplitMix64(seed);
            sm64.FillArray64(s);
            s0 = s[0];
            s1 = s[1];
            s2 = s[2];
            s3 = s[3];
        }

        /// <summary>
        /// Constructor with custom initial state.
        /// <para>WARNING: DO NOT use this constructor unless you know exactly
        /// what you're doing!</para>
        /// </summary>
        /// <param name="initialStates">Array (minimum of 4 elements) containing the
        /// initial state</param>
        public XoShiRo256starstar(UInt64[] initialStates) {
            if(initialStates.Length < NUM_STATES) throw new ArgumentException(
               $"initialStates must have at least {NUM_STATES} elements!",
               nameof(initialStates));
            s0 = initialStates[0];
            s1 = initialStates[1];
            s2 = initialStates[2];
            s3 = initialStates[3];
        }

        #endregion Constructors

        /* Overrides */

        /// <summary>
        /// Fetch an Unsigned 64-bit integer from the PRNG.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ulong Next64U() {
            // rotl := (x << k) | (x >> (64 - k))

            // rotl(s[1] * 5, 7) * 9
            UInt64 r1 = (s1 << 2) + s1;
            UInt64 r2 = (r1 << 7) | (r1 >> 57);
            UInt64 rslt = (r2 << 3) + r2;

            UInt64 t = s1 << 17;

            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;

            s2 ^= t;

            // rotl(s[3], 45)
            s3 = (s3 << 45) | (s3 >> 19);

            return rslt;
        }
    }
}
