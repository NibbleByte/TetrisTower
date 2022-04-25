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
 */
using System;

namespace Xoshiro.Base {
    /// <summary>
    /// The "Unleashed" interface for 32-bit PRNGs: This interface enables
    /// fetching of higher-strength full-range random numbers direct from the 
    /// PRNG's algorithm.
    /// </summary>
    public interface IRandomU {
        #region System.Random-compatible interface

        /// <summary>
        /// Returns a non-negative 32-bit integer from the PRNG, in the range of [0, int.MaxValue)
        /// </summary>
        int Next();

        /// <summary>
        /// Returns a non-negative 32-bit integer from the PRNG, in the range of [0, maxValue)
        /// </summary>
        /// <param name="maxValue">Must be &gt;0</param>
        int Next(Int32 maxValue);

        /// <summary>
        /// Returns the next 32-bit integer from the PRNG, in the range of [minValue, maxValue).
        /// Can be negative.
        /// </summary>
        /// <param name="minValue">Must be &lt;= maxValue. Can be negative.</param>
        /// <param name="maxValue">Must be &gt;= minValue. Can be negative</param>
        int Next(Int32 minValue, Int32 maxValue);

        /// <summary>
        /// Fills an array of bytes with the octets of the next number(s) from the PRNG
        /// </summary>
        /// <param name="buffer">Must NOT be null</param>
        void NextBytes(byte[] buffer);

        /// <summary>
        /// Returns a double precision floating-point number from the PRNG.
        /// Range is [0, 1.0)
        /// </summary>
        double NextDouble();

        #endregion System.Random-compatible interface

        #region Unleashed interface

        /// <summary>
        /// Returns a single precision floating-point number by using 24 of the 32 bits
        /// of the next number from the PRNG. Range is [0, 1.0)
        /// </summary>
        float NextFloat();

        /// <summary>
        /// Returns an unsigned 32-bit integer in the range of [0, UInt32.MaxValue]
        /// inclusive at both ends ("full range")
        /// </summary>
        UInt32 NextU();

        /// <summary>
        /// Returns an unsigned 32-bit integer in the range of [0, maxValue)
        /// </summary>
        /// <param name="maxValue">Must be &gt;1</param>
        UInt32 NextU(UInt32 maxValue);

        /// <summary>
        /// Returns an unsigned 32-bit integer in the range of [minValue, maxValue)
        /// </summary>
        /// <param name="minValue">Must be &lt; maxValue-1</param>
        /// <param name="maxValue">Must be &gt; minValue+1</param>
        UInt32 NextU(UInt32 minValue, UInt32 maxValue);

        /// <summary>
        /// Returns a signed 64-bit integer (63 bits of randomness) in the range of
        /// [0, Int64.MaxValue] inclusive at both ends ("full range")
        /// </summary>
        Int64 Next64();

        /// <summary>
        /// Returns a signed 64-bit integer (63 bits of randomness) in the range of
        /// [0, Int64.MaxValue)
        /// </summary>
        /// <param name="maxValue">Must be &gt;1</param>
        Int64 Next64(Int64 maxValue);

        /// <summary>
        /// Returns a signed 64-bit integer (63 bits of randomness) in the range of
        /// [minValue, maxValue), can be negative.
        /// </summary>
        /// <param name="minValue">Must be &lt; maxValue-1; can be negative</param>
        /// <param name="maxValue">Must be &gt; minValue+1; can be negative</param>
        Int64 Next64(Int64 minValue, Int64 maxValue);

        /// <summary>
        /// Returns an unsigned 64-bit integer in the range of [0, UInt64.MaxValue]
        /// inclusive at both ends.
        /// </summary>
        UInt64 Next64U();

        /// <summary>
        /// Returns an unsigned 64-bit integer in the range of [0, maxValue)
        /// </summary>
        /// <param name="maxValue">Must be &gt;1</param>
        UInt64 Next64U(UInt64 maxValue);

        /// <summary>
        /// Returns an unsigned 64-bit integer in the range of [minValue, maxValue)
        /// </summary>
        UInt64 Next64U(UInt64 minValue, UInt64 maxValue);

        /// <summary>
        /// Get a System.Random-compatible interface
        /// </summary>
        /// <returns></returns>
        Random GetRandomCompatible();

        #endregion Unleashed interface
    }

    /// <summary>
    /// The "Unleashed" interface for 64-bit PRNGs: This interface enables
    /// fetching of higher-strength full-range random numbers direct from the 
    /// PRNG's algorithm.
    /// </summary>
    public interface IRandom64U : IRandomU {
        #region Unleashed interface

        /// <summary>
        /// Specifies how to 'fold' 64-bit numbers generated by the PRNG into
        /// 32-bit numbers. <para>See <see cref="Fold64To32Method"/> for an explanation
        /// of the methods</para>
        /// </summary>
        /// <see cref="Fold64To32Method"/>
        Fold64To32Method FoldMethod { get; set; }

        #endregion Unleashed interface
    }

    /// <summary>
    /// Methods of folding a 64-bit value into a 32-bit value
    /// </summary>
    public enum Fold64To32Method {
        /// <summary>
        /// XOR the upper and lower parts of the 64-bit value into a 32-bit value
        /// </summary>
        XorMethod,
        /// <summary>
        /// Returns the upper 32-bit part first, then the next iteration return the
        /// lower part
        /// </summary>
        ChunkMethod
    }
}
