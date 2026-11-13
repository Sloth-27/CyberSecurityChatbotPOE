using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace CyberSecurityChatbotGUI
{
    public partial class Form1 : Form
    {
        private readonly ResponseEngine _engine = new ResponseEngine();
        private readonly RichTextBox _chat = new RichTextBox();
        private readonly TextBox _input = new TextBox();
        private readonly Button _send = new Button();
        private SoundPlayer? _player;
        private bool _awaitingName = true;
        private string _userName = "You";

        public Form1()
        {
            InitializeComponent();
            BuildUi();
            Shown += (s, e) => StartChat();
        }

        // Builds the whole window in code: header, chat area, input bar
        private void BuildUi()
        {
            Text = "Cybersecurity Awareness Bot";
            ClientSize = new Size(800, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 28, 38);

            Label header = new Label
            {
                Dock = DockStyle.Top,
                Height = 120,
                Text = AsciiArt.Logo.Trim('\r', '\n'),
                Font = new Font("Consolas", 9),
                ForeColor = Color.Cyan,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            _chat.Dock = DockStyle.Fill;
            _chat.ReadOnly = true;
            _chat.BorderStyle = BorderStyle.None;
            _chat.BackColor = Color.FromArgb(32, 38, 52);
            _chat.Font = new Font("Segoe UI", 11);

            Panel bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(10)
            };

            _input.Dock = DockStyle.Fill;
            _input.Font = new Font("Segoe UI", 12);
            _input.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    HandleSend();
                }
            };

            _send.Text = "Send";
            _send.Dock = DockStyle.Right;
            _send.Width = 100;
            _send.FlatStyle = FlatStyle.Flat;
            _send.BackColor = Color.Teal;
            _send.ForeColor = Color.White;
            _send.Click += (s, e) => HandleSend();

            bottom.Controls.Add(_input);
            bottom.Controls.Add(_send);

            // Fill control first, then the docked edges
            Controls.Add(_chat);
            Controls.Add(bottom);
            Controls.Add(header);
        }

        // Plays the voice greeting and asks for the user's name
        private void StartChat()
        {
            PlayGreeting();
            AppendMessage("Bot", "Welcome to the Cybersecurity Awareness Bot! What's your name?", Color.MediumSpringGreen);
            _input.Focus();
        }

        private void PlayGreeting()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Greeting.wav");
                _player = new SoundPlayer(path);
                _player.Play();
            }
            catch
            {
                // Greeting is optional, so the app carries on without it
            }
        }

        // Handles one message from the user
        private void HandleSend()
        {
            string text = _input.Text.Trim();
            _input.Clear();
            _input.Focus();

            if (text.Length == 0)
            {
                AppendMessage("Bot", "I didn't catch that. Please type something.", Color.Orange);
                return;
            }

            AppendMessage(_userName, text, Color.LightSkyBlue);

            if (_awaitingName)
            {
                _userName = text;
                _engine.SetUserName(text);
                _awaitingName = false;
                AppendMessage("Bot", $"Nice to meet you, {text}! Ask me about passwords, phishing, scams or privacy. Type 'exit' to quit.", Color.MediumSpringGreen);
                return;
            }

            if (text.ToLower() == "exit")
            {
                AppendMessage("Bot", $"Stay safe online, {_userName}! Goodbye.", Color.MediumSpringGreen);
                _input.Enabled = false;
                _send.Enabled = false;
                return;
            }

            AppendMessage("Bot", _engine.Respond(text), Color.MediumSpringGreen);
        }

        // Adds a coloured line to the chat display
        private void AppendMessage(string who, string text, Color colour)
        {
            _chat.SelectionStart = _chat.TextLength;
            _chat.SelectionColor = colour;
            _chat.AppendText($"{who}: {text}\n\n");
            _chat.ScrollToCaret();
        }
    }
}