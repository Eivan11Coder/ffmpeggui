using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MaterialSkin;
using MaterialSkin.Controls;
using FPBetaVer_1.Properties;

namespace FPBetaVer_1
{
    public partial class Form1: MaterialForm
    {
        private MaterialTextBox tbInputFile;
        private MaterialTextBox tbOutputFile;
        private MaterialComboBox cbOperation;
        private MaterialTextBox tbBitrate;
        private MaterialTextBox tbResolution;
        private MaterialButton btnSelectInput;
        private MaterialButton btnSelectOutput;
        private MaterialButton btnRun;
        private MaterialTextBox tbLog;
        private MaterialProgressBar progressBar;

        private FFmpegRunner ffmpeg;
        private Settings settings;

        public Form1()
        {
            SetupMaterialSkin();
            SetupControls();

            ffmpeg = new FFmpegRunner();

            LoadSettings();

            this.Load += Form1_Load;

            ffmpeg.OutputDataReceived += OnFFmpegOutput;
            ffmpeg.ProcessCompleted += OnFFmpegCompleted;
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists(ffmpeg.FFmpegPath))
            {
                MessageBox.Show($"ffmpeg не найден по пути:\n{ffmpeg.FFmpegPath}\n\n" +
                        "Поместите ffmpeg.exe в папку с программой или укажите правильный путь в настройках.",
                        "Предупреждение",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
            }
        }

