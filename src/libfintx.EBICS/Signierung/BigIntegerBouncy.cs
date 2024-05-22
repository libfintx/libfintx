using System;
using System.Collections;
using System.Globalization;
using Org.BouncyCastle.Security;

namespace libfintx.EBICS.Signierung
{
    public class BigIntegerBouncy
    {

        private int sign; // -1 means -ve; +1 means +ve; 0 means 0;
        private int[] magnitude; // array of ints with [0] being the most significant
        private int nBits = -1; // cache bitCount() value
        private int nBitLength = -1; // cache bitLength() value
        private static readonly long IMASK = 0xffffffffL;
        private long mQuote = -1L; // -m^(-1) mod b, b = 2^32 (see Montgomery mult.)

        private BigIntegerBouncy()
        {
        }

        private BigIntegerBouncy(int nWords)
        {
            sign = 1;
            magnitude = new int[nWords];
        }

        private BigIntegerBouncy(int signum, int[] mag)
        {
            sign = signum;
            if (mag.Length > 0)
            {
                int i = 0;
                while (i < mag.Length && mag[i] == 0)
                {
                    i++;
                }
                if (i == 0)
                {
                    magnitude = mag;
                }
                else
                {
                    // strip leading 0 bytes
                    int[] newMag = new int[mag.Length - i];
                    Array.Copy(mag, i, newMag, 0, newMag.Length);
                    magnitude = newMag;
                    if (newMag.Length == 0)
                        sign = 0;
                }
            }
            else
            {
                magnitude = mag;
                sign = 0;
            }
        }

        public BigIntegerBouncy(String sval) //throws FormatException
            : this(sval, 10) { }

        public BigIntegerBouncy(String sval, int rdx) //throws FormatException
        {
            if (sval.Length == 0)
            {
                throw new FormatException("Zero length BigInteger");
            }

            NumberStyles style;
            switch (rdx)
            {
                case 10:
                    style = NumberStyles.Integer;
                    break;
                case 16:
                    style = NumberStyles.AllowHexSpecifier;
                    break;
                default:
                    throw new FormatException("Only base 10 or 16 alllowed");
            }


            int index = 0;
            sign = 1;

            if (sval[0] == '-')
            {
                if (sval.Length == 1)
                {
                    throw new FormatException("Zero length BigInteger");
                }

                sign = -1;
                index = 1;
            }

            // strip leading zeros from the string value
            while (index < sval.Length && Int32.Parse(sval[index].ToString(), style) == 0)
            {
                index++;
            }

            if (index >= sval.Length)
            {
                // zero value - we're done
                sign = 0;
                magnitude = new int[0];
                return;
            }

            //////
            // could we work out the max number of ints required to store
            // sval.length digits in the given base, then allocate that
            // storage in one hit?, then generate the magnitude in one hit too?
            //////

            BigIntegerBouncy b = BigIntegerBouncy.ZERO;
            BigIntegerBouncy r = valueOf(rdx);
            while (index < sval.Length)
            {
                // (optimise this by taking chunks of digits instead?)
                b = b.multiply(r).add(valueOf(Int32.Parse(sval[index].ToString(), style)));
                index++;
            }

            magnitude = b.magnitude;
            return;
        }

        public BigIntegerBouncy(byte[] bval) //throws FormatException
        {
            if (bval.Length == 0)
            {
                throw new FormatException("Zero length BigInteger");
            }

            sign = 1;
            if (bval[0] < 0)
            {
                // FIXME:
                int iBval;
                sign = -1;
                // strip leading sign bytes
                for (iBval = 0; iBval < bval.Length && ((sbyte) bval[iBval] == -1); iBval++) ;
                magnitude = new int[(bval.Length - iBval) / 2 + 1];
                // copy bytes to magnitude
                // invert bytes then add one to find magnitude of value
            }
            else
            {
                // strip leading zero bytes and return magnitude bytes
                magnitude = makeMagnitude(bval);
            }
        }

        private int[] makeMagnitude(byte[] bval)
        {
            int i;
            int[] mag;
            int firstSignificant;

            // strip leading zeros
            for (firstSignificant = 0; firstSignificant < bval.Length
                    && bval[firstSignificant] == 0; firstSignificant++) ;

            if (firstSignificant >= bval.Length)
            {
                return new int[0];
            }

            int nInts = (bval.Length - firstSignificant + 3) / 4;
            int bCount = (bval.Length - firstSignificant) % 4;
            if (bCount == 0)
                bCount = 4;

            mag = new int[nInts];
            int v = 0;
            int magnitudeIndex = 0;
            for (i = firstSignificant; i < bval.Length; i++)
            {
                v <<= 8;
                v |= bval[i] & 0xff;
                bCount--;
                if (bCount <= 0)
                {
                    mag[magnitudeIndex] = v;
                    magnitudeIndex++;
                    bCount = 4;
                    v = 0;
                }
            }

            if (magnitudeIndex < mag.Length)
            {
                mag[magnitudeIndex] = v;
            }

            return mag;
        }

        public BigIntegerBouncy(int sign, byte[] mag) //throws FormatException
        {
            if (sign < -1 || sign > 1)
            {
                throw new FormatException("Invalid sign value");
            }

            if (sign == 0)
            {
                this.sign = 0;
                this.magnitude = new int[0];
                return;
            }

            // copy bytes
            this.magnitude = makeMagnitude(mag);
            this.sign = sign;
        }

        public BigIntegerBouncy(int numBits, Random rnd) //throws ArgumentException
        {
            if (numBits < 0)
            {
                throw new ArgumentException("numBits must be non-negative");
            }

            int nBytes = (numBits + 7) / 8;

            byte[] b = new byte[nBytes];

            if (nBytes > 0)
            {
                nextRndBytes(rnd, b);
                // strip off any excess bits in the MSB
                b[0] &= rndMask[8 * nBytes - numBits];
            }

            this.magnitude = makeMagnitude(b);
            this.sign = 1;
            this.nBits = -1;
            this.nBitLength = -1;
        }

        private static readonly int BITS_PER_BYTE = 8;
        private static readonly int BYTES_PER_INT = 4;

