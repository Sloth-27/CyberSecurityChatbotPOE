using System;
using System.Collections.Generic;

namespace CyberSecurityChatbotGUI
{
    public class QuizQuestion
    {
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>(); // empty = True/False
        public string Answer { get; set; } = "";                        // "A"-"D" or "TRUE"/"FALSE"
        public string Explanation { get; set; } = "";
    }

    public class QuizGame
    {
        private readonly List<QuizQuestion> _questions = new List<QuizQuestion>();
        private int _index;
        private int _score;

        public bool IsActive { get; private set; }
        public int Score => _score;
        public int Total => _questions.Count;

        public QuizGame()
        {
            LoadQuestions();
        }

        private void Add(string text, string answer, string explanation, params string[] options)
        {
            _questions.Add(new QuizQuestion
            {
                Text = text,
                Answer = answer,
                Explanation = explanation,
                Options = new List<string>(options)
            });
        }

        private void LoadQuestions()
        {
            Add("What should you do if you receive an email asking for your password?", "C",
                "Reporting phishing emails helps prevent scams.",
                "A) Reply with your password", "B) Delete the email", "C) Report the email as phishing", "D) Ignore it");
            Add("Using the same password on every account is safe if the password is strong.", "FALSE",
                "If one site is breached, every account with that password is exposed.");
            Add("Which of these is the strongest password?", "C",
                "Long passphrases of random words are hard to guess.",
                "A) password123", "B) Sipho1990", "C) Blue-Tractor-Rain-Coffee-42", "D) qwerty");
            Add("A padlock (HTTPS) on a website guarantees it is trustworthy.", "FALSE",
                "HTTPS only means the connection is encrypted. Scam sites can use it too.");
            Add("What is social engineering?", "B",
                "Attackers manipulate people instead of hacking systems.",
                "A) Hacking servers with code", "B) Tricking people into giving up information", "C) A type of antivirus", "D) A social media site");
            Add("You should give your OTP to a bank employee who phones you.", "FALSE",
                "Banks never ask for your OTP. Hang up and call the official number.");
            Add("What does two-factor authentication (2FA) do?", "A",
                "A second check protects you even if your password is stolen.",
                "A) Adds a second verification step", "B) Speeds up your internet", "C) Removes viruses", "D) Hides your IP address");
            Add("Public Wi-Fi is safe for online banking.", "FALSE",
                "Others on the network may be able to intercept your data.");
            Add("Which is a common sign of phishing?", "A",
                "Scammers create urgency so you act without thinking.",
                "A) Urgent language pressuring you to act now", "B) A message from a friend in their usual style", "C) A newsletter you subscribed to", "D) A reminder from your own calendar");
            Add("What should you do before clicking a link in an unexpected email?", "B",
                "Hovering shows the real destination of the link.",
                "A) Click it quickly", "B) Hover over it to check where it leads", "C) Forward it to everyone", "D) Reply asking if it is safe");
            Add("Software updates help protect you from known security flaws.", "TRUE",
                "Updates patch vulnerabilities that attackers know about.");
            Add("A stranger says you won a prize but must pay a fee first. This is:", "B",
                "Real prizes never require you to pay upfront.",
                "A) A normal competition", "B) A likely scam", "C) A bank requirement", "D) Safe if the fee is small");
            Add("A password manager helps you use a unique password for every account.", "TRUE",
                "It stores strong passwords so you don't have to remember them.");
        }

        public string Start()
        {
            _index = 0;
            _score = 0;
            IsActive = true;
            return "Let's play the cybersecurity quiz! Answer with A, B, C or D, or True/False. Type 'quit' to stop.\n\n" + CurrentQuestion();
        }

        private string CurrentQuestion()
        {
            QuizQuestion q = _questions[_index];
            string text = $"Question {_index + 1} of {_questions.Count}: {q.Text}\n";
            if (q.Options.Count > 0)
            {
                foreach (string option in q.Options)
                    text += option + "\n";
            }
            else
            {
                text += "(True or False)\n";
            }
            return text;
        }

        // Checks one answer, gives feedback, then moves on
        public string Answer(string input)
        {
            string given = input.Trim().ToUpper();

            if (given == "QUIT")
            {
                IsActive = false;
                return "Quiz stopped. Type 'quiz' to try again.";
            }

            QuizQuestion q = _questions[_index];
            bool isTrueFalse = q.Options.Count == 0;

            if (isTrueFalse)
            {
                if (given == "T") given = "TRUE";
                if (given == "F") given = "FALSE";
            }

            bool valid = isTrueFalse
                ? (given == "TRUE" || given == "FALSE")
                : (given == "A" || given == "B" || given == "C" || given == "D");

            if (!valid)
                return "Please answer with " + (isTrueFalse ? "True or False" : "A, B, C or D") + " (or type 'quit').\n\n" + CurrentQuestion();

            string feedback;
            if (given == q.Answer)
            {
                _score++;
                feedback = "Correct! " + q.Explanation;
            }
            else
            {
                feedback = $"Not quite. The correct answer is {q.Answer}. {q.Explanation}";
            }

            _index++;
            if (_index >= _questions.Count)
            {
                IsActive = false;
                string closing = _score >= _questions.Count * 0.8
                    ? "Great job! You're a cybersecurity pro!"
                    : "Keep learning to stay safe online!";
                return $"{feedback}\n\nQuiz finished! You scored {_score} out of {_questions.Count}. {closing}";
            }

            return feedback + "\n\n" + CurrentQuestion();
        }
    }
}