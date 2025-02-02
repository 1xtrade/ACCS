using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing.Text;
using Microsoft.Win32;

namespace ACCS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            ACCS.LoadSteamPathFromRegistry();
            
            string eulaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "eula.accs");

            if (!File.Exists(eulaFilePath))
            {
                using (UserAgreementForm userAgreementForm = new UserAgreementForm())
                {
                    if (userAgreementForm.ShowDialog() == DialogResult.OK)
                    {
                        File.Create(eulaFilePath).Close();
                        Application.Run(new ACCS());
                    }
                }
            }
            else
            {
                Application.Run(new ACCS());
            }
        }
    }

    public class UserAgreementForm : Form
{
    private Button acceptButton;
    private CheckBox agreementCheckBox;

    public UserAgreementForm()
    {
        this.ClientSize = new Size(600, 400);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = ColorTranslator.FromHtml("#262626");
        this.MaximizeBox = false;

        Label titleLabel = new Label();
        titleLabel.Text = "Пользовательское соглашение";
        titleLabel.Font = new Font("Arial", 14, FontStyle.Bold);
        titleLabel.ForeColor = Color.White;
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(20, 20);
        this.Controls.Add(titleLabel);

        TextBox agreementTextBox = new TextBox();
        agreementTextBox.Multiline = true;
        agreementTextBox.ReadOnly = true;
        agreementTextBox.ScrollBars = ScrollBars.Vertical;
        agreementTextBox.Size = new Size(560, 280);
        agreementTextBox.Location = new Point(20, 60);
        agreementTextBox.BackColor = ColorTranslator.FromHtml("#3d3d3d");
        agreementTextBox.ForeColor = Color.White;
        agreementTextBox.Text = @"Пользовательское Соглашение
1. Общие Положения

Настоящее Пользовательское Соглашение (далее - 'Соглашение') регулирует условия использования программы ACCS (далее - 'Программа'), разработанной 1xtrade (далее - 'Разработчик'). Соглашение вступает в силу с 1 февраля 2025 года.

2. Предоставление Доступа

Пользователь соглашается предоставить Программе доступ к реестру и пути до папки Steam на его устройстве для корректной работы Программы.

3. Конфиденциальность и Безопасность

Разработчик гарантирует, что все данные, к которым Программа получает доступ, не передаются третьим лицам и не сохраняются на сторонних серверах. Все данные остаются исключительно на устройстве Пользователя.

4. Бесплатное Использование

Программа ACCS предоставляется Пользователю на бесплатной и некоммерческой основе. Разработчик не взимает плату за использование Программы и не извлекает коммерческой выгоды от её использования.

5. Ограничение Ответственности

Программа предоставляется 'как есть', без каких-либо гарантий. Разработчик не несёт ответственность за любые прямые или косвенные убытки, возникшие в результате использования Программы.

6. Изменения в Соглашении

Разработчик оставляет за собой право вносить изменения в настоящее Соглашение в любое время. В случае изменений, Разработчик обязуется уведомить Пользователя и предоставить новое пользовательское соглашение. Продолжая использовать Программу, Пользователь соглашается с изменёнными условиями Соглашения.

7. Принятие Условий

Используя Программу, Пользователь подтверждает, что ознакомился с условиями настоящего Соглашения и полностью принимает их.";

        this.Controls.Add(agreementTextBox);

        agreementCheckBox = new CheckBox();
        agreementCheckBox.Text = "Ознакомился с пользовательским соглашением";
        agreementCheckBox.ForeColor = Color.White;
        agreementCheckBox.Location = new Point(20, 350);
        agreementCheckBox.AutoSize = true;
        agreementCheckBox.CheckedChanged += new EventHandler(CheckBoxChanged);
        this.Controls.Add(agreementCheckBox);

        acceptButton = new Button();
        acceptButton.Text = "Принять";
        acceptButton.Location = new Point(450, 345);
        acceptButton.Enabled = false;
        acceptButton.Click += new EventHandler(AcceptButton_Click);
        acceptButton.BackColor = ColorTranslator.FromHtml("#3d3d3d");
        acceptButton.ForeColor = Color.White;
        this.Controls.Add(acceptButton);
    }

    private void CheckBoxChanged(object sender, EventArgs e)
    {
        acceptButton.Enabled = agreementCheckBox.Checked;
    }

