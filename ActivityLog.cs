using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberSecurityChatbotGUI
{
    // Stores timestamped descriptions of what the bot has done
    public class ActivityLog
    {
        private readonly List<string> _entries = new List<string>();

        public void Add(string description)
        {
            _entries.Add($"{DateTime.Now:yyyy-MM-dd HH:mm} - {description}");
        }

        // Returns the most recent actions as a numbered list
        public string Show(int count = 10)
        {
            if (_entries.Count == 0)
                return "I haven't done anything for you yet.";

            var recent = _entries.Skip(Math.Max(0, _entries.Count - count)).ToList();
            string result = "Here's a summary of recent actions:\n";
            for (int i = 0; i < recent.Count; i++)
                result += $"{i + 1}. {recent[i]}\n";
            return result;
        }
    }
}