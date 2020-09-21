using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Transaction_Log_Reader.Clases {
    class HexaDataManager {
        List<Data_Insert> dt_Insert;
        List<Data_Update> dt_Update;
        List<Columna_Informacion> columnas;
        List<Columna_Update> rowLog;
        SqlConnection cnn;

        public HexaDataManager(SqlConnection _cnn) {
            this.cnn = _cnn;
        }
       
    }
}