        /**
         * strictly speaking this is a little dodgey from a compliance
         * point of view as it forces people to be using SecureRandom as
         * well, that being said - this implementation is for a crypto
         * library and you do have the source!
         */
        private void nextRndBytes(Random rnd, byte[] bytes)
        {
            int numRequested = bytes.Length;
            int numGot = 0,
            r = 0;

            if (typeof(SecureRandom).IsInstanceOfType(rnd))
            {
                ((SecureRandom) rnd).NextBytes(bytes);
            }
            else
            {
                for (; ; )
                {
                    for (int i = 0; i < BYTES_PER_INT; i++)
                    {
                        if (numGot == numRequested)
                        {
                            return;
                        }

                        r = (i == 0 ? rnd.Next() : r >> BITS_PER_BYTE);
                        bytes[numGot++] = (byte) r;
                    }
                }
            }
        }

        private static readonly byte[] rndMask = { (byte) 255, 127, 63, 31, 15, 7, 3, 1 };

        public BigIntegerBouncy(int bitLength, int certainty, Random rnd) //throws ArithmeticException
        {
            int nBytes = (bitLength + 7) / 8;

            byte[] b = new byte[nBytes];

            do
            {
                if (nBytes > 0)
                {
                    nextRndBytes(rnd, b);
                    // strip off any excess bits in the MSB
                    b[0] &= rndMask[8 * nBytes - bitLength];
                }

                this.magnitude = makeMagnitude(b);
                this.sign = 1;
                this.nBits = -1;
                this.nBitLength = -1;
                this.mQuote = -1L;

                if (certainty > 0 && bitLength > 2)
                {
                    this.magnitude[this.magnitude.Length - 1] |= 1;
                }
            } while (this.bitLength() != bitLength || !this.isProbablePrime(certainty));
        }

        public BigIntegerBouncy abs()
        {
            return (sign >= 0) ? this : this.negate();
        }

        /**
         * return a = a + b - b preserved.
         */
        private int[] add(int[] a, int[] b)
        {
            int tI = a.Length - 1;
            int vI = b.Length - 1;
            long m = 0;

            while (vI >= 0)
            {
                m += (((long) a[tI]) & IMASK) + (((long) b[vI--]) & IMASK);
                a[tI--] = (int) m;
                m = (long) ((ulong) m >> 32);
            }

            while (tI >= 0 && m != 0)
            {
                m += (((long) a[tI]) & IMASK);
                a[tI--] = (int) m;
                m = (long) ((ulong) m >> 32);
            }

            return a;
        }

        public BigIntegerBouncy add(BigIntegerBouncy val) //throws ArithmeticException
        {
            if (val.sign == 0 || val.magnitude.Length == 0)
                return this;
            if (this.sign == 0 || this.magnitude.Length == 0)
                return val;

            if (val.sign < 0)
            {
                if (this.sign > 0)
                    return this.subtract(val.negate());
            }
            else
            {
                if (this.sign < 0)
                    return val.subtract(this.negate());
            }

            // both BigIntegers are either +ve or -ve; set the sign later

            int[] mag,
            op;

            if (this.magnitude.Length < val.magnitude.Length)
            {
                mag = new int[val.magnitude.Length + 1];

                Array.Copy(val.magnitude, 0, mag, 1, val.magnitude.Length);
                op = this.magnitude;
            }
            else
            {
                mag = new int[this.magnitude.Length + 1];

                Array.Copy(this.magnitude, 0, mag, 1, this.magnitude.Length);
                op = val.magnitude;
            }

            return new BigIntegerBouncy(this.sign, add(mag, op));
        }

        public int bitCount()
        {
            if (nBits == -1)
            {
                nBits = 0;
                for (int i = 0; i < magnitude.Length; i++)
                {
                    nBits += bitCounts[magnitude[i] & 0xff];
                    nBits += bitCounts[(magnitude[i] >> 8) & 0xff];
                    nBits += bitCounts[(magnitude[i] >> 16) & 0xff];
                    nBits += bitCounts[(magnitude[i] >> 24) & 0xff];
                }
            }

            return nBits;
        }

        private readonly static byte[] bitCounts = {0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 1,
            2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4,
            4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3,
            4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5,
            3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 1, 2, 2, 3, 2,
            3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3,
            3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6,
            7, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6,
            5, 6, 6, 7, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 4, 5, 5, 6, 5, 6, 6, 7, 5,
            6, 6, 7, 6, 7, 7, 8};

        private int bitLength(int indx, int[] mag)
        {
            int bitLength;

            if (mag.Length == 0)
            {
                return 0;
            }
            else
            {
                while (indx != mag.Length && mag[indx] == 0)
                {
                    indx++;
                }

                if (indx == mag.Length)
                {
                    return 0;
                }

                // bit length for everything after the first int
                bitLength = 32 * ((mag.Length - indx) - 1);

                // and determine bitlength of first int
                bitLength += bitLen(mag[indx]);

                if (sign < 0)
                {
                    // Check if magnitude is a power of two
                    bool pow2 = ((bitCounts[mag[indx] & 0xff])
                            + (bitCounts[(mag[indx] >> 8) & 0xff])
                            + (bitCounts[(mag[indx] >> 16) & 0xff]) + (bitCounts[(mag[indx] >> 24) & 0xff])) == 1;

                    for (int i = indx + 1; i < mag.Length && pow2; i++)
                    {
                        pow2 = (mag[i] == 0);
                    }

                    bitLength -= (pow2 ? 1 : 0);
                }
            }

            return bitLength;
        }

        public int bitLength()
        {
            if (nBitLength == -1)
            {
                if (sign == 0)
                {
                    nBitLength = 0;
                }
                else
                {
                    nBitLength = bitLength(0, magnitude);
                }
            }

            return nBitLength;
        }

