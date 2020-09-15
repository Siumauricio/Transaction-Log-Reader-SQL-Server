using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Transaction_Log_Reader {
    public partial class ShowTransactions:Form {

        string database_name;
        SqlConnection cnn;
        public ShowTransactions(string _database_name, SqlConnection _cnn) {
            InitializeComponent();
            database_name = _database_name;
            cnn = _cnn;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            string sql = @"SELECT a.[Transaction ID], a.operation as Operacion,a.AllocUnitName as Tabla,b.[Begin Time] as Hora from fn_dblog(null,null) a, (SELECT  * from fn_dblog(null,null) b  where b.operation= 'LOP_BEGIN_XACT') b
                        WHERE b.[Transaction ID] = a.[Transaction ID] and a.AllocUnitName  like '%dbo." + comboBox2.Text + "%' and (a.operation = 'LOP_MODIFY_ROW' or a.operation='LOP_INSERT_ROWS' or a.operation ='LOP_DELETE_ROWS')";
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
                cnn.Close();
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
        }
    }
}


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