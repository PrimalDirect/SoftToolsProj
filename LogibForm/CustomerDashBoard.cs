using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LogibForm
{
    public partial class CustomerDashBoard : Form
    {
        private string _username;
        private string imageFolder;
        public CustomerDashBoard(string username)
        {
            InitializeComponent();
            _username = username;
            lblOverviewUser.Text = _username;
            lblLibraryUser.Text = _username;
            lblCartUser.Text = _username;
            lblGenreUser.Text = _username;

            imageFolder = Path.Combine(Application.StartupPath, "Images");

            // Makes the SATA UI Button transparent to match the gradient panel
            btnOverView.CheckedBackground = Color.Transparent;
            btnOverView.NormalBackground = Color.Transparent;

            btnGenre.CheckedBackground = Color.Transparent;
            btnGenre.NormalBackground = Color.Transparent;

            btnLibrary.CheckedBackground = Color.Transparent;
            btnLibrary.NormalBackground = Color.Transparent;

            btnCart.CheckedBackground = Color.Transparent;
            btnCart.NormalBackground = Color.Transparent;

            btnLogout.CheckedBackground = Color.Transparent;
            btnLogout.NormalBackground = Color.Transparent;
            this.Load += CustomerDashBoard_Load;
        }
        
        private void btnOverView_MouseHover(object sender, EventArgs e)
        {
            btnOverView.Cursor = Cursors.Hand;

        }

        private void btnCart_MouseHover(object sender, EventArgs e)
        {
            btnCart.Cursor = Cursors.Hand;
        }

        private void btnLogout_MouseHover(object sender, EventArgs e)
        {
            btnLogout.Cursor= Cursors.Hand;
        }

        private void btnLibrary_MouseHover(object sender, EventArgs e)
        {
            btnLibrary.Cursor = Cursors.Hand;
        }

        private void btnGenre_MouseHover(object sender, EventArgs e)
        {
            btnGenre.Cursor= Cursors.Hand;
        }
        private void showPanel(Panel panelToShow)
        {
            overviewPanel.Visible = false;
            selectGenrePanel.Visible = false;
            libraryPanel.Visible = false;
            cartPanel.Visible = false;

            panelToShow.Visible = true;
            panelToShow.BringToFront();
        }

        private void btnOverView_Click(object sender, EventArgs e)
        {
            showPanel(overviewPanel);
        }
        private void btnGenre_Click(object sender, EventArgs e)
        {
            showPanel(selectGenrePanel);
        }

        private void btnLibrary_Click(object sender, EventArgs e)
        {
            showPanel(libraryPanel);
        }

        private void btnCart_Click(object sender, EventArgs e)
        {
            showPanel(cartPanel);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            Session.LoggedInAccountID = 0;
            Session.LoggedInUser = "";
            lblOverviewUser.Text = "user_1";
            lblLibraryUser.Text = "user_1";
            lblCartUser.Text = "user_1";
            lblGenreUser.Text = "user_1";
            CustomerLoginForm form = new CustomerLoginForm();
            form.Show();
            this.Close();
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
            catch (OracleException ex)
            {
                MessageBox.Show("Database error: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message);
            }

            return names;
        }

        private List<PictureBox> GetAllPictureBoxes()
        {
            return GetControlsRecursive(pnlGames)
                .OfType<PictureBox>()
                .OrderBy(pb => pb.TabIndex)
                .ToList();
        }
        private IEnumerable<Control> GetControlsRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (var child in GetControlsRecursive(c))
                {
                    yield return child;
                }
            }
        }
        private Image LoadGameImage(string gameName)
        {
            gameName = gameName.Trim();

            if (!Directory.Exists(imageFolder))
            {
                MessageBox.Show("Images folder not found at: " + imageFolder);
                return null;
            }

            // Find image files with common extensions
            string[] files = Directory.GetFiles(imageFolder, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file).Trim();
                if (string.Equals(fileName, gameName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            return Image.FromStream(new MemoryStream(ReadFully(fs)));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image '{file}': {ex.Message}");
                        return null;
                    }
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
        private void CustomerDashBoard_Load(object sender, EventArgs e)
        {
            showPanel(overviewPanel);
            // Step 1: Get game names from DB
            List<string> games = GetGameNames();

            if (games.Count > 0)
            {
                MessageBox.Show("Games from DB: " + string.Join(", ", games));
            }
            else
            {
                MessageBox.Show("No games retrieved from DB.");
            }

            // Step 2: Get PictureBoxes
            var pictureBoxes = GetAllPictureBoxes();

            if (!Directory.Exists(imageFolder))
            {
                MessageBox.Show("Images folder not found at: " + imageFolder);
                return;
            }

            if (pictureBoxes.Count == 0)
            {
                MessageBox.Show("No PictureBoxes found on the form.");
                return;
            }

            // Step 3: Load images into PictureBoxes

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

                    // Set label text and auto-size
                    Label lbl = parentPanel.Controls.OfType<Label>().FirstOrDefault();
                    if (lbl != null)
                    {
                        lbl.Text = gameName;
                        lbl.AutoSize = true; // Auto fit content
                        lbl.MaximumSize = new Size(parentPanel.Width, 0); // Wrap text if needed
                        lbl.TextAlign = ContentAlignment.MiddleCenter;

                        // Position label below PictureBox
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
                    if (lbl != null)
                    {
                        lbl.Text = "";
                    }
                }
            }

        }
    }
}
