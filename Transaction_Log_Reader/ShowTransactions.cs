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

namespace Transaction_Log_Reader {
    struct Columna_Informacion {
        public string column_name;
        public int type_id;
        public int offset;
        public int capacity;
        public string value;
    }
    public partial class ShowTransactions:Form {
        string database_name;
        List<Columna_Informacion> columnas;
        SqlConnection cnn;
        public ShowTransactions(string _database_name, SqlConnection _cnn) {
            InitializeComponent();
            database_name = _database_name;
            cnn = _cnn;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            columnas = new List<Columna_Informacion>();
            string sql = @"SELECT a.[Transaction ID], a.operation as Operacion,a.AllocUnitName as Tabla,b.[Begin Time] as Hora from fn_dblog(null,null) a, (SELECT  * from fn_dblog(null,null) b  where b.operation= 'LOP_BEGIN_XACT') b
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
            string sql = @"select [RowLog Contents 0] from fn_dblog(null,null) where allocunitname  like '%dbo." + comboBox2.Text + "%' and [Transaction ID] ='" + id_transaction + "' and operation='" + operacion + "'";
            SqlCommand command;
            SqlDataReader dataReader;
            try {
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                dataReader = command.ExecuteReader();
                while (dataReader.Read()) {
                    var data = dataReader [0];
                    //byte [] b3 = new byte [4];
                    //Buffer.BlockCopy((Array)data, 4, b3, 0, 4);
                    //int i = BitConverter.ToInt32(b3, 0);
                    return (byte [])data;
                }
                dataReader.Close();
                command.Dispose();
                cnn.Close();
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
            return new byte [1];
        }

        private void getColumns() {
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
                        offset = int.Parse(dataReader [3].ToString())
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
                getDataColumns(row.Cells [0].Value.ToString(), row.Cells [1].Value.ToString());
            }
        }
        private void getDataColumns(string id_transaction, string operacion) {
            var fixed_length = columnas.Where(x => x.offset >= 0).OrderBy(x => x.offset).ToList();
            var variable_length = columnas.Where(x => x.offset < 0).ToList();
            byte [] rowLogContent = getRowLogContents(id_transaction, operacion);
            int fixed_length_totalbytes = 0;
            fixed_length.ForEach(x => fixed_length_totalbytes += x.capacity);
            int posicion = 0;
            int posReferencia = 0;
            for (int i = 0; i < fixed_length.Count; i++) {
                string data_hexa = "";
                byte [] data = new byte [fixed_length [i].capacity];
                for (int j = fixed_length [i].offset, k = 0; j < fixed_length [i].capacity + fixed_length [i].offset; j++, k++) {
                    data [k] = rowLogContent [j];
                    data_hexa += data [k].ToString();
                }
                posicion = fixed_length [i].capacity + fixed_length [i].offset + 5;
                Columna_Informacion ms = fixed_length [i];
                ms.value = data_hexa;
                fixed_length [i] = ms;
                posReferencia = posicion;
            }
            
            for (int i = 0; i < variable_length.Count; i++) {
                int posData = rowLogContent [posicion];
                Columna_Informacion ms = variable_length [i];
                ms.offset = posData;
                variable_length [i] = ms;
                posicion += 2;
            }
            int varStart = posReferencia + (variable_length.Count * 2);
            for (int i = 0; i < variable_length.Count; i++) {
                string data_hexa = "";
                int counter = 0;
                byte [] data = new byte [variable_length [i].capacity];
                for (int j = varStart; j <  variable_length[i].offset; j++) {
                    data [counter] = rowLogContent [j];
                    data_hexa += data [counter].ToString();
                    counter++;
                }
                Columna_Informacion ms = variable_length [i];
                ms.value = data_hexa;
                variable_length [i] = ms;
                varStart = variable_length [i].offset;
            }
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