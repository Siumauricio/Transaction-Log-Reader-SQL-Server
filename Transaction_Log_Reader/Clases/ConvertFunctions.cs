using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transaction_Log_Reader.Clases {
    class ConvertFunctions {
        public static string hexToChar(string hexa) {
            if (hexa == null || (hexa.Length & 1) == 1) {
                throw new ArgumentException();
            }
            var sb = new StringBuilder();
            for (var i = 0; i < hexa.Length; i += 2) {
                var hexChar = hexa.Substring(i, 2);
                sb.Append((char)Convert.ToByte(hexChar, 16));
            }
            return sb.ToString();
        }
        public static string hexToVarchar(string hexa) {
            if (hexa == null || (hexa.Length & 1) == 1) {
                throw new ArgumentException();
            }
            var sb = new StringBuilder();
            for (var i = 0; i < hexa.Length; i += 2) {
                var hexChar = hexa.Substring(i, 2);
                sb.Append((char)Convert.ToByte(hexChar, 16));
            }
            return sb.ToString();
        }
        public static DateTime hexToDatetime(long year, long time) {
            DateTime dt = new DateTime(1900, 1, 1);
            dt = dt.AddDays(year);
            dt = dt.AddSeconds(time / 300);
            return dt;
        }
        public static DateTime hexToSmallDatetime(long year, long time) {
            //0x94F0
            //0x0361 Convertir a little endian
            DateTime dt = new DateTime(1900, 1, 1);
            dt = dt.AddDays(year);
            dt = dt.AddMinutes(time);
            return dt;
        }
        public static int hexToInt(string hexa) {
            int value = int.Parse(hexa, System.Globalization.NumberStyles.HexNumber);
            byte [] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            int result = BitConverter.ToInt32(bytes, 0);
            return result;
        }

        public static long hexToBigint(string hexa) {
            long value = long.Parse(hexa, System.Globalization.NumberStyles.HexNumber);
            byte [] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            long result = BitConverter.ToInt64(bytes, 0);
            return result;
        }

        public static byte hexTotinyint(string hexa) {
            byte valor = byte.Parse(hexa, System.Globalization.NumberStyles.HexNumber);
            return valor;
        }

        public static decimal HexToDecimal(string hexa) {
            return long.Parse(hexa, NumberStyles.AllowHexSpecifier);
        }

        public static decimal HexToNumeric(string hexa) {
            return long.Parse(hexa, NumberStyles.AllowHexSpecifier);

        }
        public static double hexToMoney(string hex) {
            hex = LittleEndian(hex);
            double d = 0;
            for (int n = hex.Length - 1; n >= 0; n--) {
                d += System.Uri.FromHex(hex [n]) * Math.Pow(16, hex.Length - 1 - n);
            }
            hex = d.ToString();
            string punto = "." + hex.Substring(hex.Length - 4);
            hex = hex.Substring(0, hex.Length - 4) + punto;
            return double.Parse(hex);
        }
        public static double hexToFloat4Bytes(string hex) {
            byte [] raw = new byte [hex.Length / 2];
            for (int i = 0; i < raw.Length; i++) {
                raw [i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            double f = BitConverter.ToSingle(raw, 0);
            return f;
        }
        public static double hexToFloat8Bytes(string hex) {
            hex = LittleEndian(hex);
            var int64Val = Convert.ToInt64(hex, 16);
            var doubleVal = BitConverter.Int64BitsToDouble(int64Val);
            return doubleVal;
        }
        public static double hexToReal(string hex) {
            byte [] raw = new byte [hex.Length / 2];
            for (int i = 0; i < raw.Length; i++) {
                raw [i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            double f = BitConverter.ToSingle(raw, 0);
            return f;
        }
        public static string hexToBit(string hex) {
            if (hex == "00") {
                return "FALSE";
            } else {
                return "TRUE";
            }
        }
        public static string hexToBinary(string hex) {
            return string.Join(String.Empty, hex.Select(c => Convert.ToString(Convert.ToUInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
        }
        public static string LittleEndian(string num) {
            int number = Convert.ToInt32(num, 16);
            byte [] bytes = BitConverter.GetBytes(number);
            string retval = "";
            foreach (byte b in bytes)
                retval += b.ToString("X2");
            return retval;
        }
    }
}
//static void Main(string [] args) {
//    Console.WriteLine(hexToChar("43"));
//    Console.WriteLine(hexToVarchar("4d6175726963696f"));
//    Console.WriteLine(hexToInt("01000000"));
//    Console.WriteLine(hexToBigint("01000000"));
//    Console.WriteLine(hexTotinyint("FF"));
//    Console.WriteLine(hexToMoney("08AA980000000000"));
//    Console.WriteLine(hexToFloat4Bytes("00207A44"));
//    Console.WriteLine(hexToFloat8Bytes("0000000000448F40"));
//    Console.WriteLine(hexToReal("00207A44"));
//    Console.WriteLine(hexToBinary("00207A4400207A4FFFFF"));
//    Console.WriteLine(hexToBit("00"));
//    Console.WriteLine(hexToDatetime(0x000094F0, 0x00EDA008));
//    Console.WriteLine(hexToSmallDatetime(0x94F0, 0x0361));
//    Console.WriteLine(HexToDecimal("00001275"));
//    Console.WriteLine(HexToNumeric("00001275"));
//}