using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transaction_Log_Reader.Clases;

namespace Transaction_Log_Reader {
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
        //  public byte [] rowLogContent { get; set; }
        //  public bool knowOffset { get; set; }
    }
    struct Data_Insert {
        public byte [] RowLogContent { get; set; }
    }
    public partial class ShowTransactions:Form {
        string database_name;
        List<Data_Insert> dt_Insert;
        List<Data_Update> dt_Update;
        List<Columna_Informacion> columnas;
        List<Columna_Update > rowLog;
        SqlConnection cnn;
        public ShowTransactions(string _database_name, SqlConnection _cnn) {
            InitializeComponent();
            database_name = _database_name;
            cnn = _cnn;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            string sql = @"SELECT a.[Transaction ID], a.operation as Operacion,a.AllocUnitName as Tabla,a.[Slot ID],b.[Begin Time] as Hora from fn_dblog(null,null) a, (SELECT  * from fn_dblog(null,null) b  where b.operation= 'LOP_BEGIN_XACT') b
                        WHERE b.[Transaction ID] = a.[Transaction ID] and a.AllocUnitName  like '%dbo." + comboBox2.Text + "%' and (a.operation = 'LOP_MODIFY_ROW' or a.operation='LOP_INSERT_ROWS' or a.operation ='LOP_DELETE_ROWS')";
            try {
                cnn.Open();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(sql, cnn);
                DataTable dt = new DataTable();
                dataAdapter.Fill(dt);
                dataGridView2.DataSource = dt;
                cnn.Close();
                // getRowLogContents();
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
        }

        private void ShowTransactions_Load(object sender, EventArgs e) {
            string sql = "select name from sys.tables;";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    comboBox2.Items.Add(dataReader ["name"].ToString());
                }
                dataReader.Close();
                command.Dispose();
                cnn.Close();
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
        }
        private byte [] getRowLogContents(string id_transaction, string operacion) {
            string sql = @"select [RowLog Contents 0] from fn_dblog(null,null) where allocunitname like '%dbo." + comboBox2.Text + "%' and [Transaction ID] ='" + id_transaction + "' and operation='" + operacion + "'";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    var data = dataReader [0];
                    dataReader.Close();
                    command.Dispose();
                    cnn.Close();
                    //byte [] b3 = new byte [4];
                    //Buffer.BlockCopy((Array)data, 4, b3, 0, 4);
                    //int i = BitConverter.ToInt32(b3, 0);
                    return (byte [])data;
                }
                
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
            return new byte [1];
        }

        private void getRowLogContents1(string id_transaction) {
            rowLog = new List<Columna_Update>();
            string sql = @"SELECT a.[RowLog Contents 0] as Antiguo,a.[RowLog Contents 1] as Nuevo,a.[Offset in Row],a.[Modify Size],b.[RowLog Contents 0], a.operation as Operacion,a.AllocUnitName,b.operation as Tabla from fn_dblog(null,null) a, (SELECT  * from fn_dblog(null,null) b  where b.operation= 'LOP_INSERT_ROWS') b
                        WHERE b.[Slot ID] = a.[Slot ID] and a.AllocUnitName = 'dbo."+comboBox2.Text+"' and a.operation = 'LOP_MODIFY_ROW' and a.[Transaction ID] ='"+ id_transaction + "'";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    var data = new Columna_Update {
                        hex_Old = (byte [])dataReader [0],
                        hex_New = (byte [])dataReader [1],
                        offset = (short)dataReader [2],
                        size_modified = (short)dataReader [3],
                        rowLogContent = (byte [])dataReader [4],
                        knowOffset = false,
                    };
                    rowLog.Add(data);
                    dataReader.Close();
                    command.Dispose();
                    cnn.Close();
                    return;
                }

            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! "+ex.Message);
            }
        }


        private void getColumns() {
            columnas = new List<Columna_Informacion>();
            string sql = @"SELECT c.name AS column_name, max_inrow_length,pc.system_type_id, leaf_offset FROM sys.system_internals_partition_columns pc JOIN sys.partitions p ON p.partition_id = pc.partition_id JOIN sys.columns c ON column_id = partition_column_id
                         AND c.object_id = p.object_id
                         WHERE p.object_id=object_id('" + comboBox2.Text + "')  order by leaf_offset asc;";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    var data = new Columna_Informacion {
                        column_name = dataReader [0].ToString(),
                        capacity = int.Parse(dataReader [1].ToString()),
                        type_id = int.Parse(dataReader [2].ToString()),
                        offset = int.Parse(dataReader [3].ToString()),
                    };
                    columnas.Add(data);
                }
                dataReader.Close();
                command.Dispose();
                cnn.Close();
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }

        }

        //0x30 00 0E 00 6F 00000063636320202004000002001A001D00626262646464
        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex >= 0) {
                int rc = dataGridView2.CurrentCell.RowIndex;
                DataGridViewRow row = dataGridView2.Rows [e.RowIndex];
                getColumns();
                string id_transaction = row.Cells [0].Value.ToString();
                string operacion = row.Cells [1].Value.ToString();
                int slot = int.Parse(row.Cells [3].Value.ToString());

                if (operacion == "LOP_MODIFY_ROW") {
                    getRowLogContents1(id_transaction);
                    for (int i = 0; i < columnas.Count; i++) {//Para campos de tamano fijo
                        if (columnas [i].offset == rowLog [0].offset && !rowLog[0].knowOffset) {
                            Columna_Update cu = rowLog [0];
                            cu.knowOffset = true;
                            rowLog [0] = cu;
                            Array.Copy(cu.hex_New, 0, cu.rowLogContent,cu.offset,cu.size_modified );
                            getDataColumns(cu.rowLogContent);
                            break;
                        }
                    }
                    if (!rowLog[0].knowOffset) {//Campos de tamano variable
                        getRowInsert(comboBox2.Text, slot);
                        getRowsUpdate(comboBox2.Text, slot);
                       byte []arr= getInsertsContent(id_transaction);
                        getDataColumns(arr);
                    }
                } else {
                    byte [] rowLogContent = getRowLogContents(id_transaction, operacion);
                    getDataColumns(rowLogContent);
                }
            }
        }
        private void getDataColumns(byte []rowLogContent) {

            var fixed_length = columnas.Where(x => x.offset >= 0).OrderBy(x => x.offset).ToList();
            var variable_length = columnas.Where(x => x.offset < 0).ToList();

            int fixed_length_totalbytes = 0;
            int posicion = 0;
            int posReferencia = 0;
            fixed_length.ForEach(x => fixed_length_totalbytes += x.capacity);

            ConverterDataTypes cs = new ConverterDataTypes();
            for (int i = 0; i < fixed_length.Count; i++) {//Asignamiento metadata a campos de tamano fijo
                string data_hexa = "";
                byte [] data = new byte [fixed_length [i].capacity];
                for (int j = fixed_length [i].offset, k = 0; j < fixed_length [i].capacity + fixed_length [i].offset; j++, k++) {
                    data [k] = rowLogContent [j];
                    data_hexa += ConvertToHexa(data [k].ToString());
                }
                posicion = fixed_length [i].capacity + fixed_length [i].offset + 5;
                Columna_Informacion ms = fixed_length [i];
                ms.value = data_hexa;
                fixed_length [i] = ms;
                posReferencia = posicion;
            }

            for (int i = 0; i < variable_length.Count; i++) {//Asignar el offset a los campos de tamano variable ya que por defecto son nums negativos
                int posData = rowLogContent [posicion];
                Columna_Informacion ms = variable_length [i];
                ms.offset = posData;
                variable_length [i] = ms;
                posicion += 2;
            }

            int varStart = posReferencia + (variable_length.Count * 2);
            for (int i = 0; i < variable_length.Count; i++) {//Asignamiento metadata a campos de tamano variable
                string data_hexa = "";
                int counter = 0;
                byte [] data = new byte [variable_length [i].capacity];
                for (int j = varStart; j < variable_length [i].offset; j++) {
                    data [counter] = rowLogContent [j];
                    data_hexa += ConvertToHexa(data [counter].ToString());
                    counter++;
                }
                Columna_Informacion ms = variable_length [i];
                ms.value = data_hexa;
                variable_length [i] = ms;
                varStart = variable_length [i].offset;
            }
            columnas.Clear();
            columnas.AddRange(fixed_length);
            columnas.AddRange(variable_length);

            for (int i = 0; i < columnas.Count; i++) {
                Columna_Informacion ms = columnas [i];
                ms.value = Convert.ToString( cs.ConvertHexa((DataTypes)columnas[i].type_id, columnas[i].value)); 
                columnas [i] = ms;
            } 
            dataGridView1.DataSource = columnas;
        }
        public string ConvertToHexa(string _decimal) {
            return Int32.Parse(_decimal).ToString("X2");
        }

        public byte[] getInsertsContent(string transaccion_id) {
            for (int i = 0; i < dt_Update.Count; i++) {
                if (getT(dt_Update [i].offset, dt_Insert [0].RowLogContent)) {
                    byte [] valores = dt_Insert [0].RowLogContent;
                    int nuevoTam = dt_Insert [0].RowLogContent.Length - dt_Update [i].hex_Old.Length;
                    Array.Resize<byte>(ref valores, nuevoTam + dt_Update [i].hex_New.Length);
                    Array.Copy(dt_Update [i].hex_New, 0, valores, dt_Update [i].offset, dt_Update [i].hex_New.Length);
                    Data_Insert ms = dt_Insert [0];
                    ms.RowLogContent = valores;
                    dt_Insert [0] = ms;
                    Console.WriteLine("Puede reemplazar todos los bytes restantes!");
                } else {
                    getValue(dt_Insert [0].RowLogContent, dt_Update [i].offset, dt_Update [i].hex_Old.Length, dt_Update [i].hex_New);
                    Console.WriteLine("Necesita hacer desplazamiento");

                }
                if (transaccion_id == dt_Update [i].transaction_id) {
                    return dt_Insert[0].RowLogContent;
                }
            }
            return new byte [0];
        }
        public void getValue(byte [] rowLogContent, int offset, int tamano, byte [] newValue) {
            List<byte> p = new List<byte>();
            List<byte> p2 = new List<byte>();
            for (int i = 0; i < offset; i++) {
                p.Add(rowLogContent [i]);
            }

            for (int i = offset + tamano; i < rowLogContent.Length; i++) {
                p2.Add(rowLogContent [i]);
            }
            p.AddRange(newValue);
            p.AddRange(p2);
            Data_Insert ms = dt_Insert [0];
            ms.RowLogContent = p.ToArray();
            dt_Insert [0] = ms;
        }
        public void getRowInsert(string nombre_tabla, int slot_Id) {
            dt_Insert   = new List<Data_Insert>();
            string sql = @"SELECT [RowLog Contents 0] from fn_dblog(null,null) where operation = 'LOP_INSERT_ROWS' and [Slot ID]='" + slot_Id + "' AND AllocUnitName = 'dbo." + nombre_tabla + "'";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    var data = new Data_Insert {
                        RowLogContent = (byte [])dataReader [0]
                    };
                    dt_Insert.Add(data);
                }
                // ht_principal.Add("Updates", dt_Update);
                dataReader.Close();
                command.Dispose();
                cnn.Close();
            } catch (Exception ex) {
            }
        }

        public void getRowsUpdate(string nombre_tabla, int slot_Id) {
            dt_Update = new List<Data_Update>();
            string sql = @"SELECT a.[RowLog Contents 0], a.[RowLog Contents 1],a.[Offset in Row],a.[Modify Size],a.[Transaction ID],b.[Begin Time] as Hora from fn_dblog(null,null) a, (SELECT  * from fn_dblog(null,null) b  where b.operation= 'LOP_BEGIN_XACT') b
                        WHERE b.[Transaction ID] = a.[Transaction ID] and a.[Slot ID]='" + slot_Id + "' and a.AllocUnitName = 'dbo." + nombre_tabla + "' and (a.operation = 'LOP_MODIFY_ROW'  ) order by b.[Begin Time] asc";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    var data = new Data_Update {
                        hex_Old = (byte [])dataReader [0],
                        hex_New = (byte [])dataReader [1],
                        offset = (short)dataReader [2],
                        size_modified = (short)dataReader [3],
                        transaction_id = dataReader [4].ToString()
                    };
                    dt_Update.Add(data);
                }
                // ht_principal.Add("Insert", dt_Insert);
                dataReader.Close();
                command.Dispose();
                cnn.Close();
            } catch (Exception ex) {
            }

        }

        public bool getT(int offset, byte [] rowLogContent) {
            List<byte> canValues = new List<byte>();
            for (int i = offset; i < rowLogContent.Length; i++) {
                if (rowLogContent [i].ToString() != "0" && rowLogContent [i + 1].ToString() != "0") {
                    Console.WriteLine("Limite alcanzado");
                    break;
                } else if (rowLogContent [i].ToString() == "0") {
                    Console.WriteLine("Saltando Valor");

                } else {
                    Console.WriteLine(rowLogContent [i]);
                    canValues.Add(rowLogContent [i]);
                }
            }
            int canTamanoVar = columnas.Where(x => x.offset < 1).ToList().Count;
            int valor = canTamanoVar - canValues.Count;
            if ((canTamanoVar - valor) == 1) {
                return true;
            }
            return false;
        }
    }

}

