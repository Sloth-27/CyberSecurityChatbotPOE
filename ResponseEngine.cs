using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberSecurityChatbotGUI
{
    // Delegate: wraps a tip in an emotion appropriate reply
    public delegate string SentimentReply(string tip);

    public class ResponseEngine
    {
        private readonly Random _random = new Random();
        private string _userName = "friend";
        private string? _favouriteTopic;   // memory
        private string? _currentTopic;     // conversation flow
        public ActivityLog Log { get; } = new ActivityLog();
        private  readonly  TaskManager _tasks = new TaskManager();
        private readonly QuizGame _quiz = new QuizGame();
        private int? _pendingTaskId;
        private bool _awaitingTaskTitle;

        private const string DefaultReply = "I'm not sure I understand. Can you try rephrasing?";

        private readonly Dictionary<string, List<string>> _tips = new()
        {
            ["password"] = new List<string>
            {   
                "Do not share your password with anyone, even if they claim to be from a trusted organisation.",
                "Use strong, unique passwords for each account. Avoid personal details in them.",
                "Longer is stronger: try a passphrase of four or more random words.",
                "Use a password manager so you never have to reuse passwords.",
                "Make sure to use password you can remember, but is hard for others to guess. Avoid using common words or phrases."
                
            },
            ["phishing"] = new List<string>
            {
                "Back up important data regularly to protect against ransomware or data loss.",
                "Be cautious and careful of emails asking for personal information. Scammers often pose as trusted organisations.",
                "Hover over links before clicking them to check where they really lead.",
                "Check the sender's address carefully. Phishing emails often use near identical domains."
            },
            ["scam"] = new List<string>
            {
                "Be cautious of unrecognized calls with no names and just numbers.",
                "If an offer sounds too good to be true, it usually is.",
                "Never send money or share OTPs with someone who contacts you unexpectedly.",
                "Verify unexpected requests by calling the organisation on its official number."
            },
            ["privacy"] = new List<string>
            {
                "Keep to yourself: avoid oversharing personal information online.",
                "Review the privacy settings on your social media accounts regularly.",
                "Only share personal details on websites that use HTTPS.",
                "Turn off app permissions (location, microphone) that an app doesn't need."
            }
        };

        private readonly Dictionary<string, string> _sentimentWords = new()
        {
            ["worried"] = "worried", ["scared"] = "worried", ["anxious"] = "worried", ["overwhelmed"] = "worried",
            ["curious"] = "curious", ["wondering"] = "curious", ["interested"] = "curious",
            ["frustrated"] = "frustrated", ["annoyed"] = "frustrated", ["confused"] = "frustrated"
        };

        private readonly Dictionary<string, SentimentReply> _sentimentReplies = new()
        {
            ["worried"] = tip => $"You are valid to feel that way. Let me share a tip to help you stay safe: {tip}",
            ["curious"] = tip => $"Great that you're curious! Here's something worth knowing: {tip}",
            ["frustrated"] = tip => $"I know this can be annoying, but you're not alone. Here's a simple tip: {tip}"
        };

        // different ways of saying the same thing map to one topic
        private readonly Dictionary<string, string> _synonyms = new()
        {
            ["pass word"] = "password", ["passcode"] = "password", ["login"] = "password", ["2fa"] = "password",
            ["two-factor"] = "password", ["two factor"] = "password",
            ["fake email"] = "phishing", ["suspicious email"] = "phishing", ["suspicious link"] = "phishing", ["spam"] = "phishing",
            ["fraud"] = "scam", ["con "] = "scam", ["swindle"] = "scam", ["fake prize"] = "scam",
            ["personal data"] = "privacy", ["personal information"] = "privacy", ["data protection"] = "privacy", ["tracking"] = "privacy"
        };

        private readonly string[] _followUps = { "another", "more", "explain", "elaborate", "again", "don't understand" };

        public void SetUserName(string name) => _userName = name;

        public string Respond(string input)
        {
            string text = Normalise(input.Trim().ToLower());
            if (text.Length == 0) return DefaultReply;
            if (_quiz.IsActive)
            {
                string quizReply = _quiz.Answer(text);
                if (!_quiz.IsActive)
                    Log.Add($"Quiz finished - score {_quiz.Score}/{_quiz.Total}");
                return quizReply;

                
            }

            if (text.Contains("quiz") || text.Contains("play a game"))
            {
                Log.Add("Quiz started");
                return _quiz.Start();
            }
            
            string? taskReply = HandleTasks(input.Trim(), text);
            if (taskReply != null) return taskReply;
            if (text.Contains("activity log") || text.Contains("what have you done"))
            return Log.Show(10);

            if (text.Contains("how are you"))
                return "I'm doing great, thanks for asking! Ready to help you stay safe online.";
            if (text.Contains("purpose") || text.Contains("what can i ask"))
                return "I'm here to teach you about cybersecurity. Ask me about passwords, phishing, scams or privacy.";

            string? topic = _tips.Keys.FirstOrDefault(k => text.Contains(k));
            string? word = _sentimentWords.Keys.FirstOrDefault(k => text.Contains(k));
            string? sentiment = word == null ? null : _sentimentWords[word];

            // Follow up  "tell me more" keeps the current topic yeah.
            if (topic == null && _currentTopic != null && _followUps.Any(f => text.Contains(f)))
                topic = _currentTopic;

            // Memory: remember the user's interest
            if (topic != null && (text.Contains("interested in") || text.Contains("favourite") || text.Contains("favorite")))
            {
                _favouriteTopic = topic;
                _currentTopic = topic;
                return $"Gotchu! I'll remember that you're interested in {topic}. It's a crucial part of staying safe online after all.";
            }

            // Sentiment with no topic: still give a tip straight away
            if (topic == null && sentiment != null)
                topic = _currentTopic ?? _tips.Keys.ElementAt(_random.Next(_tips.Count));

            if (topic == null) return DefaultReply;

            _currentTopic = topic;
            string tip = _tips[topic][_random.Next(_tips[topic].Count)];
            string reply = sentiment != null ? _sentimentReplies[sentiment](tip) : tip;

            if (_favouriteTopic == topic)
                reply += $" As someone interested in {topic}, {_userName}, it's worth coming back to this often.";
                Log.Add($"Recognised keyword '{topic}' and gave a tip");

            return reply;
        }

                // Turns "in 3 days", "tomorrow" or "next week" into a date
        private DateTime? ParseReminder(string text, out string matched)
        {
            matched = "";
            Match m = Regex.Match(text, @"\bin (\d+) (day|week)s?");
            if (m.Success)
            {
                int n = int.Parse(m.Groups[1].Value);
                matched = m.Value;
                return DateTime.Today.AddDays(m.Groups[2].Value == "week" ? n * 7 : n);
            }
            if (text.Contains("tomorrow")) { matched = "tomorrow"; return DateTime.Today.AddDays(1); }
            if (text.Contains("next week")) { matched = "next week"; return DateTime.Today.AddDays(7); }
            return null;
        }

        // Handles add / view / complete / delete for tasks. Returns null if the message isn't about tasks.
        private string? HandleTasks(string original, string text)
        {
            try
            {
                if (_awaitingTaskTitle)
                {
                    _awaitingTaskTitle = false;
                    original = "add task " + original;
                    text = original.ToLower();
                }
                // The bot just asked "Would you like a reminder?"
                if (_pendingTaskId != null)
                {
                    int pendingId = _pendingTaskId.Value;
                    _pendingTaskId = null;
                    if (text == "no" || text == "nope" || text == "no thanks") return "Okay, no reminder set.";

                    DateTime? when = ParseReminder(text, out _);
                    if (when != null)
                    {
                        _tasks.SetReminder(pendingId, when.Value);
                        Log.Add($"Reminder set for task {pendingId} on {when:yyyy-MM-dd}");
                        return $"Got it! I'll remind you on {when:yyyy-MM-dd}.";
                    }
                    if (text.StartsWith("yes"))
                    {
                        _pendingTaskId = pendingId;
                        return "When should I remind you? For example: 'in 3 days' or 'tomorrow'.";
                    }
                }

                Match num = Regex.Match(text, @"\d+");

                if ((text.Contains("delete") || text.Contains("remove")) && text.Contains("task"))
                {
                    if (!num.Success) return "Which task? Say 'delete task 2'. Type 'show tasks' to see the numbers.";
                    int taskId = int.Parse(num.Value);
                    if (!_tasks.Delete(taskId)) return $"I couldn't find task {taskId}.";
                    Log.Add($"Task {taskId} deleted");
                    return $"Task {taskId} deleted.";
                }

                if ((text.Contains("complete") || text.Contains("done") || text.Contains("finished")) && text.Contains("task"))
                {
                    if (!num.Success) return "Which task? Say 'complete task 2'. Type 'show tasks' to see the numbers.";
                    int taskId = int.Parse(num.Value);
                    if (!_tasks.Complete(taskId)) return $"I couldn't find task {taskId}.";
                    Log.Add($"Task {taskId} marked as completed");
                    return $"Nice work! Task {taskId} is marked as completed.";
                }

                if (text.Contains("add task") || text.Contains("add a task") || text.Contains("new task")
                    || text.Contains("create a task") || text.Contains("remind me"))
                {
                    DateTime? when = ParseReminder(text, out string matched);
                    string title = original;

                    foreach (string phrase in new[] { "add a task to", "add a task", "add tasks", "add task", "new tasks",
                                                      "create a task to", "create a task", "remind me to", "remind me" })
                    {
                        int p = title.ToLower().IndexOf(phrase);
                        if (p >= 0) { title = title.Substring(p + phrase.Length); break; }
                    }
                    if (matched != "")
                    {
                        int pos = title.ToLower().IndexOf(matched);
                        if (pos >= 0) title = title.Remove(pos, matched.Length);
                    }
                    title = title.Trim(' ', '-', ':', '.', ',');
                    if (title.StartsWith("to ", StringComparison.OrdinalIgnoreCase)) title = title.Substring(3);
                    if (title.Length < 2)
                    {
                        _awaitingTaskTitle = true;
                        return "Sure! What is the task? For example: 'Review my privacy settings'.";
                    }
                    title = char.ToUpper(title[0]) + title.Substring(1);

                    string description = $"Cybersecurity task: {title}";
                    int newId = _tasks.AddTask(title, description, when);
                    Log.Add($"Task added: '{title}'" + (when != null ? $" (reminder {when:yyyy-MM-dd})" : " (no reminder set)"));

                    if (when != null) return $"Task added: '{title}'. I'll remind you on {when:yyyy-MM-dd}.";
                    _pendingTaskId = newId;
                    return $"Task added: '{title}'. Would you like a reminder?";
                }

                if (text.Contains("tasks"))
                {
                    var list = _tasks.GetTasks();
                    if (list.Count == 0) return "You have no tasks yet. Try 'Add task - Enable two-factor authentication'.";

                    string result = "Here are your tasks:\n";
                    foreach (TaskItem t in list)
                    {
                        string status = t.IsCompleted ? "[done]" : "[ ]";
                        string reminder = t.ReminderDate != null ? $" - reminder: {t.ReminderDate:yyyy-MM-dd}" : "";
                        result += $"{t.Id}. {status} {t.Title} ({t.Description}){reminder}\n";
                    }
                    Log.Add("Task list viewed");
                    return result;
                }
            }
            catch (Exception)
            {
                return "Sorry, I couldn't reach the task database. Please check that MySQL is running.";
            }

            return null;
        }
        private string Normalise(string text)
        {
            foreach (var pair in _synonyms)
            {
                if (text.Contains(pair.Key) && !text.Contains(pair.Value))
                    text += " " + pair.Value;
                    
            }
            return text;
        }
        
    }
}
