using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing.Text;

namespace ACCS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new ACCS());
        }
    }

    public class ACCS : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private Label pathLabel;
        private Label cfgPathLabel;
        private Button uploadButton;

        public ACCS()
        {
            this.ClientSize = new Size(800, 500);
            this.BackColor = ColorTranslator.FromHtml("#262626");
            this.FormBorderStyle = FormBorderStyle.None;
            this.Region = new Region(RoundedRect(new Rectangle(0, 0, 800, 500), 50));

            this.MouseDown += new MouseEventHandler(Form_MouseDown);
            this.MouseMove += new MouseEventHandler(Form_MouseMove);
            this.MouseUp += new MouseEventHandler(Form_MouseUp);
            
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(baseDirectory, "Icons", "ACCS.ico");
            this.StartPosition = FormStartPosition.CenterScreen;

            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
            else
            {
                MessageBox.Show($"Файл иконки не найден по пути: {iconPath}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            string fontPath = @"Fonts\Shket-Regular_0.024.otf";

            if (File.Exists(fontPath))
            {
                PrivateFontCollection privateFonts = new PrivateFontCollection();
                privateFonts.AddFontFile(fontPath);
                Font customFont = new Font(privateFonts.Families[0], 22);

                Label label = new Label();
                label.Text = "Путь до Steam";
                label.Font = customFont;
                label.ForeColor = Color.White;
                label.BackColor = Color.Transparent;
                label.AutoSize = true;
                label.Location = new Point(50, 50);
                this.Controls.Add(label);
                
                Button roundButton = new Button();
                roundButton.Size = new Size(40, 40);
                roundButton.Location = new Point(label.Right + 5, 50); 
                roundButton.BackColor = Color.Transparent;
                roundButton.FlatStyle = FlatStyle.Flat;
                roundButton.FlatAppearance.BorderSize = 0;
                
                string imagePath = @"Icons\explorer.png";
                if (File.Exists(imagePath))
                {
                    Image originalImage = Image.FromFile(imagePath);
                    roundButton.BackgroundImage = new Bitmap(originalImage, new Size(24, 24));
                    roundButton.BackgroundImageLayout = ImageLayout.Center;
                }
                
                roundButton.MouseEnter += (sender, e) => { roundButton.BackColor = ColorTranslator.FromHtml("#4D4D4D"); };
                roundButton.MouseLeave += (sender, e) => { roundButton.BackColor = Color.Transparent; };
                roundButton.MouseDown += (sender, e) => { roundButton.BackColor = ColorTranslator.FromHtml("#4D4D4D"); };


                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, roundButton.Width, roundButton.Height);
                roundButton.Region = new Region(path);

                roundButton.Click += new EventHandler(OpenFolderDialog);

                this.Controls.Add(roundButton);
                
                pathLabel = new Label();
                pathLabel.Font = new Font(privateFonts.Families[0], 20);
                pathLabel.ForeColor = Color.White;
                pathLabel.BackColor = Color.Transparent;
                pathLabel.AutoSize = true;
                pathLabel.Location = new Point(label.Left, label.Bottom + 5);
                pathLabel.Text = "Путь не выбран";
                this.Controls.Add(pathLabel);
                
                Label cfgLabel = new Label();
                cfgLabel.Text = "Путь до cfg";
                cfgLabel.Font = customFont;
                cfgLabel.ForeColor = Color.White;
                cfgLabel.BackColor = Color.Transparent;
                cfgLabel.AutoSize = true;
                cfgLabel.Location = new Point(50, label.Bottom + 50); 
                this.Controls.Add(cfgLabel);
                
                Button cfgButton = new Button();
                cfgButton.Size = new Size(40, 40);
                cfgButton.Location = new Point(cfgLabel.Right + 5, cfgLabel.Top);
                cfgButton.BackColor = Color.Transparent;
                cfgButton.FlatStyle = FlatStyle.Flat;
                cfgButton.FlatAppearance.BorderSize = 0;
                
                if (File.Exists(imagePath))
                {
                    Image originalImage = Image.FromFile(imagePath);
                    cfgButton.BackgroundImage = new Bitmap(originalImage, new Size(24, 24));
                    cfgButton.BackgroundImageLayout = ImageLayout.Center;
                }
                
                cfgButton.MouseEnter += (sender, e) => { cfgButton.BackColor = ColorTranslator.FromHtml("#4D4D4D"); };
                cfgButton.MouseLeave += (sender, e) => { cfgButton.BackColor = Color.Transparent; };
                cfgButton.MouseDown += (sender, e) => { cfgButton.BackColor = ColorTranslator.FromHtml("#4D4D4D"); };
                
                cfgButton.Click += new EventHandler(OpenFileDialog);
                
                GraphicsPath pathh = new GraphicsPath();
                pathh.AddEllipse(0, 0, cfgButton.Width, cfgButton.Height);
                cfgButton.Region = new Region(pathh);

                this.Controls.Add(cfgButton);
                
                cfgPathLabel = new Label();
                cfgPathLabel.Font = new Font(privateFonts.Families[0], 20);
                cfgPathLabel.ForeColor = Color.White;
                cfgPathLabel.BackColor = Color.Transparent;
                cfgPathLabel.AutoSize = true;
                cfgPathLabel.Location = new Point(cfgLabel.Left, cfgLabel.Bottom + 5);
                cfgPathLabel.Text = "Файл не выбран";
                this.Controls.Add(cfgPathLabel);
                
                uploadButton = new Button();
                uploadButton.Size = new Size(120, 40);
                uploadButton.Location = new Point(340, 440);
                uploadButton.Text = "Загрузить";
                uploadButton.BackColor = Color.Transparent;
                uploadButton.ForeColor = Color.White;
                uploadButton.Font = customFont;
                uploadButton.Enabled = false;
                uploadButton.FlatStyle = FlatStyle.Flat;
                uploadButton.FlatAppearance.BorderSize = 0;
                
                uploadButton.MouseEnter += (sender, e) => { uploadButton.BackColor = ColorTranslator.FromHtml("#4D4D4D"); };
                uploadButton.MouseLeave += (sender, e) => { uploadButton.BackColor = Color.Transparent; };
                uploadButton.MouseDown += (sender, e) => { uploadButton.BackColor = ColorTranslator.FromHtml("#4D4D4D"); };
                
                uploadButton.Click += new EventHandler(UploadFile);

                this.Controls.Add(uploadButton);
                
                GraphicsPath uploadPath = new GraphicsPath();
                int radius = 10; // радиус закругления
                uploadPath.AddArc(0, 0, radius, radius, 180, 90);
                uploadPath.AddArc(uploadButton.Width - radius, 0, radius, radius, 270, 90);
                uploadPath.AddArc(uploadButton.Width - radius, uploadButton.Height - radius, radius, radius, 0, 90);
                uploadPath.AddArc(0, uploadButton.Height - radius, radius, radius, 90, 90);
                uploadPath.CloseFigure();
                uploadButton.Region = new Region(uploadPath);
                
                LoadPathFromConfig();
                
                Button closeButton = new Button();
                closeButton.Size = new Size(50, 50);
                closeButton.Location = new Point(this.ClientSize.Width - 80, 0);
                closeButton.BackColor = Color.Transparent;
                closeButton.FlatStyle = FlatStyle.Flat;
                closeButton.FlatAppearance.BorderSize = 0;
                closeButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
                closeButton.FlatAppearance.MouseOverBackColor = Color.Transparent;

                string closeImagePath = @"Icons\close.png";
                if (File.Exists(closeImagePath))
                {
                    Image closeImage = Image.FromFile(closeImagePath);
                    closeButton.BackgroundImage = new Bitmap(closeImage, new Size(35, 35));
                    closeButton.BackgroundImageLayout = ImageLayout.Center;
                }
                
                closeButton.Click += (sender, e) => { this.Close(); };

                this.Controls.Add(closeButton);
                
                Button minimizeButton = new Button();
                minimizeButton.Size = new Size(50, 50);
                minimizeButton.Location = new Point(this.ClientSize.Width - 120, 0);
                minimizeButton.BackColor = Color.Transparent;
                minimizeButton.FlatStyle = FlatStyle.Flat;
                minimizeButton.FlatAppearance.BorderSize = 0;
                minimizeButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
                minimizeButton.FlatAppearance.MouseOverBackColor = Color.Transparent;

                string minimizeImagePath = @"Icons\down.png";
                if (File.Exists(minimizeImagePath))
                {
                    Image minimizeImage = Image.FromFile(minimizeImagePath);
                    minimizeButton.BackgroundImage = new Bitmap(minimizeImage, new Size(35, 35));
                    minimizeButton.BackgroundImageLayout = ImageLayout.Center;
                }

                minimizeButton.Click += (sender, e) => { this.WindowState = FormWindowState.Minimized; };

                this.Controls.Add(minimizeButton);
            }
            else
            {
                MessageBox.Show("Файл шрифта не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFolderDialog(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    SavePathToConfig(folderDialog.SelectedPath);
                    pathLabel.Text = folderDialog.SelectedPath;
                }
                else
                {
                    pathLabel.Text = "Путь не выбран";
                }

                CheckUploadButtonState();
            }
        }

        private void OpenFileDialog(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "Configuration Files (*.cfg)|*.cfg|All Files (*.*)|*.*";
                DialogResult result = fileDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fileDialog.FileName))
                {
                    cfgPathLabel.Text = fileDialog.FileName;
                }
                else
                {
                    cfgPathLabel.Text = "Файл не выбран";
                }

                CheckUploadButtonState();
            }
        }

        private void UploadFile(object sender, EventArgs e)
        {
            string steamPath = pathLabel.Text;
            string cfgFilePath = cfgPathLabel.Text;

            if (steamPath != "Путь не выбран" && cfgFilePath != "Файл не выбран")
            {
                string destinationPath = Path.Combine(steamPath, "steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg", "autoexec.cfg");

                try
                {
                    File.Copy(cfgFilePath, destinationPath, true);
                    MessageBox.Show("Файл успешно загружен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CheckUploadButtonState()
        {
            if (pathLabel.Text != "Путь не выбран" && cfgPathLabel.Text != "Файл не выбран")
            {
                uploadButton.Enabled = true;
            }
            else
            {
                uploadButton.Enabled = false;
            }
        }

        private void SavePathToConfig(string path)
        {
            string configPath = "settings.cfg";
            string configContent = $"steam \"{path}\"";

            File.WriteAllText(configPath, configContent);
        }

        private void LoadPathFromConfig()
        {
            string configPath = "settings.cfg";
            if (File.Exists(configPath))
            {
                string configContent = File.ReadAllText(configPath);
                string[] configLines = configContent.Split('\n');
                foreach (string line in configLines)
                {
                    if (line.StartsWith("steam "))
                    {
                        pathLabel.Text = line.Substring(7).Trim('\"');
                    }
                }
            }
            else
            {
                pathLabel.Text = "Путь не выбран";
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }
    }
}