using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transaction_Log_Reader.Clases {
    public enum DataTypes { Char = 175, VarChar = 167, DateTime = 61, SmallDateTime = 58, Int = 56, BigInt = 127, TinyInt = 48, Decimal = 106, Money = 60, Float = 62, Real = 59, Numeric = 108, Bit = 104, Binary = 173 };

    class ConverterDataTypes {
        public dynamic ConvertHexa(DataTypes dt, string hexa) {
            switch (dt) {
                case DataTypes.Char://
                    return ConvertFunctions.hexToChar(hexa);
                case DataTypes.VarChar://
                    return ConvertFunctions.hexToVarchar(hexa);
                case DataTypes.DateTime://
                    return ConvertFunctions.hexToDatetime(hexa);
                case DataTypes.SmallDateTime://
                    return ConvertFunctions.hexToSmallDatetime(hexa);
                case DataTypes.Int://
                    return ConvertFunctions.hexToInt(hexa);
                case DataTypes.BigInt://
                    return ConvertFunctions.hexToBigint(hexa);
                case DataTypes.TinyInt://
                    return ConvertFunctions.hexTotinyint(hexa);
                case DataTypes.Decimal://
                    return ConvertFunctions.HexToDecimal(hexa);
                case DataTypes.Money://
                    return ConvertFunctions.hexToMoney(hexa);
                case DataTypes.Float://
                    if (hexa.Length == 16 ) {
                        return ConvertFunctions.hexToFloat8Bytes(hexa);
                    } else {
                        return ConvertFunctions.hexToFloat4Bytes(hexa);
                    }
                case DataTypes.Real://
                    return ConvertFunctions.hexToReal(hexa);
                case DataTypes.Bit:
                    return ConvertFunctions.hexToBit(hexa);
                case DataTypes.Numeric://
                    return ConvertFunctions.HexToNumeric(hexa);
                case DataTypes.Binary:
                    return ConvertFunctions.hexToBinary(hexa);
                default:
                    return null;
            }
        }
    }
}