        private void SetupMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue400, Primary.Blue500,
                Primary.Blue600, Accent.LightBlue200,
                TextShade.WHITE);
        }

        private void SetupControls()
        {
            this.Text = "FFmpeg GUI";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(10),
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            var lblInput = new MaterialLabel { Text = "Входной файл:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            tbInputFile = new MaterialTextBox { Hint = "Выберите входной файл...", Dock = DockStyle.Fill };
            btnSelectInput = new MaterialButton { Text = "Обзор...", Dock = DockStyle.Fill, Type = MaterialButton.MaterialButtonType.Outlined };
            btnSelectInput.Click += BtnSelectInput_Click;

            table.Controls.Add(lblInput, 0, 0);
            table.Controls.Add(tbInputFile, 1, 0);
            table.Controls.Add(btnSelectInput, 2, 0);

            var lblOutput = new MaterialLabel { Text = "Выходной файл:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            tbOutputFile = new MaterialTextBox { Hint = "Выберите выходной файл...", Dock = DockStyle.Fill };
            btnSelectOutput = new MaterialButton { Text = "Обзор...", Dock = DockStyle.Fill, Type = MaterialButton.MaterialButtonType.Outlined };
            btnSelectOutput.Click += BtnSelectOutput_Click;

            table.Controls.Add(lblOutput, 0, 1);
            table.Controls.Add(tbOutputFile, 1, 1);
            table.Controls.Add(btnSelectOutput, 2, 1);

            var lblOperation = new MaterialLabel { Text = "Операция:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            cbOperation = new MaterialComboBox { Dock = DockStyle.Fill };
            cbOperation.Items.AddRange(new[] {
                "Конвертировать в MP4 (H.264)",
                "Конвертировать в AVI (без сжатия)",
                "Извлечь аудио MP3",
                "Сжать видео (H.265)"
            });
            cbOperation.SelectedIndex = 0;
            cbOperation.SelectedIndexChanged += CbOperation_SelectedIndexChanged;

            table.Controls.Add(lblOperation, 0, 2);
            table.Controls.Add(cbOperation, 1, 2);
            table.Controls.Add(new MaterialLabel(), 2, 2);

            var lblBitrate = new MaterialLabel { Text = "Битрейт (kbps):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            tbBitrate = new MaterialTextBox { Hint = "Например 1000", Dock = DockStyle.Fill, Text = "1000" };

            table.Controls.Add(lblBitrate, 0, 3);
            table.Controls.Add(tbBitrate, 1, 3);
            table.Controls.Add(new MaterialLabel(), 2, 3);

            var lblResolution = new MaterialLabel { Text = "Разрешение (ШxВ):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            tbResolution = new MaterialTextBox { Hint = "Оставьте пустым для оригинала", Dock = DockStyle.Fill };

            table.Controls.Add(lblResolution, 0, 4);
            table.Controls.Add(tbResolution, 1, 4);
            table.Controls.Add(new MaterialLabel(), 2, 4);

            btnRun = new MaterialButton { Text = "Запустить", Dock = DockStyle.Fill, Type = MaterialButton.MaterialButtonType.Contained };
            btnRun.Click += BtnRun_Click;

            table.Controls.Add(new MaterialLabel(), 0, 5); // пусто
            table.Controls.Add(btnRun, 1, 5);
            table.Controls.Add(new MaterialLabel(), 2, 5);

            var lblLog = new MaterialLabel { Text = "Лог:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            tbLog = new MaterialTextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = (RichTextBoxScrollBars)ScrollBars.Vertical,
                Height = 200,
                Dock = DockStyle.Fill
            };
            progressBar = new MaterialProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                Dock = DockStyle.Fill
            };

            table.Controls.Add(lblLog, 0, 6);
            table.Controls.Add(tbLog, 1, 6);
            table.Controls.Add(progressBar, 2, 6);

            this.Controls.Add(table);
        }

        private void LoadSettings()
        {
            settings = Settings.Load();
            if (!string.IsNullOrEmpty(settings.FFmpegPath))
                ffmpeg.FFmpegPath = settings.FFmpegPath;
            else
                ffmpeg.FFmpegPath = "ffmpeg.exe";
        }

        private void BtnSelectInput_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Все файлы|*.*|Видео|*.mp4;*.avi;*.mkv;*.mov|Аудио|*.mp3;*.wav;*.flac";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    tbInputFile.Text = ofd.FileName;
                    if (string.IsNullOrEmpty(tbOutputFile.Text))
                    {
                        string ext = Path.GetExtension(ofd.FileName);
                        string dir = Path.GetDirectoryName(ofd.FileName);
                        string name = Path.GetFileNameWithoutExtension(ofd.FileName);
                        tbOutputFile.Text = Path.Combine(dir, name + "_output" + ext);
                    }
                }
            }
        }

        private void BtnSelectOutput_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Все файлы|*.*|MP4|*.mp4|AVI|*.avi|MP3|*.mp3";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    tbOutputFile.Text = sfd.FileName;
                }
            }
        }

        private void CbOperation_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filter = "Все файлы|*.*";
            switch (cbOperation.SelectedIndex)
            {
                case 0: filter = "MP4 файлы|*.mp4"; break;
                case 1: filter = "AVI файлы|*.avi"; break;
                case 2: filter = "MP3 файлы|*.mp3"; break;
                case 3: filter = "MP4 файлы (H.265)|*.mp4"; break;
            }
        }

        private async void BtnRun_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbInputFile.Text) || string.IsNullOrEmpty(tbOutputFile.Text))
            {
                MessageBox.Show("Выберите входной и выходной файлы!");
                return;
            }

            if (!File.Exists(tbInputFile.Text))
            {
                MessageBox.Show("Входной файл не существует!");
                return;
            }

            string args = BuildFFmpegArguments();
            if (args == null) return;

            btnRun.Enabled = false;
            progressBar.Visible = true;
            tbLog.Clear();

            try
            {
                await ffmpeg.RunAsync(tbInputFile.Text, tbOutputFile.Text, args);
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
            finally
            {
                btnRun.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private string BuildFFmpegArguments()
        {
            string input = tbInputFile.Text;
            string output = tbOutputFile.Text;
            string bitrate = tbBitrate.Text.Trim();
            string resolution = tbResolution.Text.Trim();

            string args = $"-i \"{input}\" -y ";

            switch (cbOperation.SelectedIndex)
            {
                case 0: // MP4 H.264
                    args += "-c:v libx264 -preset medium -crf 23 ";
                    if (!string.IsNullOrEmpty(bitrate)) args += $"-b:v {bitrate}k ";
                    if (!string.IsNullOrEmpty(resolution)) args += $"-vf scale={resolution} ";
                    args += $"\"{output}\"";
                    break;
                case 1: // AVI без сжатия
                    args += $"-c:v rawvideo -pix_fmt yuv420p \"{output}\"";
                    break;
                case 2: // MP3 аудио
                    args += $"-vn -acodec libmp3lame ";
                    if (!string.IsNullOrEmpty(bitrate)) args += $"-b:a {bitrate}k ";
                    args += $"\"{output}\"";
                    break;
                case 3: // H.265 сжатие
                    args += $"-c:v libx265 -preset medium -crf 28 ";
                    if (!string.IsNullOrEmpty(bitrate)) args += $"-b:v {bitrate}k ";
                    if (!string.IsNullOrEmpty(resolution)) args += $"-vf scale={resolution} ";
                    args += $"\"{output}\"";
                    break;
                default:
                    MessageBox.Show("Выберите операцию!");
                    return null;
            }

            return args;
        }

        private void OnFFmpegOutput(string data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(Log), data);
            }
            else
            {
                Log(data);
            }
        }

        private void Log(string message)
        {
            if (!string.IsNullOrEmpty(message))
                tbLog.AppendText(message + Environment.NewLine);
        }

        private void OnFFmpegCompleted(int exitCode)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int>(OnFFmpegCompleted), exitCode);
                return;
            }

            if (exitCode == 0)
            {
                Log("Готово!");
                MessageBox.Show("Конвертация завершена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Log($"Ошибка, код возврата: {exitCode}");
                MessageBox.Show($"Ошибка при выполнении ffmpeg. Код {exitCode}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}