    private void AcceptButton_Click(object sender, EventArgs e)
    {
        string eulaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "eula.accs");
        File.Create(eulaFilePath).Close();
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
    public class ACCS : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private Label cfgPathLabel;
        private Button uploadButton;
        private PictureBox clColorPictureBox;
        private PictureBox clHudColorPictureBox;
        
        private float crosshairSize = 0;
        private float crosshairAlpha = 255;
        private float crosshairThickness = 0;
        private float crosshairGap = 0;
        
        private Color crosshairColor = Color.White;

        public ACCS()
        {
            this.ClientSize = new Size(800, 500);
            this.BackColor = ColorTranslator.FromHtml("#262626");
            this.FormBorderStyle = FormBorderStyle.None;
            this.Region = new Region(RoundedRect(new Rectangle(0, 0, 800, 500), 50));

            this.MouseDown += new MouseEventHandler(Form_MouseDown);
            this.MouseMove += new MouseEventHandler(Form_MouseMove);
            this.MouseUp += new MouseEventHandler(Form_MouseUp);
            
            this.Paint += new PaintEventHandler(Form_Paint);

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
                
                Label pathLabel = new Label();
                pathLabel.Font = new Font(privateFonts.Families[0], 20);
                pathLabel.ForeColor = Color.White;
                pathLabel.BackColor = Color.Transparent;
                pathLabel.AutoSize = true;
                pathLabel.Location = new Point(50, 50);
                pathLabel.Text = "Путь до Steam: " + GetSteamPathFromConfig();
                this.Controls.Add(pathLabel);

                Label cfgLabel = new Label();
                cfgLabel.Text = "Путь до cfg";
                cfgLabel.Font = customFont;
                cfgLabel.ForeColor = Color.White;
                cfgLabel.BackColor = Color.Transparent;
                cfgLabel.AutoSize = true;
                cfgLabel.Location = new Point(50, pathLabel.Bottom + 20);
                this.Controls.Add(cfgLabel);
                
                Button cfgButton = new Button();
                cfgButton.Size = new Size(40, 40);
                cfgButton.Location = new Point(cfgLabel.Right + 5, cfgLabel.Top);
                cfgButton.BackColor = Color.Transparent;
                cfgButton.FlatStyle = FlatStyle.Flat;
                cfgButton.FlatAppearance.BorderSize = 0;

                string imagePath = @"Icons\explorer.png";
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
                
                clColorPictureBox = new PictureBox();
                clColorPictureBox.Size = new Size(50, 50);
                clColorPictureBox.Location = new Point(Width - 400, 55);
                clColorPictureBox.BackColor = ColorTranslator.FromHtml("#262626");
                clColorPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                this.Controls.Add(clColorPictureBox);

                clHudColorPictureBox = new PictureBox();
                clHudColorPictureBox.Size = new Size(350, 40);
                clHudColorPictureBox.Location = new Point(Width - 400, 250);
                clHudColorPictureBox.BackColor = Color.Transparent;
                clHudColorPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                clHudColorPictureBox.Image = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configure", "cl_hud_color.png"));
                this.Controls.Add(clHudColorPictureBox);
                clHudColorPictureBox.Visible = false;

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
                int radius = 10;
                uploadPath.AddArc(0, 0, radius, radius, 180, 90);
                uploadPath.AddArc(uploadButton.Width - radius, 0, radius, radius, 270, 90);
                uploadPath.AddArc(uploadButton.Width - radius, uploadButton.Height - radius, radius, radius, 0, 90);
                uploadPath.AddArc(0, uploadButton.Height - radius, radius, radius, 90, 90);
                uploadPath.CloseFigure();
                uploadButton.Region = new Region(uploadPath);
                LoadSteamPathFromRegistry();

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

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            DrawCrosshair(e.Graphics);
        }
        
        private void DrawCrosshair(Graphics g)
        {
            float thickness = (crosshairThickness / 6) * 10;
            Pen pen = new Pen(crosshairColor, thickness);
            
            float centerX = (Width / 2) + Width / 4.6f;
            float centerY = (Height / 2) - Height / 6;
            
            float halfSize = (crosshairSize / 10) * 20;
            
            float adjustedGap = (crosshairGap + 5) * (12f / 10f);

            g.DrawLine(pen, centerX - halfSize - adjustedGap, centerY, centerX - adjustedGap, centerY); // Левый
            g.DrawLine(pen, centerX + adjustedGap, centerY, centerX + halfSize + adjustedGap, centerY); // Правый
            g.DrawLine(pen, centerX, centerY - halfSize - adjustedGap, centerX, centerY - adjustedGap); // Верхний
            g.DrawLine(pen, centerX, centerY + adjustedGap, centerX, centerY + halfSize + adjustedGap); // Нижний
        }
        
