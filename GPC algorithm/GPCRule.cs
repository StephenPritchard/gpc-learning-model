using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPCLearningModel
{
    class GPCRule
    {
        // ENUMERATED TYPES

        public enum RuleType
        {
            body,
            multi,
            context,
            two,
            mphon,
            single,
            outrule,
        }
        public enum RulePosition
        {
            beginning,
            middle,
            end,
            all,
        }

        // PROPERTIES

        public RulePosition RPosition { get; set; }
        public RuleType RType { get; set; }
        public string RGrapheme { get; set; }
        public string RPhoneme { get; set; }
        public bool RProtection { get; set; }
        public float RWeight { get; set; }
        public int gpcCount { get; set; }

        // FIELD

        public List<string> pWordsWhereRuleApplies = new List<string>();
        public List<string> sWordsWhereRuleApplies = new List<string>();
        public List<int> graphemeIndexWhereRuleApplies = new List<int>();


        // CONSTRUCTOR

        // Construct with individual values
        public GPCRule(RulePosition rPos, RuleType rType, string rGrapheme, string rPhoneme, bool rProtection, float rWeight, int rCount, string pWord, string sWord, int gIndex)
        {
            RPosition = rPos;
            RType = rType;
            RGrapheme = rGrapheme;
            RPhoneme = rPhoneme;
            RProtection = rProtection;
            RWeight = rWeight;
            gpcCount = rCount;
            pWordsWhereRuleApplies.Add(pWord);
            sWordsWhereRuleApplies.Add(sWord);
            graphemeIndexWhereRuleApplies.Add(gIndex);
        }

        // Construct with individual values, but don't record source word-pair.
        // used when creating context rules or extrapolating rules
        public GPCRule(RulePosition rPos, RuleType rType, string rGrapheme, string rPhoneme, bool rProtection, float rWeight, int rCount)
        {
            RPosition = rPos;
            RType = rType;
            RGrapheme = rGrapheme;
            RPhoneme = rPhoneme;
            RProtection = rProtection;
            RWeight = rWeight;
            gpcCount = rCount;
        }


        /// <summary>
        /// Method to allow for the word pairs in which the rule is observed to be recorded.
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        public void AddWordPair(string letterString, string phonemeString, int gIndex)
        {
            bool pairSeen = false;
            for (int i = 0; i < pWordsWhereRuleApplies.Count; i++)
            {
                if ((pWordsWhereRuleApplies[i] == letterString) &&
                    (sWordsWhereRuleApplies[i] == phonemeString) &&
                    (graphemeIndexWhereRuleApplies[i] == gIndex))
                {
                    pairSeen = true;
                }
            }

            if (pairSeen == false)
            {
                pWordsWhereRuleApplies.Add(letterString);
                sWordsWhereRuleApplies.Add(phonemeString);
                graphemeIndexWhereRuleApplies.Add(gIndex);
            }
        }


        /// <summary>
        /// Returns the list index if the GPCRule is used in the 
        /// input writtenword-spokenword pair.
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        /// <returns></returns>
        public int UsesRule(string letterString, string phonemeString)
        {
            int index = -1;
            for (int i = 0; i < pWordsWhereRuleApplies.Count; i++)
            {
                if ((pWordsWhereRuleApplies[i] == letterString) &&
                    (sWordsWhereRuleApplies[i] == phonemeString))
                {
                    index = graphemeIndexWhereRuleApplies[i];
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// return a char (b, m, e, or A) for position, so that it can easily be printed
        /// </summary>
        /// <returns></returns>
        public char GetPosChar()
        {
            switch (RPosition)
            {
                case (RulePosition.beginning):
                    return 'b';
                case (RulePosition.middle):
                    return 'm';
                case (RulePosition.end):
                    return 'e';
                case (RulePosition.all):
                    return 'A';
                default:
                    return '?';
            }
        }


        /// <summary>
        /// Return a string for rule type, for easy printing
        /// </summary>
        /// <returns></returns>
        public string GetTypeString()
        {
            switch (RType)
            {
                case (RuleType.body):
                    return "body";
                case (RuleType.multi):
                    return "multi";
                case (RuleType.two):
                    return "two";
                case (RuleType.mphon):
                    return "mphon";
                case (RuleType.context):
                    return "cs";
                case (RuleType.single):
                    return "sing";
                case (RuleType.outrule):
                    return "out";
                default:
                    return "?";
            }
        }


        /// <summary>
        /// Return a char for protection setting, for easy printing
        /// </summary>
        /// <returns></returns>
        public char GetProtectionChar()
        {
            switch (RProtection)
            {
                case (true):
                    return 'p';
                default:
                    return 'u';
            }
        }
    }
}
