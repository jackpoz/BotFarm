using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Client
{
    public static class Extensions
    {
        public static string ToHexString(this byte[] array)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = array.Length - 1; i >= 0; --i)
                builder.Append(array[i].ToString("X2"));

            return builder.ToString();
        }

        /// <summary>
        /// places a non-negative value (0) at the MSB, then converts to a BigInteger.
        /// This ensures a non-negative value without changing the binary representation.
        /// </summary>
        public static BigInteger ToBigInteger(this byte[] array)
        {
            byte[] temp;
            if ((array[array.Length - 1] & 0x80) == 0x80)
            {
                temp = new byte[array.Length + 1];
                temp[array.Length] = 0;
            }
            else
                temp = new byte[array.Length];

            Array.Copy(array, temp, array.Length);
            return new BigInteger(temp);
        }

        /// <summary>
        /// Removes the MSB if it is 0, then converts to a byte array.
        /// </summary>
        public static byte[] ToCleanByteArray(this BigInteger b)
        {
            byte[] array = b.ToByteArray();
            if (array[array.Length - 1] != 0)
                return array;

            byte[] temp = new byte[array.Length - 1];
            Array.Copy(array, temp, temp.Length);
            return temp;
        }

        public static BigInteger ModPow(this BigInteger value, BigInteger pow, BigInteger mod)
        {
            return BigInteger.ModPow(value, pow, mod);
        }

        public static string ReadCString(this BinaryReader reader)
        {
            StringBuilder builder = new StringBuilder();

            while (true)
            {
                byte letter = reader.ReadByte();
                if (letter == 0)
                    break;

                builder.Append((char)letter);
            }

            return builder.ToString();
        }

        public static byte[] SubArray(this byte[] array, int start, int count)
        {
            byte[] subArray = new byte[count];
            Array.Copy(array, start, subArray, 0, count);
            return subArray;
        }

        public static byte[] ToCString(this string str)
        {
            byte[] data = new byte[str.Length + 1];
            Array.Copy(Encoding.ASCII.GetBytes(str), data, str.Length);
            data[data.Length - 1] = 0;
            return data;
        }

        public static IEnumerable<T> GetAttributes<T>(this MemberInfo member, bool inherit)
            where T : Attribute
        {
            return (T[])member.GetCustomAttributes(typeof(T), inherit) ?? new T[] { };
        }

        public static bool TryGetAttributes<T>(this MemberInfo member, bool inherit, out IEnumerable<T> attributes)
            where T : Attribute
        {
            var attrs = (T[])member.GetCustomAttributes(typeof(T), inherit) ?? new T[] { };
            attributes = attrs;
            return attrs.Length > 0;
        }
    }
}
