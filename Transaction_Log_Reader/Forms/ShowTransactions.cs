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

    public partial class ShowTransactions:Form {
        string database_name;
        List<Data_Insert> dt_Insert;
        List<Data_Update> dt_Update;
        List<Columna_Informacion> columnas;
        List<Columna_Update> rowLog;
        SqlConnection cnn;
        public ShowTransactions(string _database_name, SqlConnection _cnn) {
            InitializeComponent();
            database_name = _database_name;
            cnn = _cnn;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            string sql = @"SELECT a.[Transaction ID], a.operation as Operacion,a.AllocUnitName as Tabla,a.[Slot ID],b.[Begin Time] as Hora from fn_dblog(null,null) a, (SELECT  * from fn_dblog(null,null) b  where b.operation= 'LOP_BEGIN_XACT') b
                        WHERE b.[Transaction ID] = a.[Transaction ID] and a.AllocUnitName='dbo." + comboBox2.Text + "' and a.RowFlags != 0  and (a.operation = 'LOP_MODIFY_ROW' or a.operation='LOP_INSERT_ROWS' or a.operation ='LOP_DELETE_ROWS')";
            try {
                cnn.Open();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(sql, cnn);
                DataTable dt = new DataTable();
                dataAdapter.Fill(dt);
                dataGridView2.DataSource = dt;
                cnn.Close();
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
        private byte [] getRowLogContents(string id_transaction, string operacion, int slot_id) {
            string sql = @"select [RowLog Contents 0] from fn_dblog(null,null) where allocunitname = 'dbo." + comboBox2.Text + "' and [Transaction ID] ='" + id_transaction + "' and operation='" + operacion + "' and [Slot ID]='" + slot_id + "'";
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
                    return (byte [])data;
                }
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
            return new byte [1];
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

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex >= 0) {
                int rc = dataGridView2.CurrentCell.RowIndex;
                DataGridViewRow row = dataGridView2.Rows [e.RowIndex];
                getColumns();
                string id_transaction = row.Cells [0].Value.ToString();
                string operacion = row.Cells [1].Value.ToString();
                int slot = int.Parse(row.Cells [3].Value.ToString());

                if (operacion == "LOP_MODIFY_ROW") {
                    getRowInsert(comboBox2.Text, slot);
                    getRowsUpdate(comboBox2.Text, slot);
                    byte [] arr = getInsertsContent(id_transaction);
                    getDataColumns(arr);
                } else {
                    byte [] rowLogContent = getRowLogContents(id_transaction, operacion, slot);
                    getDataColumns(rowLogContent);
                }
            }
        }
        private void getDataColumns(byte [] rowLogContent) {

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
                posicion = fixed_length [i].capacity + fixed_length [i].offset;
                Columna_Informacion ms = fixed_length [i];
                ms.value = data_hexa;
                fixed_length [i] = ms;
                posReferencia = posicion;
            }
            posicion++;
            for (int i = posicion; i < rowLogContent.Length; i++) {
                if (rowLogContent [i].ToString() == variable_length.Count.ToString()) {
                    posicion = i + 2;
                    break;
                }
            }
            variable_length.Reverse();
            for (int i = 0; i < variable_length.Count; i++) {//Asignar el offset a los campos de tamano variable ya que por defecto son nums negativos
                int posData = rowLogContent [posicion];
                Columna_Informacion ms = variable_length [i];
                ms.offset = posData;
                variable_length [i] = ms;
                posicion += 2;
            }

            int varStart = posicion;
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
                ms.value = Convert.ToString(cs.ConvertHexa((DataTypes)columnas [i].type_id, columnas [i].value));
                columnas [i] = ms;
            }
            dataGridView1.DataSource = columnas;
        }
        public string ConvertToHexa(string _decimal) {
            return Int32.Parse(_decimal).ToString("X2");
        }

        public byte [] getInsertsContent(string transaccion_id) {
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
                    return dt_Insert [0].RowLogContent;
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
            dt_Insert = new List<Data_Insert>();
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
                dataReader.Close();
                command.Dispose();
                cnn.Close();
            } catch (Exception ex) {
            }
        }

        public void getRowsUpdate(string nombre_tabla, int slot_Id) {
            int totalLength = 0;
            columnas.ForEach(x => totalLength += x.capacity);
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
                    if (data.offset < totalLength) {
                        dt_Update.Add(data);
                    }
                }
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

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e) {
                richTextBox1.Text = getRedoScript();
                richTextBox2.Text = getUndoScript();

        }

        private string getRedoScript() {
            if (comboBox2.Text != "" && dataGridView1.Rows.Count != 0) {
                string script = "INSERT INTO " + comboBox2.Text + " (";
                for (int i = 0; i < columnas.Count; i++) {
                    script += columnas [i].column_name + ",";
                }
                script = script.Substring(0, script.Length - 1);
                script += ") VALUES ( ";
                for (int i = 0; i < columnas.Count; i++) {
                    script += "'" + columnas [i].value + "',";
                }
                script = script.Substring(0, script.Length - 1);
                script += ");";
                return script;
            }
            return "";
        }

        private string getUndoScript() {
            if (comboBox2.Text != "" && dataGridView1.Rows.Count !=0) {

                string script = "DELETE FROM " + comboBox2.Text + " WHERE ";
                for (int i = 0; i < columnas.Count; i++) {
                    script += columnas [i].column_name + "='" + columnas [i].value + "' and ";
                }
                script = script.Substring(0, script.Length - 4);
                return script;
            }
            return "";

        }
    }

}
