using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Transaction_Log_Reader {
    public partial class PantallPrincipal:Form {
        public PantallPrincipal() {
            InitializeComponent();

        }

        private void label1_Click(object sender, EventArgs e) {
        }

        private void Form1_Load(object sender, EventArgs e) {
            // GetInstancesAsync();
            comboBox1.Text = @"LAPTOP-44FRKSCR\SQLEXPRESS";
            getDatabasesName(comboBox1.Text);
        }
        public async Task<DataTable> GetInstancesAsync() {
            var serverInstanceTable = new DataTable();
            await Task.Run(() => { serverInstanceTable = SmoApplication.EnumAvailableSqlServers(true); });
            foreach (DataRow instance in serverInstanceTable.Rows) {
                var name = instance ["name"].ToString();
                comboBox1.Items.Add(name);
                getDatabasesName(name);
            }
            return serverInstanceTable;
        }
        public void getDatabasesName(string instance_name) {
            string connectionString = "Data Source="+instance_name+"; Integrated Security=True;";
            using (SqlConnection con = new SqlConnection(connectionString)) {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", con)) {
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            Console.WriteLine(dr [0].ToString());
                            comboBox3.Items.Add(dr [0].ToString());
                        }
                    }
                }
            }
        }
        public void createConnection(string instance, string table_name) {
            string connetionString = "Data Source="+instance+";Initial Catalog="+table_name+ ";Integrated Security=True";
            SqlConnection cnn;
            cnn = new SqlConnection(connetionString);
            try {
                cnn.Open();
                ShowTransactions st = new ShowTransactions(comboBox3.Text, cnn);
                cnn.Close();
                st.Show();
            } catch (Exception ex) {
                MessageBox.Show("Can not open connection ! ");
            }
        }
        private void button1_Click(object sender, EventArgs e) {
            createConnection(comboBox1.Text, comboBox3.Text);
        }
    }
}