        private void UpdateCrosshair(string cfgFilePath)
        {
            crosshairSize = GetCrosshairValue(cfgFilePath, "cl_crosshairsize");
            crosshairAlpha = GetCrosshairValue(cfgFilePath, "cl_crosshairalpha");
            crosshairThickness = GetCrosshairValue(cfgFilePath, "cl_crosshairthickness");
            crosshairGap = GetCrosshairValue(cfgFilePath, "cl_crosshairgap");

            int r = (int)GetCrosshairValue(cfgFilePath, "cl_crosshaircolor_r");
            int g = (int)GetCrosshairValue(cfgFilePath, "cl_crosshaircolor_g");
            int b = (int)GetCrosshairValue(cfgFilePath, "cl_crosshaircolor_b");

            crosshairColor = Color.FromArgb((int)crosshairAlpha, r, g, b);

            Invalidate();

            Console.WriteLine($"Crosshair updated: size={crosshairSize}, alpha={crosshairAlpha}, thickness={crosshairThickness}, gap={crosshairGap}, color={crosshairColor}");
        }

        private float GetCrosshairValue(string cfgFilePath, string paramName)
        {
            string[] lines = File.ReadAllLines(cfgFilePath);
            foreach (string line in lines)
            {
                if (line.Contains($"\"{paramName}\""))
                {
                    int startIndex = line.IndexOf('"', line.IndexOf('"') + 1) + 1;
                    int endIndex = line.LastIndexOf('"');
                    string value = line.Substring(startIndex, endIndex - startIndex).Trim();
                    value = value.Trim('"');
                    Console.WriteLine($"Found {paramName} line: {line}, value: {value}");
                    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                    {
                        Console.WriteLine($"Parsed {paramName} value: {result}");
                        return result;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse {paramName} value: {value}");
                    }
                }
            }
            return 0;
        }
        
        private void UpdateClColorImage(string cfgFilePath)
        {
            string clColorValue = GetClColorValue(cfgFilePath);
            Console.WriteLine($"cl_color value: {clColorValue}");
            if (int.TryParse(clColorValue, out int clColor) && clColor >= 0 && clColor <= 4)
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configure", $"cl_{clColor}.png");
                Console.WriteLine($"Image path: {imagePath}");
                if (File.Exists(imagePath))
                {
                    clColorPictureBox.Image = Image.FromFile(imagePath);
                    Console.WriteLine($"Image loaded: {imagePath}");
                }
                else
                {
                    clColorPictureBox.Image = null;
                    Console.WriteLine("Image file does not exist.");
                }
            }
            else
            {
                clColorPictureBox.Image = null;
                Console.WriteLine("Invalid cl_color value.");
            }
        }

        private string GetClColorValue(string cfgFilePath)
        {
            string[] lines = File.ReadAllLines(cfgFilePath);
            foreach (string line in lines)
            {
                if (line.Contains("\"cl_color\""))
                {
                    int startIndex = line.IndexOf('"', line.IndexOf('"') + 1) + 1;
                    int endIndex = line.LastIndexOf('"');
                    string clColorValue = line.Substring(startIndex, endIndex - startIndex).Trim();
                    clColorValue = clColorValue.Trim('"');
                    Console.WriteLine($"Found cl_color line: {line}, value: {clColorValue}");
                    return clColorValue;
                }
            }
            return null;
        }
        
        private void UpdateClHudColor(string cfgFilePath)
{
    string clHudColorValue = GetClHudColorValue(cfgFilePath);
    Console.WriteLine($"cl_hud_color value: {clHudColorValue}");
    if (int.TryParse(clHudColorValue, out int clHudColor) && clHudColor >= 0 && clHudColor <= 12)
    {
        Color color = clHudColor switch
        {
            1 => ColorTranslator.FromHtml("#ebebeb"),
            2 => ColorTranslator.FromHtml("#fffeff"),
            3 => ColorTranslator.FromHtml("#9bcbfd"),
            4 => ColorTranslator.FromHtml("#3b7fff"),
            5 => ColorTranslator.FromHtml("#cc6eff"),
            6 => ColorTranslator.FromHtml("#fe3934"),
            7 => ColorTranslator.FromHtml("#ff7b38"),
            8 => ColorTranslator.FromHtml("#fffa3a"),
            9 => ColorTranslator.FromHtml("#4eff39"),
            10 => ColorTranslator.FromHtml("#79ffe0"),
            11 => ColorTranslator.FromHtml("#ffa2d4"),
            _ => Color.White
        };

        Bitmap bmp = new Bitmap(clHudColorPictureBox.Image);
        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                if (bmp.GetPixel(x, y).A != 0)
                {
                    bmp.SetPixel(x, y, color);
                }
            }
        }

        clHudColorPictureBox.Image = bmp;
        Console.WriteLine($"cl_hud_color image updated to color: {color}");
    }
    else
    {
        Console.WriteLine("Invalid cl_hud_color value.");
    }
}

