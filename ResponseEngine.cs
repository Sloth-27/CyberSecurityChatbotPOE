using System;
using System.Collections.Generic;
using System.Linq;

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

        private const string DefaultReply = "I'm not sure I understand. Can you try rephrasing?";

        private readonly Dictionary<string, List<string>> _tips = new()
        {
            ["password"] = new List<string>
            {   
                "Do not share your password with anyone, even if they claim to be from a trusted organisation.",
                "Use strong, unique passwords for each account. Avoid personal details in them.",
                "Longer is stronger: try a passphrase of four or more random words.",
                "Use a password manager so you never have to reuse passwords."
                
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

        private readonly string[] _followUps = { "another", "more", "explain", "elaborate", "again", "don't understand" };

        public void SetUserName(string name) => _userName = name;

        public string Respond(string input)
        {
            string text = input.Trim().ToLower();
            if (text.Length == 0) return DefaultReply;

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

            return reply;
        }
    }
}