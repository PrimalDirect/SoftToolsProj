using FrameworkTest;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogibForm
{
    public partial class StaffDashBoard : Form
    {
        private string _username;
        private string imageFolder;
        private DataTable inventoryTable;
        private OracleDataAdapter inventoryAdapter;
        private OracleConnection inventoryConnection;

        public StaffDashBoard(string username)
        {
            InitializeComponent();
            _username = username;
            lblInventoryUser.Text = _username;
            lblOverviewUser.Text = _username;

            imageFolder = Path.Combine(Application.StartupPath, "Images");

            // Makes the SATA UI Button transparent to match the gradient panel
            btnOverView.CheckedBackground = Color.Transparent;
            btnOverView.NormalBackground = Color.Transparent;

            btnLogout.CheckedBackground = Color.Transparent;
            btnLogout.NormalBackground = Color.Transparent;

            btnInventory.CheckedBackground = Color.Transparent;
            btnInventory.NormalBackground = Color.Transparent;
            this.Load += StaffDashBoard_Load;
        }

        private void StaffDashBoard_Load(object sender, EventArgs e)
        {
            showPanel(overviewPanel);
            loadInventoryGrid();
            LoadInventoryImages();
        }

        private void btnOverView_MouseHover(object sender, EventArgs e)
        {
            btnOverView.Cursor = Cursors.Hand;

        }

        private void btnLogout_MouseHover(object sender, EventArgs e)
        {
            btnLogout.Cursor = Cursors.Hand;
        }

        private void showPanel(Panel panelToShow)
        {
            overviewPanel.Visible = false;
            inventoryPanel.Visible = false;

            panelToShow.Visible = true;
            panelToShow.BringToFront();
        }

        private void btnOverView_Click(object sender, EventArgs e)
        {
            showPanel(overviewPanel);
        }

        private void btnInventory_Click(object sender, EventArgs e)
        {
            showPanel(inventoryPanel);
        }

        private void loadInventoryGrid()
        {
            try
            {
                inventoryTable = new DataTable();

                // Create class-level connection
                inventoryConnection = new OracleConnection(PrimalDirectDB.oradb);

                // Fill adapter
                inventoryAdapter = new OracleDataAdapter("SELECT * FROM VideoGames", inventoryConnection);
                inventoryAdapter.Fill(inventoryTable);

                // Set primary key
                if (inventoryTable.Columns.Contains("GameID"))
                    inventoryTable.PrimaryKey = new DataColumn[] { inventoryTable.Columns["GameID"] };
                else
                    throw new Exception("GameID column missing from the DataTable.");

                // Manual UpdateCommand to avoid CommandBuilder issues
                OracleCommand updateCommand = new OracleCommand(
                    @"UPDATE VideoGames SET
                      GameName = :GameName,
                      GameDescription = :GameDescription,
                      GameGenre = :GameGenre,
                      GameStorage = :GameStorage,
                      GameBuyPrice = :GameBuyPrice,
                      GameInventory = :GameInventory
                      WHERE GameID = :GameID", inventoryConnection);

                updateCommand.Parameters.Add("GameName", OracleDbType.Varchar2, 255, "GameName");
                updateCommand.Parameters.Add("GameDescription", OracleDbType.Varchar2, 1000, "GameDescription");
                updateCommand.Parameters.Add("GameGenre", OracleDbType.Varchar2, 500, "GameGenre");
                updateCommand.Parameters.Add("GameStorage", OracleDbType.Decimal, 9, "GameStorage");
                updateCommand.Parameters.Add("GameBuyPrice", OracleDbType.Decimal, 6, "GameBuyPrice");
                updateCommand.Parameters.Add("GameInventory", OracleDbType.Int32, 0, "GameInventory");
                updateCommand.Parameters.Add("GameID", OracleDbType.Int32, 0, "GameID");

                inventoryAdapter.UpdateCommand = updateCommand;

                // Bind table to DataGridView
                dgvInventory.DataSource = inventoryTable;

                // Apply styling
                dgvInventory.Columns["GameID"].ReadOnly = true;
                dgvInventory.Columns["GameName"].Width = 200;
                dgvInventory.Columns["GameGenre"].Width = 180;
                dgvInventory.Columns["GameDescription"].Width = 400;
                dgvInventory.Columns["GameStorage"].Width = 85;
                dgvInventory.Columns["GameBuyPrice"].Width = 85;
                dgvInventory.Columns["GameInventory"].Width = 70;

                dgvInventory.Columns["GameDescription"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgvInventory.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                dgvInventory.BackgroundColor = Color.White;
                dgvInventory.DefaultCellStyle.BackColor = Color.White;
                dgvInventory.DefaultCellStyle.ForeColor = Color.Black;
                dgvInventory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(33, 145, 245);
                dgvInventory.DefaultCellStyle.SelectionForeColor = Color.White;
                dgvInventory.DefaultCellStyle.Font = new Font("Century Gothic", 12);

                dgvInventory.ColumnHeadersDefaultCellStyle.Font = new Font("Century Gothic", 14, FontStyle.Bold);
                dgvInventory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(33, 145, 245);
                dgvInventory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvInventory.EnableHeadersVisualStyles = false;

                dgvInventory.RowsDefaultCellStyle.BackColor = Color.White;
                dgvInventory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading inventory: " + ex.Message);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            Session.LoggedInStaffUser = "";
            Session.LoggedInStaffID = 0;
            lblOverviewUser.Text = "user_1";
            lblInventory.Text = "user_1";
            StaffLoginForm form = new StaffLoginForm();
            form.Show();
            this.Close();
        }

        private void btnUpdateInventory_Click(object sender, EventArgs e)
        {
            try
            {
                if (inventoryAdapter == null || inventoryTable == null)
                {
                    MessageBox.Show("Inventory not loaded. Cannot update.");
                    return;
                }

                // Commit any edits in the grid
                dgvInventory.EndEdit();
                dgvInventory.CurrentCell = null; // commit current cell

                // Ensure primary key is set
                if (inventoryTable.PrimaryKey.Length == 0)
                    inventoryTable.PrimaryKey = new DataColumn[] { inventoryTable.Columns["GameID"] };

                // Open connection if not already open
                if (inventoryConnection.State != ConnectionState.Open)
                    inventoryConnection.Open();

                int updatedRows = inventoryAdapter.Update(inventoryTable);
                MessageBox.Show($"{updatedRows} row(s) updated successfully!");

                loadInventoryGrid(); // refresh grid from DB
            }
            catch (OracleException ex)
            {
                MessageBox.Show("Oracle error while updating inventory: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Operation not valid: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
            }
        }
        private List<string> GetGameNames()
        {
            var names = new List<string>();
            try
            {
                using (var conn = new OracleConnection(PrimalDirectDB.oradb))
                {
                    conn.Open();
                    string sql = "SELECT TRIM(GameName) FROM VideoGames ORDER BY GameID";

                    using (var cmd = new OracleCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching game names: " + ex.Message);
            }
            return names;
        }

        private IEnumerable<Control> GetControlsRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (var child in GetControlsRecursive(c))
                    yield return child;
            }
        }

        private void LoadInventoryGrid()
        {
            try
            {
                inventoryTable = new DataTable();
                inventoryConnection = new OracleConnection(PrimalDirectDB.oradb);
                inventoryAdapter = new OracleDataAdapter("SELECT * FROM VideoGames", inventoryConnection);
                inventoryAdapter.Fill(inventoryTable);

                dgvInventory.DataSource = inventoryTable;

                // Styling omitted for brevity, same as your existing code
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading inventory: " + ex.Message);
            }
        }

        private void LoadInventoryImages()
        {
            var games = GetGameNames();
            if (!Directory.Exists(imageFolder))
            {
                MessageBox.Show("Images folder not found: " + imageFolder);
                return;
            }

            var pictureBoxes = GetControlsRecursive(pnlGames)
                .OfType<PictureBox>()
                .OrderBy(pb => pb.TabIndex)
                .ToList();

            for (int i = 0; i < pictureBoxes.Count; i++)
            {
                var pb = pictureBoxes[i];
                var parentPanel = pb.Parent;

                if (i < games.Count)
                {
                    string gameName = games[i].Trim();
                    Image img = LoadGameImage(gameName);

                    if (img != null)
                    {
                        pb.Image = img;
                        pb.SizeMode = PictureBoxSizeMode.Zoom;
                        pb.BringToFront();
                    }
                    else
                    {
                        pb.BackColor = Color.LightGray;
                        pb.Image = null;
                        pb.Tag = gameName + " not found";
                    }

                    // Set label text below the PictureBox
                    Label lbl = parentPanel.Controls.OfType<Label>().FirstOrDefault();
                    if (lbl != null)
                    {
                        lbl.Text = gameName;
                        lbl.AutoSize = true;
                        lbl.MaximumSize = new Size(parentPanel.Width, 0);
                        lbl.TextAlign = ContentAlignment.MiddleCenter;
                        lbl.Location = new Point(
                            (parentPanel.Width - lbl.Width) / 2,
                            pb.Bottom + 5
                        );
                    }
                }
                else
                {
                    pb.BackColor = Color.LightGray;
                    pb.Image = null;
                    Label lbl = parentPanel.Controls.OfType<Label>().FirstOrDefault();
                    if (lbl != null) lbl.Text = "";
                }
            }
        }

        private Image LoadGameImage(string gameName)
        {
            gameName = gameName.Trim();

            string[] files = Directory.GetFiles(imageFolder, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var file in files)
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(file).Trim(), gameName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            return Image.FromStream(new MemoryStream(ReadFully(fs)));
                        }
                    }
                    catch { return null; }
                }
            }
            return null;
        }

        private byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