//         for (int i = 0; i<variable_length.Count; i++) {
//                int count = 0;
//string data_hexa = "";
//int posData = rowLogContent [posicion];
//byte [] data = new byte [variable_length [i].capacity];
//                if (i == 0) {
//                    while (rowLogContent [posData - 1] != 0) {
//                        data [count] = rowLogContent [posData - 1]; //Es necesario cambiar el orden de la data a Little Endian
//                        data_hexa += data [count].ToString();
//count++;
//                        posData--;
//                    }
//                } else {
//                    posicion = rowLogContent [posicion += 2];
//                    while ((posicion - posData) != 0) {
//                        data [count] = rowLogContent [posicion - 1];
//                        data_hexa += data [count].ToString();
//count++;
//                        posicion--;
//                    }
//                }
//                Columna_Informacion ms = variable_length [i];
//ms.value = data_hexa;
//                variable_length [i] = ms;
//            }

////SELECT a.operation,a.AllocUnitName,a.[Transaction ID],b.[Begin Time]
//from fn_dblog(null,null) a,
// (SELECT* from fn_dblog(null,null) b where b.operation= 'LOP_BEGIN_XACT') b
//WHERE b.[Transaction ID] = a.[Transaction ID] AND a.operation = 'LOP_MODIFY_ROW' AND a.AllocUnitName  like '%dbo%'

//SELECT a.operation, a.AllocUnitName, a.[Transaction ID], b.[Begin Time]
// from fn_dblog(null,null) a,
// (SELECT* from fn_dblog(null,null) b where b.operation= 'LOP_BEGIN_XACT') b
//WHERE b.[Transaction ID] = a.[Transaction ID] AND a.operation = 'LOP_MODIFY_ROW' AND a.AllocUnitName  like '%dbo%'


//select [Begin Time]
//from sys.fn_dblog(NULL, NULL)
//where [Transaction ID] = '0000:00000432'-- put here the [Transaction ID] found using your query
//and Operation = 'LOP_BEGIN_XACT';