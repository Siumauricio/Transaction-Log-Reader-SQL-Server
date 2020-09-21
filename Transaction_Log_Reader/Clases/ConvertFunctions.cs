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
        public static DateTime hexToDatetime(string hexa) {
            hexa = LittleEndian(hexa);
            int yr = Convert.ToInt32(hexa.Substring(0, 8), 16);
            int time = Convert.ToInt32(hexa.Substring(8, 8), 16);
            long year = yr;
            DateTime dt = new DateTime(1900, 1, 1);
            dt = dt.AddDays(year);
            dt = dt.AddSeconds(time / 300);
            return dt;
        }
        public static DateTime hexToSmallDatetime(string hexa) {
            //0x94F0
            //0x0361 Convertir a little endian
            hexa = LittleEndian(hexa);
            int yr = Convert.ToInt32(hexa.Substring(0,4), 16);
            int time = Convert.ToInt32(hexa.Substring(8, 4), 16);
            long year = yr;
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
            List<string> lst = new List<string>();
            for (int i = 0; i < hexa.Length; i += 2) {
                lst.Add(hexa.Substring(i, 2));
            }
            lst.Reverse();
            lst.RemoveAt(lst.Count - 1);
            string endian = String.Join("", lst);
            return Convert.ToInt64(endian, 16);
        }

        public static decimal HexToNumeric(string hexa) {
            List<string> lst = new List<string>();
            for (int i = 0; i < hexa.Length; i += 2) {
                lst.Add(hexa.Substring(i, 2));
            }
            lst.Reverse();
            lst.RemoveAt(lst.Count - 1);
            string endian = String.Join("", lst);
            return Convert.ToInt64(endian, 16);
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
            if (hex == "3B") {
                return "TRUE";
            } else {
                return "FALSE";
            }
        }
        public static string hexToBinary(string hex) {
            return string.Join(String.Empty, hex.Select(c => Convert.ToString(Convert.ToUInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
        }
        public static string LittleEndian(string num) {
            long number = Convert.ToInt64(num, 16);
            byte [] bytes = BitConverter.GetBytes(number);
            string retval = "";
            foreach (byte b in bytes)
                retval += b.ToString("X2");
            return retval;
        }
    }
}