        //
        // bitLen(val) is the number of bits in val.
        //
        static int bitLen(int w)
        {
            // Binary search - decision tree (5 tests, rarely 6)
            return (w < 1 << 15 ? (w < 1 << 7
                    ? (w < 1 << 3 ? (w < 1 << 1
                            ? (w < 1 << 0 ? (w < 0 ? 32 : 0) : 1)
                            : (w < 1 << 2 ? 2 : 3)) : (w < 1 << 5
                            ? (w < 1 << 4 ? 4 : 5)
                            : (w < 1 << 6 ? 6 : 7)))
                    : (w < 1 << 11
                            ? (w < 1 << 9 ? (w < 1 << 8 ? 8 : 9) : (w < 1 << 10 ? 10 : 11))
                            : (w < 1 << 13 ? (w < 1 << 12 ? 12 : 13) : (w < 1 << 14 ? 14 : 15)))) : (w < 1 << 23 ? (w < 1 << 19
                    ? (w < 1 << 17 ? (w < 1 << 16 ? 16 : 17) : (w < 1 << 18 ? 18 : 19))
                    : (w < 1 << 21 ? (w < 1 << 20 ? 20 : 21) : (w < 1 << 22 ? 22 : 23))) : (w < 1 << 27
                    ? (w < 1 << 25 ? (w < 1 << 24 ? 24 : 25) : (w < 1 << 26 ? 26 : 27))
                    : (w < 1 << 29 ? (w < 1 << 28 ? 28 : 29) : (w < 1 << 30 ? 30 : 31)))));
        }

        private readonly static byte[] bitLengths = {0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8};

        public int compareTo(Object o)
        {
            return compareTo((BigIntegerBouncy) o);
        }

        /**
         * unsigned comparison on two arrays - note the arrays may
         * start with leading zeros.
         */
        private int compareTo(int xIndx, int[] x, int yIndx, int[] y)
        {
            while (xIndx != x.Length && x[xIndx] == 0)
            {
                xIndx++;
            }

            while (yIndx != y.Length && y[yIndx] == 0)
            {
                yIndx++;
            }

            if ((x.Length - xIndx) < (y.Length - yIndx))
            {
                return -1;
            }

            if ((x.Length - xIndx) > (y.Length - yIndx))
            {
                return 1;
            }

            // lengths of magnitudes the same, test the magnitude values

            while (xIndx < x.Length)
            {
                long v1 = (long) (x[xIndx++]) & IMASK;
                long v2 = (long) (y[yIndx++]) & IMASK;
                if (v1 < v2)
                {
                    return -1;
                }
                if (v1 > v2)
                {
                    return 1;
                }
            }

            return 0;
        }

        public int compareTo(BigIntegerBouncy val)
        {
            if (sign < val.sign)
                return -1;
            if (sign > val.sign)
                return 1;

            return compareTo(0, magnitude, 0, val.magnitude);
        }

