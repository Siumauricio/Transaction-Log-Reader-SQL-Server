using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transaction_Log_Reader.Clases {
    struct Columna_Informacion {
        public string column_name { get; set; }
        public int type_id { get; set; }
        public int offset { get; set; }
        public int capacity { get; set; }
        public string value { get; set; }
    }
    struct Columna_Update {
        public byte [] hex_Old { get; set; }
        public byte [] hex_New { get; set; }
        public short offset { get; set; }
        public short size_modified { get; set; }
        public byte [] rowLogContent { get; set; }
        public bool knowOffset { get; set; }
    }

    struct Data_Update {
        public byte [] hex_Old { get; set; }
        public byte [] hex_New { get; set; }
        public short offset { get; set; }
        public short size_modified { get; set; }
        public string transaction_id { get; set; }
    }
    struct Data_Insert {
        public byte [] RowLogContent { get; set; }
    }
}