private string GetClHudColorValue(string cfgFilePath)
{
    string[] lines = File.ReadAllLines(cfgFilePath);
    foreach (string line in lines)
    {
        if (line.Contains("\"cl_hud_color\""))
        {
            int startIndex = line.IndexOf('"', line.IndexOf('"') + 1) + 1;
            int endIndex = line.LastIndexOf('"');
            string clHudColorValue = line.Substring(startIndex, endIndex - startIndex).Trim();
            clHudColorValue = clHudColorValue.Trim('"');
            Console.WriteLine($"Found cl_hud_color line: {line}, value: {clHudColorValue}");
            return clHudColorValue;
        }
    }
    return null;
}
        
        private string GetSteamPathFromConfig()
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
                        return line.Substring(7).Trim('\"');
                    }
                }
            }

            return null;
        }
        
        private void UploadFile(object sender, EventArgs e)
        {
            string steamPath = GetSteamPathFromConfig();
            string cfgFilePath = cfgPathLabel.Text;

            if (!string.IsNullOrWhiteSpace(steamPath) && cfgFilePath != "Файл не выбран")
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
        
        private void OpenFileDialog(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "Configuration Files (*.cfg)|*.cfg|All Files (*.*)|*.*";
                DialogResult result = fileDialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fileDialog.FileName))
                {
                    cfgPathLabel.Text = fileDialog.FileName;
                    Console.WriteLine($"Selected file: {fileDialog.FileName}");
                    UpdateClColorImage(fileDialog.FileName);
                    UpdateClHudColor(fileDialog.FileName);
                    UpdateCrosshair(fileDialog.FileName);
                    clHudColorPictureBox.Visible = true;
                }
                else
                {
                    cfgPathLabel.Text = "Файл не выбран";
                    clColorPictureBox.Image = null;
                    clHudColorPictureBox.Image = null;
                    clHudColorPictureBox.Visible = false;
                    Console.WriteLine("No file selected.");
                }

                CheckUploadButtonState();
            }
        }

        public static void LoadSteamPathFromRegistry()
        {
            string steamRegistryKey = @"SOFTWARE\WOW6432Node\Valve\Steam";
            string steamInstallPath = null;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(steamRegistryKey))
            {
                if (key != null)
                {
                    steamInstallPath = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrWhiteSpace(steamInstallPath))
                    {
                        SavePathToConfig(steamInstallPath);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(steamInstallPath))
            {
                MessageBox.Show("Путь до Steam не найден в реестре!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckUploadButtonState()
        {
            if (cfgPathLabel.Text != "Файл не выбран")
            {
                uploadButton.Enabled = true;
            }
            else
            {
                uploadButton.Enabled = false;
            }
        }

        public static void SavePathToConfig(string path)
        {
            string configPath = "settings.cfg";
            string configContent = $"steam \"{path}\"";

            File.WriteAllText(configPath, configContent);
            
            var accsForm = Application.OpenForms.OfType<ACCS>().FirstOrDefault();
            if (accsForm != null)
            {
                accsForm.UpdateCfgPathLabel(path);
            }
        }
        
        public void UpdateCfgPathLabel(string path)
        {
            cfgPathLabel.Text = path;
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
    }
}