        /**
         * return z = x / y - done in place (z value preserved, x contains the
         * remainder)
         */
        private int[] divide(int[] x, int[] y)
        {
            int xyCmp = compareTo(0, x, 0, y);
            int[] count;

            if (xyCmp > 0)
            {
                int[] c;

                int shift = bitLength(0, x) - bitLength(0, y);

                if (shift > 1)
                {
                    c = shiftLeft(y, shift - 1);
                    count = shiftLeft(ONE.magnitude, shift - 1);
                    if (shift % 32 == 0)
                    {
                        // Special case where the shift is the size of an int.
                        int[] countSpecial = new int[shift / 32 + 1];
                        Array.Copy(count, 0, countSpecial, 1, countSpecial.Length - 1);
                        countSpecial[0] = 0;
                        count = countSpecial;
                    }
                }
                else
                {
                    c = new int[x.Length];
                    count = new int[1];

                    Array.Copy(y, 0, c, c.Length - y.Length, y.Length);
                    count[0] = 1;
                }

                int[] iCount = new int[count.Length];

                subtract(0, x, 0, c);
                Array.Copy(count, 0, iCount, 0, count.Length);

                int xStart = 0;
                int cStart = 0;
                int iCountStart = 0;

                for (; ; )
                {
                    int cmp = compareTo(xStart, x, cStart, c);

                    while (cmp >= 0)
                    {
                        subtract(xStart, x, cStart, c);
                        add(count, iCount);
                        cmp = compareTo(xStart, x, cStart, c);
                    }

                    xyCmp = compareTo(xStart, x, 0, y);

                    if (xyCmp > 0)
                    {
                        if (x[xStart] == 0)
                        {
                            xStart++;
                        }

                        shift = bitLength(cStart, c) - bitLength(xStart, x);

                        if (shift == 0)
                        {
                            c = shiftRightOne(cStart, c);
                            iCount = shiftRightOne(iCountStart, iCount);
                        }
                        else
                        {
                            c = shiftRight(cStart, c, shift);
                            iCount = shiftRight(iCountStart, iCount, shift);
                        }

                        if (c[cStart] == 0)
                        {
                            cStart++;
                        }

                        if (iCount[iCountStart] == 0)
                        {
                            iCountStart++;
                        }
                    }
                    else if (xyCmp == 0)
                    {
                        add(count, ONE.magnitude);
                        for (int i = xStart; i != x.Length; i++)
                        {
                            x[i] = 0;
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (xyCmp == 0)
            {
                count = new int[1];

                count[0] = 1;
            }
            else
            {
                count = new int[1];

                count[0] = 0;
            }

            return count;
        }

        public BigIntegerBouncy divide(BigIntegerBouncy val) //throws ArithmeticException
        {
            if (val.sign == 0)
            {
                throw new ArithmeticException("Divide by zero");
            }

            if (sign == 0)
            {
                return BigIntegerBouncy.ZERO;
            }

            if (val.compareTo(BigIntegerBouncy.ONE) == 0)
            {
                return this;
            }

            int[] mag = new int[this.magnitude.Length];
            Array.Copy(this.magnitude, 0, mag, 0, mag.Length);

            return new BigIntegerBouncy(this.sign * val.sign, divide(mag, val.magnitude));
        }

        public BigIntegerBouncy[] divideAndRemainder(BigIntegerBouncy val) //throws ArithmeticException
        {
            if (val.sign == 0)
            {
                throw new ArithmeticException("Divide by zero");
            }

            BigIntegerBouncy[] biggies = new BigIntegerBouncy[2];

            if (sign == 0)
            {
                biggies[0] = biggies[1] = BigIntegerBouncy.ZERO;

                return biggies;
            }

            if (val.compareTo(BigIntegerBouncy.ONE) == 0)
            {
                biggies[0] = this;
                biggies[1] = BigIntegerBouncy.ZERO;

                return biggies;
            }

            int[] remainder = new int[this.magnitude.Length];
            Array.Copy(this.magnitude, 0, remainder, 0, remainder.Length);

            int[] quotient = divide(remainder, val.magnitude);

            biggies[0] = new BigIntegerBouncy(this.sign * val.sign, quotient);
            biggies[1] = new BigIntegerBouncy(this.sign, remainder);

            return biggies;
        }

        public override bool Equals(Object val)
        {
            if (val == this)
                return true;

            if (!(typeof(BigIntegerBouncy).IsInstanceOfType(val)))
                return false;
            BigIntegerBouncy biggie = (BigIntegerBouncy) val;

            if (biggie.sign != sign || biggie.magnitude.Length != magnitude.Length)
                return false;

            for (int i = 0; i < magnitude.Length; i++)
            {
                if (biggie.magnitude[i] != magnitude[i])
                    return false;
            }

            return true;
        }

        public BigIntegerBouncy gcd(BigIntegerBouncy val)
        {
            if (val.sign == 0)
                return this.abs();
            else if (sign == 0)
                return val.abs();

            BigIntegerBouncy r;
            BigIntegerBouncy u = this;
            BigIntegerBouncy v = val;

            while (v.sign != 0)
            {
                r = u.mod(v);
                u = v;
                v = r;
            }

            return u;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public int intValue()
        {
            if (magnitude.Length == 0)
            {
                return 0;
            }

            if (sign < 0)
            {
                return -magnitude[magnitude.Length - 1];
            }
            else
            {
                return magnitude[magnitude.Length - 1];
            }
        }

        /**
         * return whether or not a BigInteger is probably prime with a
         * probability of 1 - (1/2)**certainty.
         * <p>
         * From Knuth Vol 2, pg 395.
         */
        public bool isProbablePrime(int certainty)
        {
            if (certainty == 0)
            {
                return true;
            }

            BigIntegerBouncy n = this.abs();

            if (n.Equals(TWO))
            {
                return true;
            }

            if (n.Equals(ONE) || !n.testBit(0))
            {
                return false;
            }

            if ((certainty & 0x1) == 1)
            {
                certainty = certainty / 2 + 1;
            }
            else
            {
                certainty /= 2;
            }

            //
            // let n = 1 + 2^kq
            //
            BigIntegerBouncy q = n.subtract(ONE);
            int k = q.getLowestSetBit();

            q = q.shiftRight(k);

            Random rnd = new Random();
            for (int i = 0; i <= certainty; i++)
            {
                BigIntegerBouncy x;

                do
                {
                    x = new BigIntegerBouncy(n.bitLength(), rnd);
                } while (x.compareTo(ONE) <= 0 || x.compareTo(n) >= 0);

                int j = 0;
                BigIntegerBouncy y = x.modPow(q, n);

                while (!((j == 0 && y.Equals(ONE)) || y.Equals(n.subtract(ONE))))
                {
                    if (j > 0 && y.Equals(ONE))
                    {
                        return false;
                    }
                    if (++j == k)
                    {
                        return false;
                    }
                    y = y.modPow(TWO, n);
                }
            }

            return true;
        }

        public long longValue()
        {
            long val = 0;

            if (magnitude.Length == 0)
            {
                return 0;
            }

            if (magnitude.Length > 1)
            {
                val = ((long) magnitude[magnitude.Length - 2] << 32)
                        | (magnitude[magnitude.Length - 1] & IMASK);
            }
            else
            {
                val = (magnitude[magnitude.Length - 1] & IMASK);
            }

            if (sign < 0)
            {
                return -val;
            }
            else
            {
                return val;
            }
        }

        public BigIntegerBouncy max(BigIntegerBouncy val)
        {
            return (compareTo(val) > 0) ? this : val;
        }

        public BigIntegerBouncy min(BigIntegerBouncy val)
        {
            return (compareTo(val) < 0) ? this : val;
        }

        public BigIntegerBouncy mod(BigIntegerBouncy m) //throws ArithmeticException
        {
            if (m.sign <= 0)
            {
                throw new ArithmeticException("BigInteger: modulus is not positive");
            }

            BigIntegerBouncy biggie = this.remainder(m);

            return (biggie.sign >= 0 ? biggie : biggie.add(m));
        }

        public BigIntegerBouncy modInverse(BigIntegerBouncy m) //throws ArithmeticException
        {
            if (m.sign != 1)
            {
                throw new ArithmeticException("Modulus must be positive");
            }

            BigIntegerBouncy x = new BigIntegerBouncy();
            BigIntegerBouncy y = new BigIntegerBouncy();

            BigIntegerBouncy gcd = BigIntegerBouncy.extEuclid(this, m, x, y);

            if (!gcd.Equals(BigIntegerBouncy.ONE))
            {
                throw new ArithmeticException("Numbers not relatively prime.");
            }

            if (x.compareTo(BigIntegerBouncy.ZERO) < 0)
            {
                x = x.add(m);
            }

            return x;
        }

        /**
         * Calculate the numbers u1, u2, and u3 such that:
         *
         * u1 * a + u2 * b = u3
         *
         * where u3 is the greatest common divider of a and b.
         * a and b using the extended Euclid algorithm (refer p. 323
         * of The Art of Computer Programming vol 2, 2nd ed).
         * This also seems to have the side effect of calculating
         * some form of multiplicative inverse.
         *
         * @param a    First number to calculate gcd for
         * @param b    Second number to calculate gcd for
         * @param u1Out      the return object for the u1 value
         * @param u2Out      the return object for the u2 value
         * @return     The greatest common divisor of a and b
         */
        private static BigIntegerBouncy extEuclid(BigIntegerBouncy a, BigIntegerBouncy b, BigIntegerBouncy u1Out,
                BigIntegerBouncy u2Out)
        {
            BigIntegerBouncy res;

            BigIntegerBouncy u1 = BigIntegerBouncy.ONE;
            BigIntegerBouncy u3 = a;
            BigIntegerBouncy v1 = BigIntegerBouncy.ZERO;
            BigIntegerBouncy v3 = b;

            while (v3.compareTo(BigIntegerBouncy.ZERO) > 0)
            {
                BigIntegerBouncy q,
                tn;
                //tv;

                q = u3.divide(v3);

                tn = u1.subtract(v1.multiply(q));
                u1 = v1;
                v1 = tn;

                tn = u3.subtract(v3.multiply(q));
                u3 = v3;
                v3 = tn;
            }

            u1Out.sign = u1.sign;
            u1Out.magnitude = u1.magnitude;

            res = u3.subtract(u1.multiply(a)).divide(b);
            u2Out.sign = res.sign;
            u2Out.magnitude = res.magnitude;

            return u3;
        }

        /**
         * zero out the array x
         */
        private void zero(int[] x)
        {
            for (int i = 0; i != x.Length; i++)
            {
                x[i] = 0;
            }
        }

        public BigIntegerBouncy modPow(
            BigIntegerBouncy exponent,
            BigIntegerBouncy m)
        //throws ArithmeticException
        {
            int[] zVal = null;
            int[] yAccum = null;
            int[] yVal;

            // Montgomery exponentiation is only possible if the modulus is odd,
            // but AFAIK, this is always the case for crypto algo's
            bool useMonty = ((m.magnitude[m.magnitude.Length - 1] & 1) == 1);
            long mQ = 0;
            if (useMonty)
            {
                mQ = m.getMQuote();

                // tmp = this * R mod m
                BigIntegerBouncy tmp = this.shiftLeft(32 * m.magnitude.Length).mod(m);
                zVal = tmp.magnitude;

                useMonty = (zVal.Length == m.magnitude.Length);

                if (useMonty)
                {
                    yAccum = new int[m.magnitude.Length + 1];
                }
            }

            if (!useMonty)
            {
                if (magnitude.Length <= m.magnitude.Length)
                {
                    //zAccum = new int[m.magnitude.Length * 2];
                    zVal = new int[m.magnitude.Length];

                    Array.Copy(magnitude, 0, zVal, zVal.Length - magnitude.Length,
                            magnitude.Length);
                }
                else
                {
                    //
                    // in normal practice we'll never see this...
                    //
                    BigIntegerBouncy tmp = this.remainder(m);

                    //zAccum = new int[m.magnitude.Length * 2];
                    zVal = new int[m.magnitude.Length];

                    Array.Copy(tmp.magnitude, 0, zVal, zVal.Length - tmp.magnitude.Length,
                            tmp.magnitude.Length);
                }

                yAccum = new int[m.magnitude.Length * 2];
            }

            yVal = new int[m.magnitude.Length];

            //
            // from LSW to MSW
            //
            for (int i = 0; i < exponent.magnitude.Length; i++)
            {
                int v = exponent.magnitude[i];
                int bits = 0;

                if (i == 0)
                {
                    while (v > 0)
                    {
                        v <<= 1;
                        bits++;
                    }

                    //
                    // first time in initialise y
                    //
                    Array.Copy(zVal, 0, yVal, 0, zVal.Length);

                    v <<= 1;
                    bits++;
                }

                while (v != 0)
                {
                    if (useMonty)
                    {
                        // Montgomery square algo doesn't exist, and a normal
                        // square followed by a Montgomery reduction proved to
                        // be almost as heavy as a Montgomery mulitply.
                        multiplyMonty(yAccum, yVal, yVal, m.magnitude, mQ);
                    }
                    else
                    {
                        square(yAccum, yVal);
                        remainder(yAccum, m.magnitude);
                        Array.Copy(yAccum, yAccum.Length - yVal.Length, yVal, 0, yVal.Length);
                        zero(yAccum);
                    }
                    bits++;

                    if (v < 0)
                    {
                        if (useMonty)
                        {
                            multiplyMonty(yAccum, yVal, zVal, m.magnitude, mQ);
                        }
                        else
                        {
                            multiply(yAccum, yVal, zVal);
                            remainder(yAccum, m.magnitude);
                            Array.Copy(yAccum, yAccum.Length - yVal.Length, yVal, 0,
                                    yVal.Length);
                            zero(yAccum);
                        }
                    }

                    v <<= 1;
                }

                while (bits < 32)
                {
                    if (useMonty)
                    {
                        multiplyMonty(yAccum, yVal, yVal, m.magnitude, mQ);
                    }
                    else
                    {
                        square(yAccum, yVal);
                        remainder(yAccum, m.magnitude);
                        Array.Copy(yAccum, yAccum.Length - yVal.Length, yVal, 0, yVal.Length);
                        zero(yAccum);
                    }
                    bits++;
                }
            }

            if (useMonty)
            {
                // Return y * R^(-1) mod m by doing y * 1 * R^(-1) mod m
                zero(zVal);
                zVal[zVal.Length - 1] = 1;
                multiplyMonty(yAccum, yVal, zVal, m.magnitude, mQ);
            }

            return new BigIntegerBouncy(1, yVal);
        }

        /**
         * return w with w = x * x - w is assumed to have enough space.
         */
        private int[] square(int[] w, int[] x)
        {
            long u1,
            u2,
            c;

            if (w.Length != 2 * x.Length)
            {
                throw new ArgumentException("no I don't think so...");
            }

            for (int i = x.Length - 1; i != 0; i--)
            {
                long v = (x[i] & IMASK);

                u1 = v * v;
                u2 = (long) ((ulong) u1 >> 32);
                u1 = u1 & IMASK;

                u1 += (w[2 * i + 1] & IMASK);

                w[2 * i + 1] = (int) u1;
                c = u2 + (u1 >> 32);

                for (int j = i - 1; j >= 0; j--)
                {
                    u1 = (x[j] & IMASK) * v;
                    u2 = (long) ((ulong) u1 >> 31); // multiply by 2!
                    u1 = (u1 & 0x7fffffff) << 1; // multiply by 2!
                    u1 += (w[i + j + 1] & IMASK) + c;

                    w[i + j + 1] = (int) u1;
                    c = u2 + (long) ((ulong) u1 >> 32);
                }
                c += w[i] & IMASK;
                w[i] = (int) c;
                w[i - 1] = (int) (c >> 32);
            }

            u1 = (x[0] & IMASK);
            u1 = u1 * u1;
            u2 = (long) ((ulong) u1 >> 32);
            u1 = u1 & IMASK;

            u1 += (w[1] & IMASK);

            w[1] = (int) u1;
            w[0] = (int) (u2 + (u1 >> 32) + w[0]);

            return w;
        }

        /**
         * return x with x = y * z - x is assumed to have enough space.
         */
        private int[] multiply(int[] x, int[] y, int[] z)
        {
            for (int i = z.Length - 1; i >= 0; i--)
            {
                long a = z[i] & IMASK;
                long value = 0;

                for (int j = y.Length - 1; j >= 0; j--)
                {
                    value += a * (y[j] & IMASK) + (x[i + j + 1] & IMASK);

                    x[i + j + 1] = (int) value;

                    value = (long) ((ulong) value >> 32);
                }

                x[i] = (int) value;
            }

            return x;
        }

        /**
         * Calculate mQuote = -m^(-1) mod b with b = 2^32 (32 = word size)
         */
        private long getMQuote()
        {
            if (mQuote != -1L)
            { // allready calculated
                return mQuote;
            }
            if ((magnitude[magnitude.Length - 1] & 1) == 0)
            {
                return -1L; // not for even numbers
            }

            byte[] bytes = { 1, 0, 0, 0, 0 };
            BigIntegerBouncy b = new BigIntegerBouncy(1, bytes); // 2^32
            mQuote = this.negate().mod(b).modInverse(b).longValue();
            return mQuote;
        }

        /**
         * Montgomery multiplication: a = x * y * R^(-1) mod m
         * <br>
         * Based algorithm 14.36 of Handbook of Applied Cryptography.
         * <br>
         * <li> m, x, y should have length n </li>
         * <li> a should have length (n + 1) </li>
         * <li> b = 2^32, R = b^n </li>
         * <br>
         * The result is put in x
         * <br>
         * NOTE: the indices of x, y, m, a different in HAC and in Java
         */
        public void multiplyMonty(int[] a, int[] x, int[] y, int[] m, long mQuote)
        // mQuote = -m^(-1) mod b
        {
            int n = m.Length;
            int nMinus1 = n - 1;
            long y_0 = y[n - 1] & IMASK;

            // 1. a = 0 (Notation: a = (a_{n} a_{n-1} ... a_{0})_{b} )
            for (int i = 0; i <= n; i++)
            {
                a[i] = 0;
            }

            // 2. for i from 0 to (n - 1) do the following:
            for (int i = n; i > 0; i--)
            {

                long x_i = x[i - 1] & IMASK;

                // 2.1 u = ((a[0] + (x[i] * y[0]) * mQuote) mod b
                long u = ((((a[n] & IMASK) + ((x_i * y_0) & IMASK)) & IMASK) * mQuote) & IMASK;

                // 2.2 a = (a + x_i * y + u * m) / b
                long prod1 = x_i * y_0;
                long prod2 = u * (m[n - 1] & IMASK);
                long tmp = (a[n] & IMASK) + (prod1 & IMASK) + (prod2 & IMASK);
                long carry = (long) ((ulong) prod1 >> 32) + (long) ((ulong) prod2 >> 32) + (long) ((ulong) tmp >> 32);
                for (int j = nMinus1; j > 0; j--)
                {
                    prod1 = x_i * (y[j - 1] & IMASK);
                    prod2 = u * (m[j - 1] & IMASK);
                    tmp = (a[j] & IMASK) + (prod1 & IMASK) + (prod2 & IMASK) + (carry & IMASK);
                    carry = (long) ((ulong) carry >> 32) + (long) ((ulong) prod1 >> 32) +
                        (long) ((ulong) prod2 >> 32) + (long) ((ulong) tmp >> 32);
                    a[j + 1] = (int) tmp; // division by b
                }
                carry += (a[0] & IMASK);
                a[1] = (int) carry;
                a[0] = (int) ((ulong) carry >> 32); // OJO!!!!!
            }

            // 3. if x >= m the x = x - m
            if (compareTo(0, a, 0, m) >= 0)
            {
                subtract(0, a, 0, m);
            }

            // put the result in x
            for (int i = 0; i < n; i++)
            {
                x[i] = a[i + 1];
            }
        }

        public BigIntegerBouncy multiply(BigIntegerBouncy val)
        {
            if (sign == 0 || val.sign == 0)
                return BigIntegerBouncy.ZERO;

            int[] res = new int[magnitude.Length + val.magnitude.Length];

            return new BigIntegerBouncy(sign * val.sign, multiply(res, magnitude, val.magnitude));
        }

        public BigIntegerBouncy negate()
        {
            return new BigIntegerBouncy(-sign, magnitude);
        }

        public BigIntegerBouncy pow(int exp) //throws ArithmeticException
        {
            if (exp < 0)
                throw new ArithmeticException("Negative exponent");
            if (sign == 0)
                return (exp == 0 ? BigIntegerBouncy.ONE : this);

            BigIntegerBouncy y,
            z;
            y = BigIntegerBouncy.ONE;
            z = this;

            while (exp != 0)
            {
                if ((exp & 0x1) == 1)
                {
                    y = y.multiply(z);
                }
                exp >>= 1;
                if (exp != 0)
                {
                    z = z.multiply(z);
                }
            }

            return y;
        }

        /**
         * return x = x % y - done in place (y value preserved)
         */
        private int[] remainder(int[] x, int[] y)
        {
            int xyCmp = compareTo(0, x, 0, y);

            if (xyCmp > 0)
            {
                int[] c;
                int shift = bitLength(0, x) - bitLength(0, y);

                if (shift > 1)
                {
                    c = shiftLeft(y, shift - 1);
                }
                else
                {
                    c = new int[x.Length];

                    Array.Copy(y, 0, c, c.Length - y.Length, y.Length);
                }

                subtract(0, x, 0, c);

                int xStart = 0;
                int cStart = 0;

                for (; ; )
                {
                    int cmp = compareTo(xStart, x, cStart, c);

                    while (cmp >= 0)
                    {
                        subtract(xStart, x, cStart, c);
                        cmp = compareTo(xStart, x, cStart, c);
                    }

                    xyCmp = compareTo(xStart, x, 0, y);

                    if (xyCmp > 0)
                    {
                        if (x[xStart] == 0)
                        {
                            xStart++;
                        }

                        shift = bitLength(cStart, c) - bitLength(xStart, x);

                        if (shift == 0)
                        {
                            c = shiftRightOne(cStart, c);
                        }
                        else
                        {
                            c = shiftRight(cStart, c, shift);
                        }

                        if (c[cStart] == 0)
                        {
                            cStart++;
                        }
                    }
                    else if (xyCmp == 0)
                    {
                        for (int i = xStart; i != x.Length; i++)
                        {
                            x[i] = 0;
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (xyCmp == 0)
            {
                for (int i = 0; i != x.Length; i++)
                {
                    x[i] = 0;
                }
            }

            return x;
        }

        public BigIntegerBouncy remainder(BigIntegerBouncy val) //throws ArithmeticException
        {
            if (val.sign == 0)
            {
                throw new ArithmeticException("BigInteger: Divide by zero");
            }

            if (sign == 0)
            {
                return BigIntegerBouncy.ZERO;
            }

            int[] res = new int[this.magnitude.Length];

            Array.Copy(this.magnitude, 0, res, 0, res.Length);

            return new BigIntegerBouncy(sign, remainder(res, val.magnitude));
        }

        /**
         * do a left shift - this returns a new array.
         */
        private int[] shiftLeft(int[] mag, int n)
        {
            int nInts = (int) ((uint) n >> 5);
            int nBits = n & 0x1f;
            int magLen = mag.Length;
            int[] newMag = null;

            if (nBits == 0)
            {
                newMag = new int[magLen + nInts];
                for (int i = 0; i < magLen; i++)
                {
                    newMag[i] = mag[i];
                }
            }
            else
            {
                int i = 0;
                int nBits2 = 32 - nBits;
                int highBits = (int) ((uint) mag[0] >> nBits2);

                if (highBits != 0)
                {
                    newMag = new int[magLen + nInts + 1];
                    newMag[i++] = highBits;
                }
                else
                {
                    newMag = new int[magLen + nInts];
                }

                int m = mag[0];
                for (int j = 0; j < magLen - 1; j++)
                {
                    int next = mag[j + 1];

                    newMag[i++] = (m << nBits) | (int) ((uint) next >> nBits2);
                    m = next;
                }

                newMag[i] = mag[magLen - 1] << nBits;
            }

            return newMag;
        }

        public BigIntegerBouncy shiftLeft(int n)
        {
            if (sign == 0 || magnitude.Length == 0)
            {
                return ZERO;
            }
            if (n == 0)
            {
                return this;
            }

            if (n < 0)
            {
                return shiftRight(-n);
            }

            return new BigIntegerBouncy(sign, shiftLeft(magnitude, n));
        }

        /**
         * do a right shift - this does it in place.
         */
        private int[] shiftRight(int start, int[] mag, int n)
        {
            int nInts = (int) ((uint) n >> 5) + start;
            int nBits = n & 0x1f;
            int magLen = mag.Length;

            if (nInts != start)
            {
                int delta = (nInts - start);

                for (int i = magLen - 1; i >= nInts; i--)
                {
                    mag[i] = mag[i - delta];
                }
                for (int i = nInts - 1; i >= start; i--)
                {
                    mag[i] = 0;
                }
            }

            if (nBits != 0)
            {
                int nBits2 = 32 - nBits;
                int m = mag[magLen - 1];

                for (int i = magLen - 1; i >= nInts + 1; i--)
                {
                    int next = mag[i - 1];

                    mag[i] = (int) ((uint) m >> nBits) | (next << nBits2);
                    m = next;
                }

                mag[nInts] = (int) ((uint) mag[nInts] >> nBits);
            }

            return mag;
        }

        /**
         * do a right shift by one - this does it in place.
         */
        private int[] shiftRightOne(int start, int[] mag)
        {
            int magLen = mag.Length;

            int m = mag[magLen - 1];

            for (int i = magLen - 1; i >= start + 1; i--)
            {
                int next = mag[i - 1];

                mag[i] = ((int) ((uint) m >> 1)) | (next << 31);
                m = next;
            }

            mag[start] = (int) ((uint) mag[start] >> 1);

            return mag;
        }

        public BigIntegerBouncy shiftRight(int n)
        {
            if (n == 0)
            {
                return this;
            }

            if (n < 0)
            {
                return shiftLeft(-n);
            }

            if (n >= bitLength())
            {
                return (this.sign < 0 ? valueOf(-1) : BigIntegerBouncy.ZERO);
            }

            int[] res = new int[this.magnitude.Length];

            Array.Copy(this.magnitude, 0, res, 0, res.Length);

            return new BigIntegerBouncy(this.sign, shiftRight(0, res, n));
        }

        public int signum()
        {
            return sign;
        }

        /**
         * returns x = x - y - we assume x is >= y
         */
        private int[] subtract(int xStart, int[] x, int yStart, int[] y)
        {
            int iT = x.Length - 1;
            int iV = y.Length - 1;
            long m;
            int borrow = 0;

            do
            {
                m = (((long) x[iT]) & IMASK) - (((long) y[iV--]) & IMASK) + borrow;

                x[iT--] = (int) m;

                if (m < 0)
                {
                    borrow = -1;
                }
                else
                {
                    borrow = 0;
                }
            } while (iV >= yStart);

            while (iT >= xStart)
            {
                m = (((long) x[iT]) & IMASK) + borrow;
                x[iT--] = (int) m;

                if (m < 0)
                {
                    borrow = -1;
                }
                else
                {
                    break;
                }
            }

            return x;
        }

        public BigIntegerBouncy subtract(BigIntegerBouncy val)
        {
            if (val.sign == 0 || val.magnitude.Length == 0)
            {
                return this;
            }
            if (sign == 0 || magnitude.Length == 0)
            {
                return val.negate();
            }
            if (val.sign < 0)
            {
                if (this.sign > 0)
                    return this.add(val.negate());
            }
            else
            {
                if (this.sign < 0)
                    return this.add(val.negate());
            }

            BigIntegerBouncy bigun,
            littlun;
            int compare = compareTo(val);
            if (compare == 0)
            {
                return ZERO;
            }

            if (compare < 0)
            {
                bigun = val;
                littlun = this;
            }
            else
            {
                bigun = this;
                littlun = val;
            }

            int[] res = new int[bigun.magnitude.Length];

            Array.Copy(bigun.magnitude, 0, res, 0, res.Length);

            return new BigIntegerBouncy(this.sign * compare, subtract(0, res, 0, littlun.magnitude));
        }

        public byte[] toByteArray()
        {
            int bitLength = this.bitLength();
            byte[] bytes = new byte[bitLength / 8 + 1];

            int bytesCopied = 4;
            int mag = 0;
            int ofs = magnitude.Length - 1;
            int carry = 1;
            long lMag;
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                if (bytesCopied == 4 && ofs >= 0)
                {
                    if (sign < 0)
                    {
                        // we are dealing with a +ve number and we want a -ve one, so
                        // invert the magnitude ints and add 1 (propagating the carry)
                        // to make a 2's complement -ve number
                        lMag = ~magnitude[ofs--] & IMASK;
                        lMag += carry;
                        if ((lMag & ~IMASK) != 0)
                            carry = 1;
                        else
                            carry = 0;
                        mag = (int) (lMag & IMASK);
                    }
                    else
                    {
                        mag = magnitude[ofs--];
                    }
                    bytesCopied = 1;
                }
                else
                {
                    mag = (int) ((uint) mag >> 8);
                    bytesCopied++;
                }

                bytes[i] = (byte) mag;
            }

            return bytes;
        }

        public override String ToString()
        {
            return ToString(10);
        }

        public String ToString(int rdx)
        {
            String format;
            switch (rdx)
            {
                case 10:
                    format = "d";
                    break;
                case 16:
                    format = "x";
                    break;
                default:
                    throw new FormatException("Only base 10 or 16 are allowed");
            }

            if (magnitude == null)
            {
                return "null";
            }
            else if (sign == 0)
            {
                return "0";
            }

            String s = "";
            String h;

            if (rdx == 16)
            {
                for (int i = 0; i < magnitude.Length; i++)
                {
                    h = "0000000" + magnitude[i].ToString("x");
                    h = h.Substring(h.Length - 8);
                    s = s + h;
                }
            }
            else
            {
                // This is algorithm 1a from chapter 4.4 in Seminumerical Algorithms, slow but it works
                Stack S = new Stack();
                BigIntegerBouncy bs = new BigIntegerBouncy(rdx.ToString());
                // The sign is handled separatly.
                // Notice however that for this to work, radix 16 _MUST_ be a special case,
                // unless we want to enter a recursion well. In their infinite wisdom, why did not 
                // the Sun engineers made a c'tor for BigIntegers taking a BigInteger as parameter?
                // (Answer: Becuase Sun's BigIntger is clonable, something bouncycastle's isn't.)
                BigIntegerBouncy u = new BigIntegerBouncy(this.abs().ToString(16), 16);
                BigIntegerBouncy b;

                // For speed, maye these test should look directly a u.magnitude.Length?
                while (!u.Equals(BigIntegerBouncy.ZERO))
                {
                    b = u.mod(bs);
                    if (b.Equals(BigIntegerBouncy.ZERO))
                        S.Push("0");
                    else
                    {
                        // see how to interact with different bases
                        S.Push(b.magnitude[0].ToString(format));
                    }
                    u = u.divide(bs);
                }
                // Then pop the stack
                while (S.Count != 0)
                    s = s + S.Pop();
            }
            // Strip leading zeros.
            while (s.Length > 1 && s[0] == '0')
                s = s.Substring(1);

            if (s.Length == 0)
                s = "0";
            else if (sign == -1)
                s = "-" + s;

            return s;
        }

        public static readonly BigIntegerBouncy ZERO = new BigIntegerBouncy(0, new byte[0]);
        public static readonly BigIntegerBouncy ONE = valueOf(1);
        private static readonly BigIntegerBouncy TWO = valueOf(2);

        public static BigIntegerBouncy valueOf(long val)
        {
            if (val == 0)
            {
                return BigIntegerBouncy.ZERO;
            }

            // store val into a byte array
            byte[] b = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                b[7 - i] = (byte) val;
                val >>= 8;
            }

            return new BigIntegerBouncy(b);
        }

        private int max(int a, int b)
        {
            if (a < b)
                return b;
            return a;
        }

        public int getLowestSetBit()
        {
            if (this.Equals(ZERO))
            {
                return -1;
            }

            int w = magnitude.Length - 1;

            while (w >= 0)
            {
                if (magnitude[w] != 0)
                {
                    break;
                }

                w--;
            }

            int b = 31;

            while (b > 0)
            {
                if ((uint) (magnitude[w] << b) == 0x80000000)
                {
                    break;
                }

                b--;
            }

            return (((magnitude.Length - 1) - w) * 32 + (31 - b));
        }

        public bool testBit(int n) //throws ArithmeticException
        {
            if (n < 0)
            {
                throw new ArithmeticException("Bit position must not be negative");
            }

            if ((n / 32) >= magnitude.Length)
            {
                return sign < 0;
            }

            return ((magnitude[(magnitude.Length - 1) - n / 32] >> (n % 32)) & 1) > 0;
        }


    }
}
